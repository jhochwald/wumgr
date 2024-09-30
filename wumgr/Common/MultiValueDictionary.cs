#region

using System;
using System.Collections.Generic;

#endregion

namespace wumgr.Common;

public class MultiValueDictionary<TKey, TValue> : Dictionary<TKey, List<TValue>>
{
    public void Add(TKey key, TValue value)
    {
        List<TValue> container = null;
        if (!TryGetValue(key, out container))
        {
            container = new List<TValue>();
            base.Add(key, container);
        }

        container.Add(value);
    }

    public bool ContainsValue(TKey key, TValue value)
    {
        bool toReturn = false;
        List<TValue> values = null;
        if (TryGetValue(key, out values)) toReturn = values.Contains(value);
        return toReturn;
    }

    public void Remove(TKey key, TValue value)
    {
        List<TValue> container = null;
        if (TryGetValue(key, out container))
        {
            container.Remove(value);
            if (container.Count <= 0) Remove(key);
        }
    }

    public List<TValue> GetValues(TKey key, bool returnEmptySet = true)
    {
        List<TValue> toReturn = null;
        if (!TryGetValue(key, out toReturn) && returnEmptySet) toReturn = new List<TValue>();
        return toReturn;
    }

    public int GetCount()
    {
        int count = 0;
        foreach (KeyValuePair<TKey, List<TValue>> pair in this)
            count += pair.Value.Count;
        return count;
    }

    public TValue GetAt(int index)
    {
        int count = 0;
        foreach (KeyValuePair<TKey, List<TValue>> pair in this)
        {
            if (count + pair.Value.Count > index)
                return pair.Value[index - count];
            count += pair.Value.Count;
        }

        throw new IndexOutOfRangeException();
    }

    public TKey GetKey(int index)
    {
        int count = 0;
        foreach (KeyValuePair<TKey, List<TValue>> pair in this)
        {
            if (count + pair.Value.Count > index)
                return pair.Key;
            count += pair.Value.Count;
        }

        throw new IndexOutOfRangeException();
    }
}