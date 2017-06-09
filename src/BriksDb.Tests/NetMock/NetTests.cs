using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ninject;
using Ninject.Modules;
using Ninject.Parameters;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Modules.Net.ConnectionBehavior;
using Qoollo.Impl.NetInterfaces;
using Qoollo.Impl.NetInterfaces.Distributor;
using Qoollo.Impl.Writer.WriterNet;

namespace Qoollo.Tests.NetMock
{
    public class NModule : NinjectModule
    {
        public override void Load()
        {
            Bind<IConnectionBehavior<ICommonNetReceiverForDb>>()
            .To<WpfConnection<ICommonNetReceiverForDb>>();
        }
    }

    [TestClass]
    public class NetTests
    {
        [TestMethod]
        public void SomeTest()
        {
            //var kernel = new StandardKernel();
            //kernel.Load(Assembly.GetExecutingAssembly());

            //kernel.Bind<ITest<object>>().To<Test2<object>>();

            //var test = kernel.Get<ITest<object>>(new ConstructorArgument("t1", 10));
            //kernel.Bind<IConnectionBehavior<ICommonNetReceiverForDb>>()
            //    .To<WpfConnection<ICommonNetReceiverForDb>>();

            var kernel = new StandardKernel();
            var ass = Assembly.GetExecutingAssembly();
            kernel.Load(Assembly.GetExecutingAssembly());            

            var connection = kernel.Get<IConnectionBehavior<ICommonNetReceiverForDb>>(
                new ConstructorArgument("server", new ServerId("123", 123)),
                new ConstructorArgument("configuration", (object)null),
                new ConstructorArgument("timeoutConfiguration", (object)null));
            //var connection = new SingleConnectionToDistributor(new ServerId("123", 123), null, null);
        }

        

        interface ITest<TType>
        {
             
        }

        private abstract class Test1<TType> : ITest<TType>
        {
        }

        private class Test2<TType> : Test1<TType>
        {
            private readonly int _t1;

            public Test2(int t1)
            {
                _t1 = t1;
            }
        }
    }
}
