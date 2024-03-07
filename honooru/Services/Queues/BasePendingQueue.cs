﻿using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace honooru.Services.Queues {

    public abstract class BasePendingQueue<T, U> : IProcessQueue {

        internal ILogger _Logger;

        /// <summary>
        ///     queue of the items to be pulled out
        /// </summary>
        internal ConcurrentQueue<T> _Items = new ConcurrentQueue<T>();

        /// <summary>
        ///     a signal for when an item is in the queue
        /// </summary>
        internal SemaphoreSlim _Signal = new SemaphoreSlim(0);

        /// <summary>
        ///     how long it takes each item in the queue to be processed in some way
        /// </summary>
        internal ConcurrentQueue<long> _ProcessTime = new ConcurrentQueue<long>();

        /// <summary>
        ///     how many items have been processed in this queue
        /// </summary>
        internal long _ProcessedCount = 0;

        /// <summary>
        ///     Set of pending entries to be updated
        /// </summary>
        private readonly HashSet<U> _Pending = new();

        public BasePendingQueue(ILoggerFactory factory) {
            _Logger = factory.CreateLogger($"watchtower.Services.Queues.BasePendingQueue<{typeof(T).Name}>");
        }

        /// <summary>
        ///     Get the next item in the list. This will block until there is one available
        /// </summary>
        /// <param name="cancel">Stopping token</param>
        public async Task<T> Dequeue(CancellationToken cancel) {
            await _Signal.WaitAsync(cancel);
            _Items.TryDequeue(out T? entry);
            ++_ProcessedCount;

            U id = GetEntryID(entry!);
            lock (_Pending) {
                _Pending.Remove(id);
            }

            return entry!;
        }

        /// <summary>
        ///     Attempt to dequeue an entry from the queue. If no entry is in the queue, the return value will be null.
        ///     This call will not block until there is an entry in the queue, unlike <see cref="Dequeue(CancellationToken)"/>
        /// </summary>
        /// <returns></returns>
        public T? TryDequeue() {
            _Items.TryDequeue(out T? entry);

            if (entry != null) {
                U id = GetEntryID(entry);
                lock (_Pending) {
                    _Pending.Remove(id);
                }
            }

            return entry;
        }

        /// <summary>
        ///     Peak at the next item in the queue. This will block until there is one available.
        ///     DO NOT USE THIS WITH MULTIPLER WORKERS. If you have multiple background processors using Peak,
        ///     they will be working on the same <typeparamref name="T"/>!
        /// </summary>
        /// <param name="cancel">Stopping token</param>
        /// <returns></returns>
        public async Task<T> Peak(CancellationToken cancel) {
            await _Signal.WaitAsync(cancel);
            _Items.TryPeek(out T? entry);
            _Signal.Release();

            return entry!;
        }

        /// <summary>
        ///     Insert a new entry into the front of the queue. Use this sparingly,
        ///     as in order to insert at the top of the list, a copy of the list must be allocated,
        ///     the items are cleared, then each item is re-queued behind <paramref name="entry"/>
        /// </summary>
        /// <param name="entry">Entry to be queued at the front</param>
        public void QueueAtFront(T entry) {

            U id = GetEntryID(entry);
            lock (_Pending) {
                if (_Pending.Contains(id)) {
                    return;
                }
                _Pending.Add(id);
            }

            lock (_Items) {
                T[] items = _Items.ToArray();
                _Items.Clear();
                _Items.Enqueue(entry);
                foreach (T iter in items) {
                    _Items.Enqueue(iter);
                }
            }
            _Signal.Release();
        }

        /// <summary>
        ///     Queue a new entry into the queue
        /// </summary>
        public void Queue(T entry) {

            U id = GetEntryID(entry);
            lock (_Pending) {
                if (_Pending.Contains(id)) {
                    _Logger.LogDebug($"duplicate entry {id} already pending, not updating");
                    return;
                }
                _Pending.Add(id);
            }

            _Items.Enqueue(entry);
            _Signal.Release();
        }

        /// <summary>
        ///     Add some basic metrics about how long it took to process "something" in the queue
        /// </summary>
        /// <param name="ms">How many milliseconds it took to process something that came from this queue</param>
        public void AddProcessTime(long ms) {
            _ProcessTime.Enqueue(ms);
            while (_ProcessTime.Count > 100) {
                _ = _ProcessTime.TryDequeue(out _);
            }
        }

        /// <summary>
        ///     Get a copy of a list that contains how many milliseconds it took to process data in this queue
        /// </summary>
        /// <returns>
        ///     A newly allocated list whose elements represent how long it took to process data from this queue
        /// </returns>
        public List<long> GetProcessTime() {
            lock (_ProcessTime) {
                List<long> ms = new List<long>(_ProcessTime);
                return ms;
            }
        }

        /// <summary>
        ///     Get how many entries are in the queue
        /// </summary>
        /// <returns></returns>
        public int Count() {
            return _Items.Count;
        }

        /// <summary>
        ///     Return how many items have been removed from this queue
        /// </summary>
        /// <returns></returns>
        public long Processed() {
            return _ProcessedCount;
        }

        /// <summary>
        ///     Allocate a copy of the items in the list
        /// </summary>
        /// <returns>A newly allocated list that contains a shallow-reference to the items in the list</returns>
        public List<T> ToList() {
            T[] arr = new T[Count()];
            _Items.CopyTo(arr, 0);
            return arr.ToList();
        }

        /// <summary>
        ///     Get the ID of the <typeparamref name="T"/> based on a specific instance of the object
        /// </summary>
        /// <param name="entry"></param>
        /// <returns>
        ///     <typeparamref name="U"/>, which is used as a key to preventing duplicate entries from being entered into the queue
        /// </returns>
        internal abstract U GetEntryID(T entry);

    }
}
