using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace honooru.Services.Queues {

    public abstract class BasePendingQueue<T, U> : BaseQueue<T>, IProcessQueue {

        /// <summary>
        ///     Set of pending entries to be updated
        /// </summary>
        private readonly HashSet<U> _Pending = new();

        public BasePendingQueue(ILoggerFactory factory) : base(factory) {
            _Logger = factory.CreateLogger($"honooru.Services.Queues.BasePendingQueue<{typeof(T).Name}>");
        }

        /// <summary>
        ///     Get the next item in the list. This will block until there is one available
        /// </summary>
        /// <param name="cancel">Stopping token</param>
        public new async Task<T> Dequeue(CancellationToken cancel) {
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
        public new T? TryDequeue() {
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
        public new async Task<T> Peak(CancellationToken cancel) {
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
        public new void QueueAtFront(T entry) {

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
        public new void Queue(T entry) {

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
        ///     Get the ID of the <typeparamref name="T"/> based on a specific instance of the object
        /// </summary>
        /// <param name="entry"></param>
        /// <returns>
        ///     <typeparamref name="U"/>, which is used as a key to preventing duplicate entries from being entered into the queue
        /// </returns>
        internal abstract U GetEntryID(T entry);

    }
}
