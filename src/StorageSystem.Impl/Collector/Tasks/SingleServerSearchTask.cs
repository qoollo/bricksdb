using System.Collections.Generic;
using System.Linq;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Server;

namespace Qoollo.Impl.Collector.Tasks
{
    public class SingleServerSearchTask
    {
        public string TableName { get; private set; }
        public string Script { get; private set; }
        public bool IsAllDataRead { get; private set; }
        public bool IsServersAvailbale { get; private set; }

        public object LastKey { get; private set; }

        public FieldDescription IdDescription { get; private set; }
        public ServerId ServerId { get; private set; }
        private List<SearchData> _data;
        private int _dataPos;
        public List<FieldDescription> UserParametrs { get; set; } 

        public SingleServerSearchTask(ServerId serverId, string script, FieldDescription idDescription, string tableName)
        {
            TableName = tableName;
            IdDescription = idDescription;
            Script = script;
            _data = new List<SearchData>();
            IsAllDataRead = false;
            IsServersAvailbale = true;
            ServerId = serverId;
            _dataPos = 0;
            UserParametrs = new List<FieldDescription>();
        }

        #region Data work

        public void AddPage(List<SearchData> page)
        {
            for (int i = 0; i < _dataPos; i++)
            {
                _data.RemoveAt(0);
            }
            _dataPos = 0;

            _data.AddRange(page);
        }

        public int Length
        {
            get { return _data.Count - _dataPos; }
        }

        public SearchData GetData()
        {
            if (_dataPos >= _data.Count)
                return null;

            return _data[_dataPos];
        }

        public void IncrementPosition()
        {
            _dataPos++;
        }

        public void AllDataRead()
        {
            IsAllDataRead = true;
        }

        public void ServerUnavailable()
        {
            IsServersAvailbale = false;
        }

        #endregion

        #region Page work

        public void SetLastKey(object key)
        {
            LastKey = key;
            IdDescription.Value = LastKey;
        }

        public void FindNextLastKey()
        {
            LastKey = _data.Last().Key;
            IdDescription.Value = LastKey;
        }

        #endregion
    }
}
