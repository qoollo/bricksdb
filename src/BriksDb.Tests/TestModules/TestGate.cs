﻿using Qoollo.Client.Configuration;
using Qoollo.Client.ProxyGate;
using Qoollo.Tests.Support;

namespace Qoollo.Tests.TestModules
{
    public class TestGate : ProxyApi
    {
        //private IStorage<int, DataWithBuffer> _data;

        public TestGate(NetConfiguration netConfiguration, ProxyConfiguration proxyConfiguration)
            : base(netConfiguration, proxyConfiguration)
        {
        }

        //public IStorage<int, DataWithBuffer> Test
        //{
        //    get { return _data; }
        //}

        public IStorage<int, int> Int
        {
            get { return CallApi<int, int>("Int"); }
        }

        public IStorage<int, int> Int2
        {
            get { return CallApi<int, int>("Int2"); }
        }

        public IStorage<int, int> Int3
        {
            get { return CallApi<int, int>("Int3"); }
        }

        protected override void InnerBuild()
        {
            RegistrateApi("Int3", true, new IntValueHashDataProvider());
            //_data = RegistrateApi("Test", false, new DataWithBufferDataProvider());
            RegistrateApi("Int", false, new IntDataProvider());
            RegistrateApi("Int2", false, new IntDataProvider());
        }
    }
}
