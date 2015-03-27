using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Qoollo.Turbo.Threading.QueueProcessing;

namespace BricksDb.RedisInterface.Server
{
    class RedisServer
    {
        private readonly TcpListener tcpListener;
        private DeleageQueueAsyncProcessor<Socket> _queue;
        private readonly int _processorCount = Environment.ProcessorCount;
        private readonly int _maxQueueSize = 10000;


        public RedisServer()
        {
            IPAddress ipAddress = LocalIPAddress(); //System.Net.IPAddress.Parse("10.5.7.11");
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);
            tcpListener = new TcpListener(localEndPoint);
        }


        public IPAddress LocalIPAddress()
        {
            IPHostEntry host;
            IPAddress localIP = null;
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip;
                    break;
                }
            }
            return localIP;
        }

        private void StartListener()
        {
            tcpListener.Start();
            Console.WriteLine("Listen started on {0} ...", tcpListener.LocalEndpoint.ToString());
        }

        public void ListenWithQueue()
        {
            StartListener();
            _queue = new Qoollo.Turbo.Threading.QueueProcessing.DeleageQueueAsyncProcessor<Socket>(_processorCount, _maxQueueSize, 
                "Work thread",ReadSocket);
            _queue.Start();

            Console.WriteLine("Socket queue started with # of threads: {0}", _queue.ThreadCount);

            try
            {
                while (true)
                {
                    var task = tcpListener.AcceptSocketAsync();
                    task.Wait();
                    _queue.Add(task.Result);
                }
            }
            catch (Exception e){}

            _queue.Stop();
        }

        public void Listen()
        {
            StartListener();
            try
            {
                while (true)
                {
                    var task = tcpListener.AcceptSocketAsync();
                    task.Wait();
                    Task.Factory.StartNew(() => ReadSocket(task.Result, new CancellationToken()));
                }
            }
            catch (Exception e) { }
        }

        public void ReadSocket(Socket handler, CancellationToken token)
        {
            var bytes = new byte[1024];
            
            int bytesRec = handler.Receive(bytes);
            if (bytesRec != 0)
            {
                var data = Encoding.ASCII.GetString(bytes, 0, bytesRec);
                var responce = ProcessMessage(data);
                byte[] msg = Encoding.ASCII.GetBytes(responce);

                handler.Send(msg); // TODO: контрлировать, сколько отослал, т.е. sended = han...

                _queue.Add(handler);
            }
        }

        private string ProcessMessage(string message)
        {
            var responce = "+OK\r\n";
            return responce;
        }
    }
}
