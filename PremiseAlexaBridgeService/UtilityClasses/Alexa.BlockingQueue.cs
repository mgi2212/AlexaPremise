using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Configuration;
using System.Threading;

namespace PremiseAlexaBridgeService
{
    public sealed class AlexaBlockingQueue<T> : IEnumerable<T>
    {
        #region Fields

        private readonly Queue<T> _queue = new Queue<T>();
        private int _count;

        #endregion Fields

        #region Methods

        public int Count => _queue.Count;

        public List<T> InternalQueue => _queue.ToList();

        public void Clear()
        {
            lock (_queue)
            {
                _queue.Clear();
            }
        }

        public T Dequeue()
        {
            lock (_queue)
            {
                while (_count <= 0) Monitor.Wait(_queue);
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
            while (true) yield return Dequeue();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)this).GetEnumerator();
        }

        #endregion Methods
    }
}