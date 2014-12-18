using System;
using System.Collections.Generic;
using Qoollo.Impl.Common.HashFile;
using Qoollo.Impl.Configurations;

namespace Qoollo.HashFileGenerator
{
    public static class HashFileGen
    {
        public static void Generate(string fileName, List<Tuple<string, int, int>> servers)
        {
            var writer =
                new HashWriter(new HashMapConfiguration(fileName, HashMapCreationMode.CreateNew, servers.Count,
                    servers.Count, HashFileType.Distributor));
            writer.CreateMap();

            int index = 0;

            foreach (var server in servers)
            {
                writer.SetServer(index, server.Item1, server.Item2, server.Item3);
                index++;
            }

            writer.Save();
        }

        public static void Generate(string fileName, string[] args)
        {           
            var servers = new List<Tuple<string, int, int>>();

            for (int i = 1; i < args.Length; i+=3)
            {
                servers.Add(new Tuple<string, int, int>(args[i], int.Parse(args[i + 1]), int.Parse(args[i+2])));
            }

            Generate(fileName, servers);
        }

        public static void Generate(string fileName)
        {            
            var servers = new List<Tuple<string, int,int>>();
            var server = AskServer();
            servers.Add(server);

            while (Finish())
            {
                server = AskServer();
                servers.Add(server);
            }            

            Generate(fileName, servers);
        }

        private static Tuple<string, int, int> AskServer()
        {
            Console.WriteLine("Input host:");
            string host = Console.ReadLine();
            Console.WriteLine("Input port for Distributor:");
            int p1 = int.Parse(Console.ReadLine());
            Console.WriteLine("Input port for Collector:");
            int p2 = int.Parse(Console.ReadLine());

            return new Tuple<string, int, int>(host, p1, p2);
        }

        private static bool Finish()
        {
            Console.WriteLine("Add another server?(yes/no):");
            string host = Console.ReadLine();
            return host == "yes";
        }
    }
}
