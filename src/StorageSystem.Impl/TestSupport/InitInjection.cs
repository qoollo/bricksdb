using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Impl.Common.Support;

namespace Qoollo.Impl.TestSupport
{
    internal static class InitInjection
    {
        public static string RestoreHelpFileOut { get; set; }

        public static string RestoreHelpFile
        {
            get { return string.IsNullOrEmpty(RestoreHelpFileOut) ? Consts.RestoreHelpFile : RestoreHelpFileOut; }
        }
    }
}
