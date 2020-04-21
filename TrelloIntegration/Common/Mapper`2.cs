namespace TrelloIntegration.Common
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class Mapper<T1, T2> : IEnumerable<KeyValuePair<T1, T2>>
    {
        private Dictionary<T1, T2> _direct;
        private Dictionary<T2, T1> _reverse;

        public T2 this[T1 t1] { get { return Transform(t1); } }

        public T1 this[T2 t2] { get { return Transform(t2); } }

        public Mapper()
        {
            _direct = new Dictionary<T1, T2>();
            _reverse = new Dictionary<T2, T1>();
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
            _direct[t1] = t2;
            _reverse[t2] = t1;
        }

        public T2 Transform(T1 t1)
        {
            return _direct.TryGetValue(t1, out T2 temp) ? temp : default(T2);
        }

        public T1 Transform(T2 t2)
        {
            return _reverse.TryGetValue(t2, out T1 temp) ? temp : default(T1);
        }

        public IEnumerator<KeyValuePair<T1, T2>> GetEnumerator()
        {
            return _direct.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
