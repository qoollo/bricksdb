using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Qoollo.Impl.Common.Exceptions;
using Qoollo.Impl.Common.HashFile;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules;

namespace Qoollo.Impl.Writer.Model
{
    internal class WriterModel:ControlModule
    {
        private readonly HashMap _map;
        private List<HashMapRecord> _localMap; 
        private readonly ServerId _local;

        public List<HashMapRecord> LocalMap { get { return _localMap; } }

        public ServerId Local { get { return _local; } }

        public List<ServerId> Servers { get { return _map.Servers.Select(x => new ServerId(x)).ToList(); } } 

        public WriterModel(ServerId local, HashMapConfiguration hashMapConfiguration)
        {
            Contract.Requires(local!=null);

            _local = local;
            _map = new HashMap(hashMapConfiguration);
        }

        public override void Start()
        {
            _map.CreateMap();
            _localMap = _map.GetLocalMap(_local);
            if(_localMap.Count==0)
                throw new Exception("There is no server in hash file with our address");
        }

        public bool IsMine(string hash)
        {
            return _localMap.Exists(x => x.IsMine(hash));
        }

        public void UpdateModel()
        {
            _map.UpdateFileModel(); 
            _localMap = _map.GetLocalMap(_local);
            if (_localMap.Count == 0)
                throw new InitializationException("There is no server in hash file with our address");
        }

        public string UpdateHashViaNet(List<HashMapRecord> map)
        {
            var currentMap = _map.Map;
            bool equal = true;
            currentMap.ForEach(x =>
            {
                if (!map.Contains(x))
                    equal = false;
            });

            map.ForEach(x =>
            {
                if (!currentMap.Contains(x))
                    equal = false;
            });

            if (equal)
                return string.Empty;

            HashFileUpdater.UpdateFile(_map.FileName);
            _map.CreateNewMapWithFile(map);
            try
            {
                UpdateModel();
            }
            catch (InitializationException e)
            {
                return e.Message;
            }
            return string.Empty;
        }

        protected override void Dispose(bool isUserCall)
        {
            if(isUserCall)
                _map.Dispose();
        }        
    }
}
