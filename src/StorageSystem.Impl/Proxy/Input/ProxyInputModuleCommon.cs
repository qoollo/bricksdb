using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Ninject;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.HashHelp;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules;
using Qoollo.Impl.Modules.Queue;
using Qoollo.Impl.Proxy.Caches;

namespace Qoollo.Impl.Proxy.Input
{
    internal class ProxyInputModuleCommon : ControlModule
    {
        private readonly Dictionary<string, ProxyInputModule> _apis; 
        private readonly QueueConfiguration _queueConfiguration;
        private readonly ProxyDistributorModule _distributor;
        private readonly AsyncProxyCache _asyncProxyCache;
        private readonly ProxyMainLogicModule _mainLogic;        
        private readonly IGlobalQueue _queue;

        public ProxyInputModuleCommon(StandardKernel kernel, ProxyMainLogicModule mainLogic, QueueConfiguration queueConfiguration,
            ProxyDistributorModule distributor, AsyncProxyCache asyncProxyCache)
            :base(kernel)
        {
            Contract.Requires(queueConfiguration != null);
            Contract.Requires(mainLogic != null);
            Contract.Requires(distributor != null);
            Contract.Requires(asyncProxyCache != null);

            _queueConfiguration = queueConfiguration;
            _distributor = distributor;
            _asyncProxyCache = asyncProxyCache;
            _mainLogic = mainLogic;
            _queue = kernel.Get<IGlobalQueue>();
            _apis = new Dictionary<string, ProxyInputModule>();
        }

        public override void Start()
        {
            _queue.ProxyInputOtherQueue.Registrate(_queueConfiguration, ProcessInner);
            _queue.ProxyInputWriteAndUpdateQueue.Registrate(_queueConfiguration, ProcessInner);
        }

        private void ProcessInner(InnerData ev)
        {
            if (!_mainLogic.Process(ev))
                _queue.ProxyInputOtherQueue.Add(ev);
        }

        public void ProcessData(InnerData ev, string tableName)
        {
            ev.Transaction.TableName = tableName;
            _queue.ProxyInputWriteAndUpdateQueue.Add(ev);
        }

        public UserTransaction GetTransaction(UserTransaction transaction)
        {
            return _mainLogic.GetTransaction(transaction);
        }

        public IStorageInner CreateApi(string tableName, bool hashFromValue, IHashCalculater hashCalculater)
        {
            if (_apis.ContainsKey(tableName))
                return null;

            var api = new ProxyInputModule(Kernel, tableName, hashFromValue, _asyncProxyCache, hashCalculater,
                _distributor, this);
            _apis.Add(tableName, api);

            return api;
        }
    }
}
