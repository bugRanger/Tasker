namespace Common
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class Mapper<T1, T2> : IEnumerable<KeyValuePair<T1, T2>>, IDictionary<T1, T2>
    {
        #region Fields

        private Dictionary<T1, T2> _direct;
        private Dictionary<T2, T1> _reverse;

        #endregion Fields

        #region Properties

        public T2 this[T1 key] { get => Transform(key); set => ((IDictionary<T1, T2>)_direct)[key] = value; }

        public T1 this[T2 key] { get { return Transform(key); } }

        public ICollection<T1> Keys => ((IDictionary<T1, T2>)_direct).Keys;

        public ICollection<T2> Values => ((IDictionary<T1, T2>)_direct).Values;

        public int Count => ((IDictionary<T1, T2>)_direct).Count;

        public bool IsReadOnly => ((IDictionary<T1, T2>)_direct).IsReadOnly;

        #endregion Properties

        public Mapper()
        {
            _direct = new Dictionary<T1, T2>();
            _reverse = new Dictionary<T2, T1>();
        }

        #region Methods

        public T2 Transform(T1 t1)
        {
            return _direct.TryGetValue(t1, out T2 temp) ? temp : default(T2);
        }

        public T1 Transform(T2 t2)
        {
            return _reverse.TryGetValue(t2, out T1 temp) ? temp : default(T1);
        }

        public bool TryGetValue(T1 t1, out T2 t2)
        {
            return _direct.TryGetValue(t1, out t2);
        }

        public bool ContainsKey(T1 t1) 
        {
            return _direct.ContainsKey(t1);
        }

        public bool TryGetValue(T2 t2, out T1 t1)
        {
            return _reverse.TryGetValue(t2, out t1);
        }

        public bool ContainsKey(T2 t2)
        {
            return _reverse.ContainsKey(t2);
        }

        public void Add(T1 t1, T2 t2)
        {
            if (_direct.ContainsKey(t1) && !_direct[t1].Equals(t2))
                Remove(_direct[t1]);

            if (_reverse.ContainsKey(t2) && !_reverse[t2].Equals(t1))
                Remove(t2);

            _direct[t1] = t2;
            _reverse[t2] = t1;
        }

        public void Add(KeyValuePair<T1, T2> item)
        {
            if (_direct.ContainsKey(item.Key) && !_direct[item.Key].Equals(item.Value))
                Remove(_direct[item.Key]);

            if (_reverse.ContainsKey(item.Value) && !_reverse[item.Value].Equals(item.Key))
                Remove(item.Value);

            _direct.Add(item.Key, item.Value);
            _reverse.Add(item.Value, item.Key);
        }

        public bool Remove(T1 key)
        {
            if (!_direct.TryGetValue(key, out var value))
                return true;

            if (_reverse.ContainsKey(value))
                _reverse.Remove(value);

            return _direct.Remove(key);
        }

        public bool Remove(T2 key)
        {
            if (!_reverse.TryGetValue(key, out var value))
                return true;

            if (_direct.ContainsKey(value))
                _direct.Remove(value);

            return _reverse.Remove(key);
        }

        public void Clear()
        {
            _direct.Clear();
            _reverse.Clear();
        }

        public bool Contains(KeyValuePair<T1, T2> item)
        {
            return ((IDictionary<T1, T2>)_direct).Contains(item);
        }

        public void CopyTo(KeyValuePair<T1, T2>[] array, int arrayIndex)
        {
            ((IDictionary<T1, T2>)_direct).CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<T1, T2> item)
        {
            _reverse.Remove(item.Value);
            return ((IDictionary<T1, T2>)_direct).Remove(item);
        }

        public IEnumerator<KeyValuePair<T1, T2>> GetEnumerator()
        {
            return _direct.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion Methods
    }
}
