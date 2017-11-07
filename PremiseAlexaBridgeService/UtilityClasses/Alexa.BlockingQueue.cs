using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Alexa.RegisteredTasks;

namespace PremiseAlexaBridgeService
{
    public sealed class AlexaBlockingQueue<T> : IEnumerable<T> where T : new()
    {
        #region Fields

        private readonly Queue<T> _queue = new Queue<T>();
        private int _count;

        #endregion Fields

        #region Methods

        public List<T> InternalQueue => _queue.ToList();

        public T Dequeue()
        {
            lock (_queue)
            {
                while (_count <= 0)
                {
                    Monitor.Wait(_queue, 1000);
                    if (BackgroundTaskManager.Shutdown.IsCancellationRequested)
                    {
                        return new T();
                    }
                }
                _count--;
                return _queue.Dequeue();
            }
        }

        public void Enqueue(T data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            lock (_queue)
            {
                _queue.Enqueue(data);
                _count++;
                Monitor.Pulse(_queue);
            }
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            while (!BackgroundTaskManager.Shutdown.IsCancellationRequested)
            {
                if (!BackgroundTaskManager.Shutdown.IsCancellationRequested)
                {
                    yield return Dequeue();
                }
                else
                {
                    yield break;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)this).GetEnumerator();
        }

        #endregion Methods
    }
}