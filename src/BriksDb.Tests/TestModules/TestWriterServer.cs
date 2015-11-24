﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.NetInterfaces.Writer;

namespace Qoollo.Tests.TestModules
{
    class TestWriterServer : ICommonNetReceiverWriterForWrite, IDisposable
    {
        public ServiceHost Host;
        public InnerData retData = null;

        public int Value = 0;
        public void Process(InnerData data)
        {
            Interlocked.Add(ref Value, 1);
        }

        public Task<RemoteResult> ProcessTaskBased(InnerData data)
        {
            return Task.FromResult(ProcessSync(data));
        }

        public RemoteResult ProcessSync(InnerData data)
        {
            Interlocked.Add(ref Value, 1);
            return new SuccessResult();
        }

        public void Rollback(InnerData data)
        {
            Interlocked.Add(ref Value, -1);
        }

        public InnerData ReadOperation(InnerData data)
        {
            return retData;
        }

        public RemoteResult SendSync(NetCommand command)
        {
            return new SuccessResult();
        }

        public void SendASync(NetCommand command)
        {
        }

        public RemoteResult Ping()
        {
            return new SuccessResult();
        }

        public void Dispose()
        {
            try
            {
                Host.Close();
            }
            catch (Exception)
            {
            }
        }
    }
}
