using System;
using System.Diagnostics.Contracts;
using Qoollo.Benchmark.Commands;
using Qoollo.Benchmark.Send.Interfaces;
using Qoollo.Client.Request;

namespace Qoollo.Benchmark.Send
{
    class DistributorAdapter : ICrud
    {
        public DistributorAdapter(WriterCommand command)
        {
            Contract.Requires(command != null);
            _command = command;
            //, new NetConfiguration(_command.Localhost, command.Localport)
            _proxy = new ProxyGate(command.TableName);
            _proxy.Build();
        }

        private readonly WriterCommand _command;
        private readonly ProxyGate _proxy;        

        public void Start()
        {
            _proxy.Start();
            var result = _proxy.Api.SayIAmHere(_command.Host, _command.Port);
            if (result.IsError)
                Console.WriteLine(result);
        }

        public bool Send(long key, string data)
        {            
            return !_proxy.Api.CreateSync(key, data).IsError;
        }

        public bool Read(long key)
        {
            RequestDescription result;
            _proxy.Api.Read(key, out result);
            return !result.IsError;
        }

        public void Dispose()
        {
            _proxy.Dispose();
        }
    }
}
