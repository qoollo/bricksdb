using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.HashHelp;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Common.NetResults.Inner;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Modules.Db.Impl;
using Qoollo.Impl.Writer.Db.Commands;
using Qoollo.Impl.Writer.PerfCounters;

namespace Qoollo.Impl.Writer.Db
{
    class DbLogicCreateAndUpdateHelper<TCommand, TKey, TValue, TConnection, TReader>
         where TConnection : class
         where TCommand: IDisposable
    {
        private readonly IUserCommandCreator<TCommand, TConnection, TKey, TValue, TReader> _userCommandCreator;
        private readonly IMetaDataCommandCreator<TCommand, TReader> _metaDataCommandCreator;
        private readonly DbImplModule<TCommand, TConnection, TReader> _implModule;
        private readonly IHashCalculater _hashCalc;

        public DbLogicCreateAndUpdateHelper(
            IUserCommandCreator<TCommand, TConnection, TKey, TValue, TReader> userCommandCreator,
            IMetaDataCommandCreator<TCommand, TReader> metaDataCommandCreator,
            DbImplModule<TCommand, TConnection, TReader> implModule, IHashCalculater hashCalc)
        {
            Contract.Requires(userCommandCreator != null);
            Contract.Requires(implModule != null);
            Contract.Requires(metaDataCommandCreator != null);
            _userCommandCreator = userCommandCreator;
            _metaDataCommandCreator = metaDataCommandCreator;
            _implModule = implModule;
            _hashCalc = hashCalc;
        }

        public RemoteResult Create(InnerData obj, bool local, Tuple<MetaData, bool> meta, object key, object value)
        {
            if (meta.Item1 == null && meta.Item2)
                return CreateInner(obj, local, key, value);

            if (meta.Item1 == null && !meta.Item2)
                return CreateMetaUpdateData(obj, local, key, value);

            if (meta.Item2)
                return CreateDataWithoutMetaData(key, value);

            if (meta.Item1.IsDeleted)
                return CreateWhenDataDeleted(local, key, value);

            return new InnerFailResult(Errors.DataAlreadyExists, false);
        }

        public RemoteResult Update(InnerData obj, bool local, Tuple<MetaData, bool> meta, object key, object value)
        {
            //No meta, no data
            if (meta.Item1 == null && meta.Item2)
                return CreateInner(obj, local, key, value);

            //no meta, data exists
            if (meta.Item1 == null && !meta.Item2)
                return CreateMetaUpdateData(obj, local, key, value);

            // meta exists
            if (meta.Item2)
                return CreateDataWithoutMetaData(key, value);

            if (meta.Item1.IsDeleted)
                SetMetaDataNotDeleted(key);

            return UpdateInner(local, key, value);
        }

        public RemoteResult UpdateRestore(InnerData obj, bool local, Tuple<MetaData, bool> meta, object key, object value)
        {
            //No meta, no data
            if (meta.Item1 == null && meta.Item2)
                return CreateInner(obj, local, key, value);

            //no meta, data exists
            if (meta.Item1 == null && !meta.Item2)
                return CreateMetaUpdateData(obj, local, key, value);

            // meta exists
            if (meta.Item2)
                return CreateDataWithoutMetaData(key, value);

            if (meta.Item1.IsDeleted)
                SetMetaDataNotDeleted(key);

            //return UpdateInner(local, key, value);
            return UpdateMeta(local, key);
        }

        public RemoteResult UpdateRestorePackage(List<InnerData> datas, List<Tuple<MetaData, bool, object>> metadatas)
        {
            var dataForCreate = new List<InnerData>();
            var dataForCreateDataUpdateMeta = new List<InnerData>();
            var dataForCreateWithouMeta = new List<InnerData>();
            var dataMeta = new List<InnerData>();
            var dataUpdateMeta = new List<InnerData>();

            var fail = new List<InnerData>();
            foreach (var innerData in datas)
            {
                var key = DeserializeKey(innerData);

                var meta = metadatas.FirstOrDefault(x => x.Item3 == key);
                if (meta == null)
                    fail.Add(innerData);

                else
                {
                    var m = new MetaData(key);
                    innerData.MetaData = m;
                 
                    if (meta.Item1 == null && meta.Item2)
                    {
                        m.Value = DeserializeValue(innerData);
                        dataForCreate.Add(innerData);
                    }
                    else if (meta.Item1 == null && !meta.Item2)
                    {
                        m.Value = DeserializeValue(innerData);
                        dataForCreateDataUpdateMeta.Add(innerData);
                    }
                    else if (meta.Item2)
                    {
                        m.Value = DeserializeValue(innerData);
                        dataForCreateWithouMeta.Add(innerData);
                    }
                    else if (meta.Item1.IsDeleted)
                        dataMeta.Add(innerData);
                    else
                        dataUpdateMeta.Add(innerData);
                }
            }
            return new SuccessResult();
        }


        #region Create

        private RemoteResult CreateWhenDataDeleted(bool local, object key, object value)
        {
            var ret = SetMetaDataNotDeleted(key);

            if (IsError(ref ret))
                return ret;

            return UpdateInner(local, key, value);
        }

        private RemoteResult CreateDataWithoutMetaData(object key, object value)
        {
            using (var command = _userCommandCreator.Create((TKey)key, (TValue)value))
            {
                var ret = _implModule.ExecuteNonQuery(command);

                IsError(ref ret);
                return ret;
            }
        }

        private RemoteResult CreateMetaUpdateData(InnerData obj, bool local, object key, object value)
        {
            using (var metaCommand = _metaDataCommandCreator.CreateMetaData(local, obj.Transaction.DataHash, key))
            {
                var ret = _implModule.ExecuteNonQuery(metaCommand);

                if (!IsError(ref ret))
                {
                    using (var command = _userCommandCreator.Update((TKey)key, (TValue)value))
                    {
                        ret = _implModule.ExecuteNonQuery(command);
                        IsError(ref ret);
                    }
                }
                return ret;
            }
        }

        private RemoteResult CreateInner(InnerData obj, bool local, object key, object value)
        {
            var timer = WriterCounters.Instance.CreateTimer.StartNew();

            using (var command = _userCommandCreator.Create((TKey)key, (TValue)value))
            {
                var ret = _implModule.ExecuteNonQuery(command);

                if (!ret.IsError)
                {
                    var metaTimer = WriterCounters.Instance.CreateMetaDataTimer.StartNew();

                    using (var metaCommand = _metaDataCommandCreator.CreateMetaData(local, obj.Transaction.DataHash, key))
                    {
                        ret = _implModule.ExecuteNonQuery(metaCommand);
                    }
                    metaTimer.Complete();
                }

                IsError(ref ret);

                timer.Complete();
                return ret;
            }
        }

        #endregion

        #region Update

        private RemoteResult UpdateInner(bool local, object key, object value)
        {
            using (var command = _userCommandCreator.Update((TKey)key, (TValue)value))
            {
                var ret = _implModule.ExecuteNonQuery(command);

                if (!ret.IsError)
                    return UpdateMeta(local, key);

                IsError(ref ret);
                return ret;
            }
        }

        private RemoteResult UpdateMeta(bool local, object key)
        {
            using (var command = _metaDataCommandCreator.UpdateMetaData(local, key))
            {
                var ret = _implModule.ExecuteNonQuery(command);
                IsError(ref ret);
                return ret;
            }
        }

        #endregion

        #region Package

        //private RemoteResult CreateInner(List<InnerData> objects)
        //{
        //    var timer = WriterCounters.Instance.CreateTimer.StartNew();

        //    var command = _userCommandCreator.Create((TKey)key, (TValue)value);
        //    var ret = _implModule.ExecuteNonQuery(command);

        //    if (!ret.IsError)
        //    {
        //        var metaTimer = WriterCounters.Instance.CreateMetaDataTimer.StartNew();

        //        var metaCommand = _metaDataCommandCreator.CreateMetaData(local, obj.Transaction.DataHash, key);
        //        ret = _implModule.ExecuteNonQuery(metaCommand);

        //        metaTimer.Complete();
        //    }

        //    IsError(ref ret);            
        //    timer.Complete();
        //    return ret;
        //}

        #endregion

        private RemoteResult SetMetaDataNotDeleted(object key)
        {
            using (var metaCommand = _metaDataCommandCreator.SetDataNotDeleted(key))
            {
                return _implModule.ExecuteNonQuery(metaCommand);
            }
        }

        private bool IsError(ref RemoteResult result)
        {
            var ret = result.IsError;
            if (ret)
                result = new InnerServerError(result);
            return ret;
        }        

        private object DeserializeKey(InnerData data)
        {
            return _hashCalc.DeserializeKey(data.Key);
        }

        private object DeserializeValue(InnerData data)
        {
            return _hashCalc.DeserializeValue(data.Data);
        }
    }
}
