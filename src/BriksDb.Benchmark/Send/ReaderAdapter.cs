using System;
using System.Diagnostics.Contracts;
using Qoollo.Benchmark.Commands;
using Qoollo.Benchmark.Send.Interfaces;
using Qoollo.Client.CollectorGate;
using Qoollo.Client.Configuration;
using Qoollo.Client.WriterGate;

namespace Qoollo.Benchmark.Send
{
    class ReaderAdapter:IDataAdapter
    {
        private readonly CollectorCommand _command;
        private readonly CollectorGate _collector;

        public ReaderAdapter(DbFactory dbFactory,  CollectorCommand command)
        {
            Contract.Requires(dbFactory != null);
            Contract.Requires(command != null);

            _command = command;
            bool useDistributor = !IsUseDistributor();
            _collector = new CollectorGate(command.TableName, dbFactory,
                new CollectorConfiguration(command.HashFileName, command.CountReplics, command.PageSize, useDistributor),
                new CollectorNetConfiguration(),
                new TimeoutConfiguration());
            _collector.Build();            
        }

        private bool IsUseDistributor()
        {
            return _command.Host != "default" && _command.Port != -1;
        }

        public void Start()
        {
            _collector.Start();
            if (IsUseDistributor())
            {
                var result = _collector.Api.SayIAmHere(_command.Host, _command.Port);
                if(result.IsError)
                    Console.WriteLine(result);
            }
        }

        public StorageDbReader ExecuteQuery(QueryDescription query)
        {
            return _collector.Api.CreateReader(query.QueryScript);
        }

        public void Dispose()
        {
            _collector.Dispose();
        }
    }
}
