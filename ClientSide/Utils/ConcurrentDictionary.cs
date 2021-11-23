using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace SNet_Client.Utils
{
    public class SNETConcurrentDictionary<TKey, TValue>
    {
        private Dictionary<TKey, TValue> InternalDict = new Dictionary<TKey, TValue>();

        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);        

        public void Add(TKey key, TValue value)
        {
            _lock.EnterWriteLock();
            try
            {
                InternalDict.Add(key, value);
            }
            finally
            {
                if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
            }
        }
        public void Add(KeyValuePair<TKey, TValue> item)
        {
            _lock.EnterWriteLock();
            try
            {
                InternalDict.Add(item.Key, item.Value);
            }
            finally
            {
                if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
            }
        }

        public void Clear()
        {
            _lock.EnterWriteLock();
            try
            {
                InternalDict.Clear();
            }
            finally
            {
                if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
            }
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            _lock.EnterReadLock();
            try
            {
                return InternalDict.ContainsKey(item.Key) && InternalDict.ContainsValue(item.Value);
            }
            finally
            {
                if (_lock.IsWriteLockHeld) _lock.ExitReadLock();
            }
        }

        public bool ContainsKey(TKey key)
        {
            _lock.EnterReadLock();
            try
            {
                return InternalDict.ContainsKey(key);
            }
            finally
            {
                if (_lock.IsWriteLockHeld) _lock.ExitReadLock();
            }
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            _lock.EnterReadLock();
            try
            {
                return InternalDict.GetEnumerator();
            }
            finally
            {
                if (_lock.IsWriteLockHeld) _lock.ExitReadLock();
            }
        }

        public bool Remove(TKey key)
        {
            _lock.EnterWriteLock();
            try
            {
                return InternalDict.Remove(key);
            }
            finally
            {
                if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
            }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            _lock.EnterWriteLock();
            try
            {
                if(InternalDict.TryGetValue(item.Key, out TValue value))
                {
                    if (value.Equals(item.Value))
                        return InternalDict.Remove(item.Key);
                }
                return false;
            }
            finally
            {
                if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            _lock.EnterReadLock();
            try
            {
                return InternalDict.TryGetValue(key, out value);
            }
            finally
            {
                if (_lock.IsWriteLockHeld) _lock.ExitReadLock();
            }
        }
    }
}
