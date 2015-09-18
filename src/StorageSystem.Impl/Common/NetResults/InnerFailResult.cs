﻿using System.Runtime.Serialization;

namespace Qoollo.Impl.Common.NetResults
{
    [DataContract]
    internal class InnerFailResult:RemoteResult
    {
        public InnerFailResult(string description) : base(true, description)
        {
        }

        public InnerFailResult(string description, bool isError)
            : base(isError, description)
        {
        }
    }
}
