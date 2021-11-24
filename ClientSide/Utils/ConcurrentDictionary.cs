using System.Collections.Generic;

namespace SNet_Client.Utils
{
    public class SNETConcurrentDictionary<TKey, TValue>
    {
        private Dictionary<TKey, TValue> InternalDict = new Dictionary<TKey, TValue>();

        private readonly object _lock = new object();

        public void Add(TKey key, TValue value)
        {
            lock (_lock)
            {
                InternalDict.Add(key, value);
            }
        }
        public void Add(KeyValuePair<TKey, TValue> item)
        {
            lock (_lock)
            {
                InternalDict.Add(item.Key, item.Value);
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                InternalDict.Clear();
            }
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            lock (_lock)
            {
                return InternalDict.ContainsKey(item.Key) && InternalDict.ContainsValue(item.Value);
            }
        }

        public bool ContainsKey(TKey key)
        {
            lock (_lock)
            {
                return InternalDict.ContainsKey(key);
            }
        }

        public bool Remove(TKey key)
        {
            lock (_lock)
            {
                return InternalDict.Remove(key);
            }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            lock (_lock)
            {
                if (InternalDict.TryGetValue(item.Key, out TValue value))
                {
                    if (value.Equals(item.Value))
                        return InternalDict.Remove(item.Key);
                }
                return false;
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            lock (_lock)
            {
                return InternalDict.TryGetValue(key, out value);
            }
        }
    }
}
