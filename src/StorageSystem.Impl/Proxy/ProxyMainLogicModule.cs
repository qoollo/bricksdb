using System.Diagnostics.Contracts;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Modules;
using Qoollo.Impl.Proxy.Caches;
using Qoollo.Impl.Proxy.ProxyNet;

namespace Qoollo.Impl.Proxy
{
    internal class ProxyMainLogicModule : ControlModule
    {
        private ProxyDistributorModule _distributor;
        private IProxyNetModule _net;
        private ProxyCache _cache;

        public ProxyMainLogicModule(ProxyDistributorModule distributorModule, IProxyNetModule net,
                                    ProxyCache proxyCache)
        {
            Contract.Requires(distributorModule != null);
            Contract.Requires(net != null);
            Contract.Requires(proxyCache != null);
            _net = net;
            _distributor = distributorModule;
            _cache = proxyCache;
        }       

        public bool Process(InnerData ev)
        {
            ev.Transaction.ProxyServerId = _distributor.ProxyServerId;
            var dest = _distributor.GetDestination(ev.Transaction.UserTransaction);

            if (dest == null)
            {
                //конец всего, некуда отправить данные
                _cache.AddToCache(ev.Transaction.CacheKey, new ServerId("", -1));
                return true;
            }            

            var result = _net.Process(dest, ev);
            if (result is FailNetResult)
            {                
                //todo Надо поискать другой сервер, надо потом записать новый сервер
                _cache.AddToCache(ev.Transaction.EventHash, dest);
                return false;
            }            
            return true;
        }

        public UserTransaction GetTransaction(UserTransaction transaction)
        {
            var cachedData = _cache.Get(transaction.CacheKey);
            var server = cachedData==null || cachedData.Port==-1 ? _distributor.GetDestination(transaction):cachedData;

            UserTransaction result = null;
            var nresult = _net.GetTransaction(server, transaction, out result);
            if (nresult is FailNetResult)
            {
                result = new UserTransaction(transaction);
                result.SetError();
                result.AddErrorDescription(Errors.ServerWithResultNotAvailable);
            }
            return result;
        }

    }
}
