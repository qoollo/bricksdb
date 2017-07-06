﻿using System;
using System.Collections.Generic;
using Ninject;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Modules;
using Qoollo.Impl.NetInterfaces.Data;

namespace Qoollo.Impl.Writer.Db
{
    public abstract class DbModule:ControlModule
    {
        protected DbModule(StandardKernel kernel) : base(kernel)
        {
        }

        public abstract string TableName { get; }

        public abstract RemoteResult InitDb();

        public abstract RemoteResult Create(InnerData obj, bool local);

        public abstract RemoteResult Update(InnerData obj, bool local);

        public abstract RemoteResult Delete(InnerData obj);

        public abstract RemoteResult DeleteFull(InnerData obj);        

        internal abstract RemoteResult AsyncProcess(RestoreDataContainer restoreData);

        public abstract RemoteResult SelectRead(SelectDescription description, out SelectSearchResult searchResult);

        public abstract RemoteResult RestoreUpdate(InnerData obj, bool local);

        public abstract RemoteResult RestoreUpdatePackage(List<InnerData> obj);

        public abstract RemoteResult CustomOperation(InnerData obj, bool local);

        public abstract InnerData ReadExternal(InnerData obj);

        public abstract RemoteResult CreateRollback(InnerData obj, bool local);

        public abstract RemoteResult UpdateRollback(InnerData obj, bool local);

        public abstract RemoteResult DeleteRollback(InnerData obj, bool local);

        public abstract RemoteResult CustomOperationRollback(InnerData obj, bool local);
    }
}

