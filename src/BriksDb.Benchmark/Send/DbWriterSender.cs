using System;
using System.Diagnostics.Contracts;
using Qoollo.Client.Support;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.NetInterfaces.Data;
using Qoollo.Impl.NetInterfaces.Writer;
using Consts = Qoollo.Impl.Common.Support.Consts;

namespace Qoollo.Benchmark.Send
{
    class DbWriterSender:DataSender
    {
        public DbWriterSender(string host, int port, string tableName)
        {
            Contract.Requires(!string.IsNullOrEmpty(host));
            Contract.Requires(!string.IsNullOrEmpty(tableName));
            Contract.Requires(port > 0);
            _host = host;
            _port = port;
            _tableName = tableName;

            _dataProvider = new DataProvider();
        }

        private readonly string _host;
        private readonly int _port;
        private readonly string _tableName;
        private ICommonNetReceiverWriterForWrite _channel;
        private readonly DataProvider _dataProvider;


        public override void Start()
        {
            _channel = CreateChannel(_host, _port);
            _channel.Ping();
        }

        private ICommonNetReceiverWriterForWrite CreateChannel(string host, int port)
        {
            var factory = NetConnector.Connect<ICommonNetReceiverWriterForWrite>(
                new ServerId(host, port), Client.Support.Consts.WcfServiceName,
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));
            return factory.CreateChannel();
        }

        public void Stop()
        {
        }

        public override bool Send(long key, string data)
        {
            try
            {
                var result = _channel.ProcessSync(new InnerData(new Transaction("123", "123")
                {
                    OperationName = OperationName.Create,
                    OperationType = OperationType.Sync,
                    TableName = _tableName
                })
                {
                    Data = _dataProvider.SerializeValue(data),
                    Key = _dataProvider.SerializeKey(key)
                });
                return !result.IsError;
            }
            catch (Exception e)
            {
                 return false;    
            }            
        }

        private class DataProvider:CommonDataProvider<long, string>
        {
            public override string CalculateHashFromKey(long key)
            {
                return key.ToString();
            }            
        }
    }
}
