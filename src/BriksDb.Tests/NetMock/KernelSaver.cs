using System;
using Ninject;
using Ninject.Modules;
using Qoollo.Impl.TestSupport;

namespace Qoollo.Tests.NetMock
{
    public class KernelSaver:IDisposable
    {
        private readonly StandardKernel _oldKernel;
        private readonly StandardKernel _newKernel;

        public KernelSaver(StandardKernel newkernel)
        {
            _oldKernel = InitInjection.Kernel;
            _newKernel = newkernel;
            InitInjection.Kernel = newkernel;
        }

        public KernelSaver():this(new StandardKernel(new TestInjectionModule()))
        {

        }

        public StandardKernel Kernel => _newKernel;

        public void Dispose()
        {
            InitInjection.Kernel = _oldKernel;
        }
    }
}