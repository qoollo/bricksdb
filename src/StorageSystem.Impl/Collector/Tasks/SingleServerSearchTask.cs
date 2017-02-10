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

//        public object LastKey { get; private set; }

        public FieldDescription IdDescription { get; private set; }
        public List<FieldDescription> OrderKeyDescriptions { get; set; }
        public ServerId ServerId { get; private set; }
        private readonly List<SearchData> _data;
        private int _dataPos;
        public List<FieldDescription> UserParametrs { get; set; }
        public bool IsUserScript { get; set; }

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
            IsUserScript = false;
        }

        #region Data work

        public void AddPage(List<SearchData> page)
        {
            _data.RemoveRange(0, _dataPos);
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

        public SearchData GetData(int deep)
        {
            if (_dataPos + deep >= _data.Count)
                return null;

            return _data[_dataPos + deep];
        }

        public void RemoveAt(int deep)
        {
            if (_dataPos + deep >= _data.Count)
                return;

            _data.RemoveAt(_dataPos + deep);
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

        public void FindNextLastKey()
        {
            //TODO check
            IdDescription.Value = _data[_data.Count - 1].Key;
            if (OrderKeyDescriptions != null)
            {
                foreach (var description in OrderKeyDescriptions)
                {
                    var value = _data[_data.Count - 1].Fields.First(x => string.Equals(x.Item2, description.AsFieldName, System.StringComparison.OrdinalIgnoreCase)).Item1;
                    description.Value = value;
                }
            }
        }

        #endregion
    }
}
