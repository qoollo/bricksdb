using System;
using System.Collections.Generic;
using Qoollo.Impl.Common.Data.DataTypes;

namespace Qoollo.Impl.Writer.Db
{
    public class RestoreDataContainer
    {
        public bool IsDeleted { get; private set; }
        public bool Local { get; private set; }
        public int CountElemnts { get; private set; }
        public Action<InnerData> Process { get; private set; }
        public Action<List<InnerData>> ProcessPackage { get; private set; }
        public Func<MetaData, bool> IsMine { get; private set; }
        public bool UsePackage { get; private set; }
        public bool IsFirstRead { get; set; }
        public object LastId { get; set; }
        public bool IsAllDataRead { get; set; }

        public RestoreDataContainer(bool isDeleted, bool local, int countElemnts, Action<InnerData> process,
            Func<MetaData, bool> isMine, bool usePackage)
        {
            IsDeleted = isDeleted;
            Local = local;
            CountElemnts = countElemnts;
            Process = process;
            IsMine = isMine;
            UsePackage = usePackage;
            IsFirstRead = true;
        }

        public RestoreDataContainer(bool isDeleted, bool local, int countElemnts, Action<List<InnerData>> process,
            Func<MetaData, bool> isMine, bool usePackage)
        {
            IsDeleted = isDeleted;
            Local = local;
            CountElemnts = countElemnts;
            ProcessPackage = process;
            IsMine = isMine;
            UsePackage = usePackage;
            IsFirstRead = true;
        }

        public void StartNewDb()
        {
            IsFirstRead = true;
            LastId = null;
        }
    }
}
