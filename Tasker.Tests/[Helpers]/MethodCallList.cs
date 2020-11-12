namespace Framework.Tests
{
    using System.Collections.Generic;

    using NUnit.Framework;

    internal class MethodCallList
    {
        #region Fields

        private readonly List<Tests.MethodCallEntry> _expectedCallList;

        private readonly List<Tests.MethodCallEntry> _actualCallList;

        #endregion Fields

        #region Constructors

        public MethodCallList()
        {
            _expectedCallList = new List<Tests.MethodCallEntry>();
            _actualCallList = new List<Tests.MethodCallEntry>();
        }

        #endregion Constructors

        #region Methods

        public void Clear()
        {
            _expectedCallList.Clear();
            _actualCallList.Clear();
        }

        public void Add(MethodCallEntry methodCallEntry)
        {
            _actualCallList.Add(methodCallEntry);
        }

        public void Assert(params MethodCallEntry[] methodCallEntry)
        {
            _expectedCallList.AddRange(methodCallEntry);
            CollectionAssert.AreEqual(_expectedCallList, _actualCallList);
        }

        public void AssertSet(params MethodCallEntry[] methodCallEntry)
        {
            _expectedCallList.AddRange(methodCallEntry);
            CollectionAssert.AreEquivalent(_expectedCallList, _actualCallList);

            // Normalize order of calls to allow call Assert later
            for (var i = _actualCallList.Count - methodCallEntry.Length; i < _actualCallList.Count; ++i)
                _expectedCallList[i] = _actualCallList[i];
        }

        #endregion Methods
    }
}
