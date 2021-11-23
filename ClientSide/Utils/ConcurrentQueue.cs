using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace SNet_Client.Utils
{
    public class SNETConcurrentQueue<T>
    {
        private Queue<T> InternalQueue = new Queue<T>();

        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        public bool TryDequeue(out T value)
        {
            value = default(T);
            _lock.EnterWriteLock();
            try
            {
                if (InternalQueue.Count > 0)
                {
                    value = InternalQueue.Dequeue();
                    return true;
                }
                return false;
            }
            finally
            {
                if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
            }
        }

        public void Enqueue(T value)
        {
            _lock.EnterWriteLock();
            try
            {
                InternalQueue.Enqueue(value);
            }
            finally
            {
                if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
            }
        }
    }
}
