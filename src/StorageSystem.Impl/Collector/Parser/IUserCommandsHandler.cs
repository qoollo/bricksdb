using System;
using System.Collections.Generic;

namespace Qoollo.Impl.Collector.Parser
{
    public interface IUserCommandsHandler
    {
        List<Tuple<string, Type>> GetDbFieldsDescription();

        Dictionary<string, Type> GetMetaDescription();

        string GetKeyName();
    }
}
