using System;
using System.Collections.Generic;
using Qoollo.Impl.Collector.Tasks;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Modules.Db.Impl;

namespace Qoollo.Impl.Collector
{
    internal class SelectReader:DbReader<int>
    {
        private List<SearchData> _searchData;
        private readonly int _limitCount;
        private SearchTask _searchTask;
        private int _pos;
        private bool _isStart;
        private int _countFields;
        private int _current;

        public SystemSearchStateInner SearchState
        {
            get { return _searchTask.SearchState; }
        }

        public override int Reader
        {
            get { throw new NotImplementedException(); }
        }

        public override bool IsCanRead
        {
            get
            {
                CheckIsStart();

                if (_limitCount == -1)
                    return _pos < _searchData.Count;
             
                if (_current >= _limitCount)
                    return false;
                
                return _pos < _searchData.Count;
            }
        }

        public SelectReader(SearchTask searchTask, List<SearchData> list, int limitCount)
        {
            _searchTask = searchTask;
            _pos = -1;
            _searchData = list;
            _limitCount = limitCount;
            _isStart = false;
            _current = 0;
        }

        protected override int CountFieldsInner()
        {            
            CheckIsStart();
            return _countFields;
        }

        protected override void ReadNextInner()
        {
            CheckIsStart();
            _pos++;
            _current++;

            if (_pos == _searchData.Count && _searchTask.IsCanRead())
            {
                _searchData = _searchTask.GetDataInner();
                _pos = 0;
            }
        }

        protected override object GetValueInner(int index)
        {
            CheckIsStart();

            if (index >= _countFields)
                throw  new IndexOutOfRangeException();            

            return _searchData[_pos].Fields[index].Item1;
        }

        protected override object GetValueInner(string index)
        {
            CheckIsStart();

            var value = _searchData[_pos].Fields.Find(x => x.Item2.ToLower() == index.ToLower());
            if(value ==null)
                throw new IndexOutOfRangeException();  

            return value.Item1;
        }

        protected override bool IsValidRead()
        {
            CheckIsStart();
            return true;
        }

        protected override void StartInner()
        {
            _isStart = true;
            if (_searchData.Count > 0)
                _countFields = _searchData[0].Fields.Count;
        }

        private void CheckIsStart()
        {
            if(!_isStart)
                throw new Exception(Errors.DbReaderNotStarted);
        }
    }
}
