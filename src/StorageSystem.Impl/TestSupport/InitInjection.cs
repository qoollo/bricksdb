using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Impl.TestSupport
{
    internal static class InitInjection
    {

        public static bool RestoreUsePackage { get; set; }

        static InitInjection()
        {
            RestoreUsePackage = false;
        }
    }
}
