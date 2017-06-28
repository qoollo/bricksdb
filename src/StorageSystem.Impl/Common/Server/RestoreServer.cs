using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Qoollo.Impl.Common.HashFile;
using Qoollo.Impl.Common.HashHelp;

namespace Qoollo.Impl.Common.Server
{
    [Serializable]
    [DataContract]
    public class RestoreServer : ServerId
    {
        [DataMember]
        public bool IsNeedRestore { get; set; }

        [DataMember]
        public bool IsRestored { get; set; }

        [DataMember]
        public bool IsFailed { get; set; }

        public bool IsCurrentServer { get; set; }

        private readonly List<HashMapRecord> _getHashMap;

        public RestoreServer(string remoteHost, int port)
            : base(remoteHost, port)
        {
            CommonServer();
        }

        public RestoreServer(ServerId server, List<HashMapRecord> getHashMap)
            : base(server)
        {
            _getHashMap = getHashMap;
            CommonServer();
        }

        public RestoreServer() : base("default", -1)
        {
        }

        private void CommonServer()
        {
            IsNeedRestore = false;
            IsRestored = false;
            IsFailed = false;
            IsCurrentServer = false;
        }

        public void NeedRestoreInitiate()
        {
            IsNeedRestore = true;
            IsRestored = false;
            IsFailed = false;
        }

        public void AfterFailed()
        {
            IsNeedRestore = true;
            IsRestored = false;
        }

        public bool IsNeedCurrentRestore()
        {
            return IsNeedRestore && !IsRestored;
        }

        public bool IsServerRestored()
        {
            return IsRestored && !IsFailed;
        }

        public bool IsHashInRange(string hash)
        {
            return _getHashMap.Exists(
                x =>
                    HashComparer.Compare(x.Begin, hash) <= 0 &&
                    HashComparer.Compare(hash, x.End) <= 0);
        }

        public override string ToString()
        {
            return $"{base.ToString()}, restored = {IsRestored}, failed = {IsFailed}";
        }
    }
}
