using System.Collections.Generic;
using System.Threading;

namespace SNet_Client.Utils
{
    public class SNETConcurrentQueue<T>
    {
        private Queue<T> InternalQueue = new Queue<T>();

        private readonly object _lock = new object();

        public bool TryDequeue(out T value)
        {
            value = default(T);
            lock (_lock)
            { 
                if (InternalQueue.Count > 0)
                {
                    value = InternalQueue.Dequeue();
                    return true;
                }
                return false;
            }
        }
		
		public bool TryDequeue(out T value, int waitingTime)
        {
            value = default(T);
            bool result = false;
            try
            {
                result = Monitor.TryEnter(_lock, waitingTime);
                if (result)
                {
                    if (InternalQueue.Count > 0)
                    {
                        value = InternalQueue.Dequeue();
                        return true;
                    }
                    return false;
                }
                return false;
            }
            finally
            {
                if (result)
                    Monitor.Exit(_lock);
            }
        }

        public void Enqueue(T value)
        {
            lock (_lock)
            {
                InternalQueue.Enqueue(value);
            }
        }
    }
}
