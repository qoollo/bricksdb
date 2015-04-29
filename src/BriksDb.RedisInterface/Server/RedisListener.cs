﻿using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Qoollo.Client.ProxyGate;
using Qoollo.Turbo.Threading.QueueProcessing;

namespace BricksDb.RedisInterface.Server
{
    internal class RedisListener
    {
        private readonly IStorage<string, string> _redisTable;
        private readonly TcpListener _tcpListener;
        private DeleageQueueAsyncProcessor<Socket> _queue;        
        private const int MaxQueueSize = 10000;
        private readonly Func<string, string> _processMessageFunc;

        public RedisListener(RedisMessageProcessor process, IStorage<string, string> redisTable)
        {
            _redisTable = redisTable;
            _processMessageFunc = process.ProcessMessage;
            var ipAddress = LocalIpAddress();
            var localEndPoint = new IPEndPoint(ipAddress, 11000);
            _tcpListener = new TcpListener(localEndPoint);
        }
        
        private void ProcessSocket(Socket handler, CancellationToken token)
        {
            var bytes = new byte[1024];

            int bytesRec = handler.Receive(bytes);            
            if (bytesRec != 0)
            {
                var data = Encoding.ASCII.GetString(bytes, 0, bytesRec);
                var responce = _processMessageFunc(data);
                byte[] msg = Encoding.ASCII.GetBytes(responce);

                Send(handler, msg);

                _queue.Add(handler, token);
            }
        }

        private void Send(Socket handler, byte[] msg)
        {
            var offset = 0;
            var length = msg.Length;

            while (offset < length)
            {
                var sended =  handler.Send(msg, offset, length, SocketFlags.None);
                offset += sended;
                length -= sended;
            }
        }

        private void ConnectToBriksDb()
        {
            var result = _redisTable.SayIAmHere(ConfigurationHelper.Instance.DistributorHost,
                ConfigurationHelper.Instance.DistributorPort);
            Console.WriteLine(result);
        }

        public void ListenWithQueue()
        {
            ConnectToBriksDb();

            _tcpListener.Start();
            Console.WriteLine("Listen started on {0} ...", _tcpListener.LocalEndpoint);

            _queue = new DeleageQueueAsyncProcessor<Socket>(ConfigurationHelper.Instance.CountThreads,
                MaxQueueSize, "Work thread", ProcessSocket);
            _queue.Start();

            Console.WriteLine("Socket queue started with # of threads: {0}", _queue.ThreadCount);

            try
            {
                while (true)
                {
                    var task = _tcpListener.AcceptSocketAsync();
                    task.Wait();
                    _queue.Add(task.Result);
                }
            }
            catch (Exception e)
            {
            }
        }

        public void ListenWithQueueAsync()
        {
            Task.Factory.StartNew(ListenWithQueue);
        }

        public void StopListen()
        {
            _tcpListener.Stop();
            _queue.Stop();
        }

        public static IPAddress LocalIpAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            return host.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
        }

    }
}
