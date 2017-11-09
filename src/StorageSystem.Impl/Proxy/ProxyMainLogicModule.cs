using Ninject;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Modules;
using Qoollo.Impl.Proxy.Interfaces;

namespace Qoollo.Impl.Proxy
{
    internal class ProxyMainLogicModule : ControlModule, IProxyMainLogicModule
    {
        private readonly Qoollo.Logger.Logger _logger = Logger.Logger.Instance.GetThisClassLogger();

        private IProxyDistributorModule _distributor;
        private IProxyNetModule _net;
        private IProxyCache _cache;

        public ProxyMainLogicModule(StandardKernel kernel) : base(kernel)
        {
        }

        public override void Start()
        {
            _distributor = Kernel.Get<IProxyDistributorModule>();
            _net = Kernel.Get<IProxyNetModule>();
            _cache = Kernel.Get<IProxyCache>();
        }

        public bool Process(InnerData ev)
        {
            ev.Transaction.ProxyServerId = _distributor.ProxyServerId;
            var dest = _distributor.GetDestination(ev.Transaction.UserTransaction);

            if (dest == null)
            {
                //конец всего, некуда отправить данные
                _logger.Warn("Cannot send data to distributor");
                _cache.AddToCache(ev.Transaction.CacheKey, new ServerId("default", -1));
                return true;
            }            

            var result = _net.Process(dest, ev);
            if (result is FailNetResult)
            {                
                //todo Надо поискать другой сервер, надо потом записать новый сервер
                _cache.AddToCache(ev.Transaction.DataHash, dest);
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
