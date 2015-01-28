using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.DistributorModules.DistributorNet.Interfaces;

namespace Qoollo.Tests.TestModules
{
    class NetModuleTest : INetModule
    {
        public Dictionary<ServerId, bool> Return;

        public NetModuleTest(Dictionary<ServerId, bool> ret)
        {
            Return = ret;
        }

        public RemoteResult Process(ServerId server, InnerData data)
        {
            bool ret = false;
            if (Return.TryGetValue(server, out ret))
            {
                if (ret)
                    return new SuccessResult();
            }
            return new FailNetResult("");
        }

        public RemoteResult Rollback(ServerId server, InnerData data)
        {
            return new SuccessResult();
        }

        public RemoteResult SendToProxy(ServerId server, NetCommand command)
        {
            throw new NotImplementedException();
        }

        public RemoteResult ASendToProxy(ServerId server, NetCommand command)
        {
            throw new NotImplementedException();
        }

        public RemoteResult SendToDistributor(ServerId server, NetCommand command)
        {
            throw new NotImplementedException();
        }

        public InnerData ReadOperation(ServerId server, InnerData data)
        {
            throw new NotImplementedException();
        }

        public void AddServerCallBack(ServerId server, string result)
        {
            throw new NotImplementedException();
        }

        public RemoteResult AddNewDistributor(ServerId destination, ServerId local)
        {
            throw new NotImplementedException();
        }

    }
}
