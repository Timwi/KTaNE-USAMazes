using System;
using System.Collections.Generic;

namespace WorldMazes
{
    static class ExtensionMethods
    {
        public static void AddSafe<TKey, TValue>(this Dictionary<TKey, List<TValue>> dic, TKey key, TValue value)
        {
            List<TValue> list;
            if (!dic.TryGetValue(key, out list))
                dic[key] = list = new List<TValue>();
            list.Add(value);
        }

        public static int IncSafe<K>(this IDictionary<K, int> dic, K key, int amount = 1)
        {
            return dic.ContainsKey(key) ? (dic[key] = dic[key] + amount) : (dic[key] = amount);
        }

        public static int IndexOf<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            int index = 0;
            foreach (var v in source)
            {
                if (predicate(v))
                    return index;
                index++;
            }
            return -1;
        }
    }
}
