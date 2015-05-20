using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Concierge;
using Qoollo.Concierge.Attributes;
using Qoollo.Concierge.Commands;

namespace Qoollo.Benchmark
{
    [DefaultExecutor]
    class BenchmarkExecutor:IUserExecutable
    {
        [CommandHandler("test","Test command")]
        public string TestCommand(UserCommand command)
        {
            return "All right";
        }

        public void Dispose()
        {            
        }

        public void Start()
        {            
        }

        public void Stop()
        {            
        }

        public IWindowsServiceConfig Configuration
        {
            get { return new WinServiceConfig
            {
                Async = true,
                Description = "Service for BriksDb benchmark",
                DisplayName = "BriksDb.Benchmark",
                InstallName = "BriksDb.Benchmark",
                StartAfterInstall = true
            }; }
        }
    }
}
