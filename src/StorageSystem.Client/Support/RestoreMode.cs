using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Impl.Common.Exceptions;
using Qoollo.Impl.Common.Support;

namespace Qoollo.Client.Support
{
    public enum RestoreMode
    {
        FullRestoreNeed = 0,
        SimpleRestoreNeed = 1,
        Default = 2
    }

    internal static class RestoreModeConverter
    {
        public static RestoreState Convert(RestoreMode mode)
        {
            RestoreState state;
            if (Enum.TryParse(Enum.GetName(typeof (RestoreMode), mode), out state))
                return state;
            throw new InitializationException(string.Format("Cannot convert {0} to inner type", mode));
        }
    }
}
