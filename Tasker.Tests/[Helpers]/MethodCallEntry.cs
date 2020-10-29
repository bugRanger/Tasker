namespace Framework.Tests
{
    using System.Linq;
    using System.Text;
    using System.Collections.Generic;

    internal class MethodCallEntry
    {
        private readonly object[] _arguments;

        protected MethodCallEntry(params object[] arguments)
        {
            _arguments = arguments;
        }

        public override bool Equals(object obj)
        {
            if (GetType() != obj?.GetType())
                return false;

            var entry = (MethodCallEntry)obj;

            if (_arguments.Length != entry._arguments.Length)
                return false;

            for (var i = 0; i < _arguments.Length; ++i)
            {
                if (_arguments[i] == null && entry._arguments[i] == null)
                    continue;

                if (_arguments[i] == null && entry._arguments[i] != null ||
                    _arguments[i] != null && entry._arguments[i] == null)
                    return false;

                if (_arguments[i] is IEnumerable<MethodCallEntry> argumentCollection)
                {
                    if (!(entry._arguments[i] is IEnumerable<MethodCallEntry> entryCollection))
                        return false;

                    var argumentList = argumentCollection.ToList();
                    var entryList = entryCollection.ToList();

                    if (argumentList.Count != entryList.Count)
                        return false;

                    for (var j = 0; j < argumentList.Count; j++)
                    {
                        if (argumentList[j]._arguments.Length != entryList[j]._arguments.Length)
                            return false;

                        for (var k = 0; k < argumentList[j]._arguments.Length; k++)
                        {
                            if (!argumentList[j]._arguments[k].Equals(entryList[j]._arguments[k]))
                                return false;
                        }
                    }

                }
                else if (!Equals(_arguments[i], entry._arguments[i]))
                    return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            var hashCode = 0;
            for (var i = 0; i < _arguments.Length; ++i)
                if (_arguments[i] != null)
                    hashCode ^= _arguments[i].GetHashCode();
            return hashCode;
        }

        public override string ToString()
        {
            var sb = new StringBuilder(GetType().Name);

            if (_arguments.Length > 0)
            {
                for (var i = 0; i < _arguments.Length; ++i)
                    sb.AppendFormat("{0} ", _arguments[i]);
                sb.Length -= 1;
            }

            return sb.ToString();
        }
    }
}
