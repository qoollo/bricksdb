using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Qoollo.Impl.Common.Support;

namespace Qoollo.Impl.TestSupport
{
    internal static class InitInjection
    {
        public static string RestoreHelpFileOut { get; set; }
        internal static string RestoreHelpFile
        {
            get { return string.IsNullOrEmpty(RestoreHelpFileOut) ? Consts.RestoreHelpFile : RestoreHelpFileOut; }
        }

        private static TimeSpan _pingPeriodOut = TimeSpan.FromMinutes(1);
        public static TimeSpan PingPeriodOut { get{return _pingPeriodOut;} set { _pingPeriodOut = value; } }
        internal static TimeSpan PingPeriod { get { return PingPeriodOut; } }        

        public static bool RestoreUsePackage { get; set; }

        static InitInjection()
        {
            RestoreUsePackage = false;
        }
    }
}
