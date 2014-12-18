using System;
using System.Collections.Generic;
using System.Linq;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Exceptions;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.NetInterfaces.Data;

namespace Qoollo.Impl.DbController.Db
{
    public class DbModuleCollection:DbModule
    {
        private Dictionary<string, DbModule> _dbModules;

        public DbModuleCollection()
        {
            _dbModules = new Dictionary<string, DbModule>();
        }

        public override void Start()
        {
            if (_dbModules.Count == 0)
                throw new InitializationException(Errors.DatabaseIsEmpty);

            _dbModules.Values.ToList().ForEach(x => x.Start());
        }

        #region Db helper

        public RemoteResult AddDbModule(DbModule dbModule)
        {
            if(_dbModules.ContainsKey(dbModule.TableName))
                return new InnerFailResult(Errors.TableAlreadyExists);

            _dbModules.Add(dbModule.TableName, dbModule);
            return new SuccessResult();
        }

        public List<DbModule> GetDbModules
        {
            get { return _dbModules.Values.ToList(); }
        } 

        private RemoteResult CommonMethodHandler(string name, Func<DbModule, RemoteResult> func)
        {
            if (name == Consts.AllTables)
            {
                var res = _dbModules.Select(module => func(module.Value)).ToList();

                return AggregateResultHelper.AggregateResults(res);
            }

            DbModule dbModule = null;
            if (_dbModules.TryGetValue(name, out dbModule))
                return func(dbModule);

            return new InnerFailResult(Errors.TableDoesNotExists);
        }

        private RemoteResult CommonMethodHandler(InnerData data, Func<DbModule, RemoteResult> func)
        {
            return CommonMethodHandler(data.Transaction.TableName, func);
        }
        private RemoteResult CommonMethodHandler(Func<DbModule, RemoteResult> func)
        {
            return CommonMethodHandler(Consts.AllTables, func);
        }

        private InnerData CommonMethodHandler(InnerData data, Func<DbModule, InnerData> func)
        {
            DbModule dbModule = null;
            
            if (_dbModules.TryGetValue(data.Transaction.TableName, out dbModule))
                return func(dbModule);

            var ret = new InnerData(data.Transaction)
            {
                Key = data.Key,
                Data = null
            };

            ret.Transaction.SetError();
            ret.Transaction.AddErrorDescription(Errors.TableDoesNotExists);

            return ret;
        }

        #endregion

        #region DbModule

        public RemoteResult InitDb(string name)
        {
            return CommonMethodHandler(name, (dbModule) => dbModule.InitDb());
        }        

        public override RemoteResult InitDb()
        {
            return CommonMethodHandler((dbModule) => dbModule.InitDb());
        }

        public override RemoteResult Create(InnerData obj, bool local)
        {
            return CommonMethodHandler(obj, (dbModule) => dbModule.Create(obj, local));
        }

        public override RemoteResult Update(InnerData obj, bool local)
        {
            return CommonMethodHandler(obj, (dbModule) => dbModule.Update(obj, local));
        }

        public override RemoteResult Delete(InnerData obj)
        {
            return CommonMethodHandler(obj, (dbModule) => dbModule.Delete(obj));
        }

        public override RemoteResult DeleteFull(InnerData obj)
        {
            return CommonMethodHandler(obj, (dbModule) => dbModule.DeleteFull(obj));
        }        

        public override RemoteResult RestoreUpdate(InnerData obj, bool local)
        {
            return CommonMethodHandler(obj, (dbModule) => dbModule.RestoreUpdate(obj, local));
        }

        public override RemoteResult CustomOperation(InnerData obj, bool local)
        {
            return CommonMethodHandler(obj, (dbModule) => dbModule.CustomOperation(obj, local));
        }

        public override RemoteResult CreateRollback(InnerData obj, bool local)
        {
            return CommonMethodHandler(obj, (dbModule) => dbModule.CreateRollback(obj, local));
        }

        public override RemoteResult UpdateRollback(InnerData obj, bool local)
        {
            return CommonMethodHandler(obj, (dbModule) => dbModule.UpdateRollback(obj, local));
        }

        public override RemoteResult DeleteRollback(InnerData obj, bool local)
        {
            return CommonMethodHandler(obj, (dbModule) => dbModule.DeleteRollback(obj, local));
        }

        public override RemoteResult CustomOperationRollback(InnerData obj, bool local)
        {
            return CommonMethodHandler(obj, (dbModule) => dbModule.CustomOperationRollback(obj, local));
        }

        #endregion

        #region Not common func

        public override InnerData ReadExternal(InnerData obj)
        {
            return CommonMethodHandler(obj, (dbModule) => dbModule.ReadExternal(obj));
        }        

        public override RemoteResult SelectRead(SelectDescription description, out SelectSearchResult searchResult)
        {
            SelectSearchResult value = null;
            var ret =  CommonMethodHandler(description.TableName,
                (dbModule) => dbModule.SelectRead(description, out value));

            searchResult = value;
            return ret;
        }

        #endregion

        #region not implemented

        public override string TableName
        {
            get { throw new NotImplementedException(); }
        }

        public override RemoteResult AsyncProcess(bool isDeleted, bool local, int countElemnts, Action<InnerData> process, Func<MetaData, bool> isMine, bool isFirstRead,
            ref object lastId)
        {
            throw new NotImplementedException();
        }

        #endregion

        protected override void Dispose(bool isUserCall)
        {
            if(isUserCall)
                _dbModules.Values.ToList().ForEach(x => x.Dispose());

            base.Dispose(isUserCall);
        }
    }
}
