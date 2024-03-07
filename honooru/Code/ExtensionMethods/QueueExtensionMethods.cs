using System.Collections.Generic;

namespace honooru.Code.ExtensionMethods {

    public static class QueueExtensionMethods {

        /// <summary>
        ///     deqeueue an element from the <see cref="Queue{T}"/>,
        ///     returning null if the queue is empty
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queue"></param>
        /// <returns></returns>
        public static T? DequeueOrDefault<T>(this Queue<T> queue) where T : class {
            if (queue.TryDequeue(out T? result) == true) {
                return result;
            }

            return null;
        }

        public static T? PeekOrDefault<T>(this Queue<T> queue) where T : class {
            if (queue.TryPeek(out T? result) == true) {
                return result;
            }

            return null;
        }

    }
}
