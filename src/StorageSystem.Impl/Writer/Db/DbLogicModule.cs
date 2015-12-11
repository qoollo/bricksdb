using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.HashHelp;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Common.NetResults.Inner;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Modules.Db.Impl;
using Qoollo.Impl.NetInterfaces.Data;
using Qoollo.Impl.Writer.Db.Commands;
using Qoollo.Impl.Writer.PerfCounters;

namespace Qoollo.Impl.Writer.Db
{
    internal class DbLogicModule<TCommand, TKey, TValue, TConnection, TReader> : DbModule
        where TConnection : class
    {
        private readonly IHashCalculater _hashCalculater;
        private readonly IUserCommandCreator<TCommand, TConnection, TKey, TValue, TReader> _userCommandCreator;
        private readonly IMetaDataCommandCreator<TCommand, TReader> _metaDataCommandCreator;
        private readonly DbImplModule<TCommand, TConnection, TReader> _implModule;

        private readonly string _tableName;        

        private readonly DbLogicCreateAndUpdateHelper<TCommand, TKey, TValue, TConnection, TReader> _createAndUpdate;


        public DbLogicModule(IHashCalculater hashCalc, 
            IUserCommandCreator<TCommand, TConnection, TKey, TValue, TReader> userCommandCreator,
            IMetaDataCommandCreator<TCommand, TReader> metaDataCommandCreator,
            DbImplModule<TCommand, TConnection, TReader> implModule)
        {
            Contract.Requires(hashCalc != null);
            Contract.Requires(userCommandCreator != null);
            Contract.Requires(implModule != null);
            Contract.Requires(metaDataCommandCreator != null);
            _hashCalculater = hashCalc;
            _userCommandCreator = userCommandCreator;
            _implModule = implModule;
            _metaDataCommandCreator = metaDataCommandCreator;

            _createAndUpdate = new DbLogicCreateAndUpdateHelper<TCommand, TKey, TValue, TConnection, TReader>(_userCommandCreator, _metaDataCommandCreator, _implModule);


            var idName = _userCommandCreator.GetKeyName();
            _metaDataCommandCreator.SetKeyName(idName);

            var tableName = _userCommandCreator.GetTableNameList();
            _metaDataCommandCreator.SetTableName(tableName);

            _tableName = tableName.Aggregate("", (current, result) => current + result + "_");
            _tableName = _tableName.Remove(_tableName.Length - 1);
        }

        public override string TableName
        {
            get { return _tableName; }
        }

        public override void Start()
        {
            _implModule.Start();            
        }

        #region Process

        public override RemoteResult InitDb()
        {
            var connection = _implModule.RentConnectionInner();
            bool result = _userCommandCreator.CreateDb(connection.Element);
            connection.Dispose();

            RemoteResult ret;

            if (result)
            {
                var idinit = _userCommandCreator.GetKeyInitialization();
                var metaCommand = _metaDataCommandCreator.InitMetaDataDb(idinit);

                ret = _implModule.ExecuteNonQuery(metaCommand);
            }
            else
                ret = new InnerFailResult("Fail init db");

            if (!(ret is SuccessResult))
            {
                ret = new InnerServerError(ret);
            }

            return ret;
        }
       
        private Tuple<MetaData, bool> ReadMetaData(InnerData obj)
        {
            var timer = WriterCounters.Instance.ReadMetaDataTimer.StartNew();

            object key;
            DeserializeKey(obj, out key);

            var script = _metaDataCommandCreator.ReadMetaData(_userCommandCreator.Read(), key);
            var reader = _implModule.CreateReader(script);

            var meta = new Tuple<MetaData, bool>(null, true);
            try
            {
                reader.Start();

                if (reader.IsFail)
                {
                    timer.Complete();
                    return null;
                }

                if (reader.IsCanRead)
                {
                    reader.ReadNext();

                    meta = _metaDataCommandCreator.ReadMetaDataFromReader(reader);
                }
            }
            catch (Exception e)
            {
                Logger.Logger.Instance.Warn(e, "");
            }
            finally
            {
                reader.Dispose();
            }            

            timer.Complete();
            return meta;
        }

        public override RemoteResult Create(InnerData obj, bool local)
        {
            object key;
            object value;
            DeserializeData(obj, out key, out value);

            var meta = ReadMetaData(obj);

            return _createAndUpdate.Create(obj, local, meta, key, value);

        }        

        public override RemoteResult Update(InnerData obj, bool local)
        {
            object key;
            object value;
            DeserializeData(obj, out key, out value);

            var meta = ReadMetaData(obj);

            return _createAndUpdate.Update(obj, local, meta, key, value);
        }        

        public override RemoteResult Delete(InnerData obj)
        {
            object key;
            DeserializeKey(obj, out key);

            var metaCommand = _metaDataCommandCreator.SetDataDeleted(key);            
            var ret = _implModule.ExecuteNonQuery(metaCommand);

            if (ret.IsError)
                ret = new InnerServerError(ret);
            WriterCounters.Instance.DeletePerSec.OperationFinished();
            return ret;
        }

        public override RemoteResult DeleteFull(InnerData obj)
        {
            object key;
            object value;
            DeserializeData(obj, out key, out value);

            var command = _metaDataCommandCreator.DeleteMetaData(key);            
            _implModule.ExecuteNonQuery(command);

            command = _userCommandCreator.Delete((TKey) key);
            var ret = _implModule.ExecuteNonQuery(command);

            WriterCounters.Instance.DeleteFullPerSec.OperationFinished();
            return ret;
        }

        public override RemoteResult AsyncProcess(bool isDeleted, bool local, int countElemnts, Action<InnerData> process,
            Func<MetaData, bool> isMine, bool isFirstRead, ref object lastId)
        {
            var script = _metaDataCommandCreator.ReadWithDeleteAndLocal(isDeleted, local);
            return ProcessRestore(script, countElemnts, process, isMine, isFirstRead, ref lastId, isDeleted);
        }

        public override RemoteResult SelectRead(SelectDescription description, out SelectSearchResult searchResult)
        {
            var result = new List<SearchData>();

            description.IdDescription.PageSize = description.CountElements + 2;
            var command = _metaDataCommandCreator.CreateSelectCommand(description);

            if (command == null)
            {
                searchResult = new SelectSearchResult(new List<SearchData>(), true);
                return new InnerServerError(Errors.QueryError);
            }

            var reader = _implModule.CreateReader(command);

            reader.Start();

            if (reader.IsFail)
            {
                reader.Dispose();
                searchResult = new SelectSearchResult(result, true);
                return new InnerFailResult("script error");
            }

            while (reader.IsCanRead && result.Count < description.CountElements)
            {
                reader.ReadNext();

                var fields = _metaDataCommandCreator.SelectProcess(reader);

                var key = fields.Find(x => x.Item2.ToLower() == description.IdDescription.AsFieldName.ToLower());
                result.Add(new SearchData(fields, key.Item1));
            }
            bool isAllDataRead = !reader.IsCanRead;

            reader.Dispose();
            searchResult = new SelectSearchResult(result, isAllDataRead);

            return new SuccessResult();
        }

        public override RemoteResult RestoreUpdate(InnerData obj, bool local)
        {
            object key;
            object value;
            DeserializeData(obj, out key, out value);

            var meta = ReadMetaData(obj);

            return _createAndUpdate.UpdateRestore(obj, local, meta, key, value);
        }

        public override RemoteResult CustomOperation(InnerData obj, bool local)
        {
            object key;
            DeserializeKey(obj, out key);

            var connection = _implModule.RentConnectionInner();
            RemoteResult ret;
            try
            {
                ret = _userCommandCreator.CustomOperation(connection.Element, (TKey) key, obj.Data,
                    obj.Transaction.CustomOperationField);
            }
            catch (Exception e)
            {
                Logger.Logger.Instance.Warn("Custom operation error: " + e);
                ret = new InnerFailResult(e.Message);
            }
            connection.Dispose();

            return ret;
        }

        public override InnerData ReadExternal(InnerData obj)
        {
            object key;            
            DeserializeKey(obj, out key);

            var ret = new InnerData(new Transaction(obj.Transaction))
            {
                Key = obj.Key
            };

            return ReadInner(key, ret);
        }

        private InnerData ReadInner(object key, InnerData ret, bool isDeleted = false)
        {
            var timer = WriterCounters.Instance.ReadTimer.StartNew();

            var script = _userCommandCreator.Read();
            script = _metaDataCommandCreator.ReadWithDelete(script, isDeleted, key);            

            var reader = _implModule.CreateReader(script);

            try
            {
                reader.Start();

                if (reader.IsFail)
                {
                    ret.Data = null;
                    ret.Transaction.SetError();
                    ret.Transaction.AddErrorDescription("Script error");
                    reader.Dispose();
                    return ret;
                }

                if (reader.IsCanRead)
                {
                    reader.ReadNext();

                    TKey tmp;
                    object value = _userCommandCreator.ReadObjectFromReader(reader, out tmp);
                    ret.Data = value == null ? null : _hashCalculater.SerializeValue(value);
                }
            }
            catch (Exception e)
            {
                Logger.Logger.Instance.Error(e, "");
                ret.Data = null;

                ret.Transaction.SetError();
                ret.Transaction.AddErrorDescription(e.Message);
            }

            reader.Dispose();
            timer.Complete();
            return ret;
        }

        #endregion

        #region Rollback

        public override RemoteResult CreateRollback(InnerData obj, bool local)
        {
            var meta = ReadMetaData(obj);

            if (meta.Item1 == null || meta.Item1.IsDeleted)
                return new SuccessResult();

            return CreateRollbackInner(obj);
        }

        private RemoteResult CreateRollbackInner(InnerData obj)
        {
            object key;
            DeserializeKey(obj, out key);

            var command = _metaDataCommandCreator.DeleteMetaData(key);
            _implModule.ExecuteNonQuery(command);

            command = _userCommandCreator.Delete((TKey) key);
            var result = _implModule.ExecuteNonQuery(command);

            return result;
        }

        public override RemoteResult UpdateRollback(InnerData obj, bool local)
        {
            //TODO тут тоже доделать после определения версионности
            return new SuccessResult();
        }

        public override RemoteResult DeleteRollback(InnerData obj, bool local)
        {
            object key;
            DeserializeKey(obj, out key);

            var metaCommand = _metaDataCommandCreator.SetDataNotDeleted(key);
            return _implModule.ExecuteNonQuery(metaCommand);
        }

        public override RemoteResult CustomOperationRollback(InnerData obj, bool local)
        {
            object key;
            DeserializeKey(obj, out key);

            var connection = _implModule.RentConnectionInner();

            RemoteResult ret;
            try
            {
                ret = _userCommandCreator.CustomOperationRollback(connection.Element, (TKey) key,
                    obj.Data, obj.Transaction.CustomOperationField);
            }
            catch (Exception e)
            {
                Logger.Logger.Instance.Warn("Custom operation error: " + e);
                ret = new InnerFailResult(e.Message);
            }
            connection.Dispose();
            return ret;
        }

        #endregion

        #region private

        private void DeserializeData(InnerData data, out object key, out object value)
        {
            DeserializeKey(data, out key);

            value = null;
            if (data.Data != null)
                DeserializeValue(data, out value);
        }

        private void DeserializeKey(InnerData data, out object key)
        {
            key = _hashCalculater.DeserializeKey(data.Key);
        }

        private void DeserializeValue(InnerData data, out object value)
        {
            value = _hashCalculater.DeserializeValue(data.Data);
        }

        #endregion

        #region Restore

        private void ReadDataList(List<MetaData> ids, bool isDeleted, Action<InnerData> process, int threadsCount)
        {
            threadsCount = Math.Min(ids.Count, threadsCount);
            var threads = new Task[threadsCount];

            for (int j = 0; j < threadsCount; j++)
            {
                int j1 = j;
                var task = Task.Factory.StartNew(() =>
                {                                        
                    try
                    {
                        Logger.Logger.Instance.DebugFormat("Start thread {0}", j1);

                        int start = j1*ids.Count/threadsCount;
                        int end = (j1+1)*ids.Count/threadsCount;
                        
                        var list = ids.GetRange(start, end-start);                        
                        var ret = ReadInnerList(list, isDeleted);
                        foreach (var data in ret)
                        {                            
                            process(data);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Logger.Instance.ErrorFormat("Fail in thread = {0}", e);
                        throw;
                    }
                    Logger.Logger.Instance.DebugFormat("Finish thread {0}", j1);
                });

                threads[j] = task;
            }

            Task.WaitAll(threads);
            
            for (int j = 0; j < threadsCount; j++)
            {
                threads[j].Dispose();
            }
        }

        private List<InnerData> ReadInnerList(List<MetaData> ids, bool isDeleted)
        {
            var command = _metaDataCommandCreator.ReadWithDeleteAndLocalList(_userCommandCreator.Read(), isDeleted,
                ids.Select(x => x.Id).ToList());

            var reader = _implModule.CreateReader(command);

            try
            {
                reader.Start();

                if (reader.IsFail)
                {
                    return new List<InnerData>();
                }
                var ret = new List<InnerData>();
                
                while (reader.IsCanRead)
                {
                    reader.ReadNext();
                    
                    TKey tmp;
                    object value = _userCommandCreator.ReadObjectFromReader(reader, out tmp);

                    var meta = ids.Find(x => x.Id.Equals(tmp));

                    var data = new InnerData(new Transaction(meta.Hash, "default"))
                    {
                        Data = value == null ? null : _hashCalculater.SerializeValue(value),
                        MetaData = meta,
                        Key = _hashCalculater.SerializeKey(tmp),
                        Transaction = {TableName = TableName}
                    };
                    ret.Add(data);
                }
                return ret;
            }
            catch (Exception e)
            {
                Logger.Logger.Instance.Error(e, "");
            }
            finally
            {
                reader.Dispose();
            }
            return new List<InnerData>();
        }


        private RemoteResult ProcessRestore(string script, int countElemnts, Action<InnerData> process,
             Func<MetaData, bool> isMine, bool isFirstAsk, ref object lastId, bool isDeleted)
        {
            bool isAllDataRead = true;
            var keys = ReadMetaDataUsingSelect(script, countElemnts, isFirstAsk, ref lastId, isMine, ref isAllDataRead);

            ReadDataList(keys, isDeleted, process, 10);

            if (!isAllDataRead)
                return new SuccessResult();

            return new FailNetResult("");
        }

        private FieldDescription PrepareKeyDescription(int countElements, bool isfirstAsk, object lastId)
        {
            var idDescription = _metaDataCommandCreator.GetKeyDescription();
            idDescription.PageSize = countElements;
            idDescription.IsFirstAsk = isfirstAsk;

            if (!isfirstAsk)
                idDescription.Value = lastId;
            else
            {
                idDescription.Value = idDescription.SystemFieldType.IsValueType
                    ? Activator.CreateInstance(idDescription.SystemFieldType)
                    : null;
            }

            return idDescription;
        }

        private List<MetaData> ReadMetaDataUsingSelect(string script, int countElements, bool isfirstAsk, ref object lastId,
            Func<MetaData, bool> isMine, ref bool isAllDataRead)
        {
            var list = new List<MetaData>();
            var idDescription = PrepareKeyDescription(countElements, isfirstAsk, lastId);
            SelectSearchResult result;

            int count = 0;

            var select = new SelectDescription(idDescription, script, countElements, new List<FieldDescription>());
            var ret = SelectRead(select, out result);

            while (!ret.IsError)
            {
                bool exit = false;
                foreach (var searchData in result.Data)
                {
                    var meta = _metaDataCommandCreator.ReadMetaFromSearchData(searchData);

                    WriterCounters.Instance.RestoreCheckPerSec.OperationFinished();
                    WriterCounters.Instance.RestoreCheckCount.Increment();
                    if (isMine(meta))
                    {
                        list.Add(meta);
                        count++;
                    }

                    lastId = meta.Id;

                    if (count == countElements)
                    {
                        exit = true;
                        break;
                    }
                }

                if (result.IsAllDataRead || exit)
                    break;

                idDescription = PrepareKeyDescription(countElements, false, lastId);
                select = new SelectDescription(idDescription, script, countElements, new List<FieldDescription>());
                ret = SelectRead(select, out result);
            }

            isAllDataRead = result.IsAllDataRead;

            return list;
        }

        #endregion

        protected override void Dispose(bool isUserCall)
        {
            if (isUserCall)
            {
                _implModule.Dispose();
            }
            base.Dispose(isUserCall);
        }
    }
}

