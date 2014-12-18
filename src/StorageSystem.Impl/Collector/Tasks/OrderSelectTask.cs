using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Qoollo.Impl.Collector.Load;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Server;

namespace Qoollo.Impl.Collector.Tasks
{
    internal class OrderSelectTask : SearchTask
    {
        private readonly int _limitCount;
        public int UserPage { get; private set; }
        private List<SearchData> _mergeData;
        private int _currentRead;
        public FieldDescription ScriptDescription { get; private set; }

        public OrderSelectTask(List<ServerId> servers, FieldDescription scriptDescription,
            FieldDescription keyDescription, string script, int limitCount, int userPage,
            List<FieldDescription> userParametrs, string tableName)
            : base(servers, keyDescription, script, userParametrs, tableName)
        {
            Contract.Requires(limitCount > 0 || limitCount == -1);
            Contract.Requires(userPage > 0);

            _limitCount = limitCount;
            ScriptDescription = scriptDescription;
            UserPage = userPage;
            _mergeData = new List<SearchData>();
            _currentRead = 0;

            if (_limitCount != -1)
                _limitCount -= userPage;
        }

        #region Interface

        public override List<SearchData> GetData()
        {
            return new List<SearchData>(_mergeData);
        }

        protected override bool BackgroundLoad(IDataLoader loader,
            Func<OrderSelectTask, List<SingleServerSearchTask>, List<SearchData>> merge)
        {
            _mergeData = merge(this, SearchTasks);

            if (_limitCount == - 1)
                return !CalculateCanReadInner();

            _currentRead += _mergeData.Count;

            if (_currentRead < _limitCount)
                return !CalculateCanReadInner();

            int diff = _currentRead - _limitCount;
            _mergeData.RemoveRange(_mergeData.Count - diff, diff);

            return true;
        }

        #endregion
    }
}
