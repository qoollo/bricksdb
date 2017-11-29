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
            throw new InitializationException($"Cannot convert {mode} to inner type");
        }

        public static Qoollo.Impl.Common.Support.RestoreType Convert(RestoreType type)
        {
            Qoollo.Impl.Common.Support.RestoreType resultType;
            if (Enum.TryParse(Enum.GetName(typeof(Qoollo.Impl.Common.Support.RestoreType), type), out resultType))
                return resultType;
            throw new InitializationException($"Cannot convert {type} to inner type");
        }
    }

    public enum RestoreType
    {
        Single = 0,
        Broadcast = 1,
        None = 2,
    }

}
