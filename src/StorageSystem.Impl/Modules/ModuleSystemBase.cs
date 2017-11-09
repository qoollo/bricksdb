using System;
using System.Collections.Generic;
using Ninject.Modules;
using Qoollo.Impl.Common.Support;

namespace Qoollo.Impl.Modules
{
    internal abstract class ModuleSystemBase:IDisposable
    {
        protected List<ControlModule> Modules = new List<ControlModule>();
        protected List<ControlModule> ModulesForDispose = new List<ControlModule>();

        public abstract void Build(NinjectModule module = null, string configFile = Consts.ConfigFilename);

        public virtual void Start()
        {
            Modules.ForEach(x=>x.Start());
        }

        protected void AddModule(ControlModule module)
        {
            Modules.Add(module);
        }

        protected void AddModuleDispose(ControlModule module)
        {
            ModulesForDispose.Add(module);
        }

        protected virtual void Dispose(bool isUserCall)
        {
            if (isUserCall)
            {
                if (ModulesForDispose.Count > 0)
                    ModulesForDispose.ForEach(x => x.Dispose());
                else
                    Modules.ForEach(x => x.Dispose());
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
