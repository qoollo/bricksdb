using System;
using System.Diagnostics.Contracts;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.NetResults.Inner;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Modules.Db.Impl;
using Qoollo.Impl.Writer.Db.Commands;
using Qoollo.Impl.Writer.PerfCounters;

namespace Qoollo.Impl.Writer.Db
{
    class DbLogicCreateAndUpdateHelper<TCommand, TKey, TValue, TConnection, TReader>
        where TConnection : class
    {
        private readonly IUserCommandCreator<TCommand, TConnection, TKey, TValue, TReader> _userCommandCreator;
        private readonly IMetaDataCommandCreator<TCommand, TReader> _metaDataCommandCreator;
        private readonly DbImplModule<TCommand, TConnection, TReader> _implModule;

        public DbLogicCreateAndUpdateHelper(
            IUserCommandCreator<TCommand, TConnection, TKey, TValue, TReader> userCommandCreator,
            IMetaDataCommandCreator<TCommand, TReader> metaDataCommandCreator,
            DbImplModule<TCommand, TConnection, TReader> implModule)
        {
            Contract.Requires(userCommandCreator != null);
            Contract.Requires(implModule != null);
            Contract.Requires(metaDataCommandCreator != null);
            _userCommandCreator = userCommandCreator;
            _metaDataCommandCreator = metaDataCommandCreator;
            _implModule = implModule;
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

            return new InnerServerError(Errors.DataAlreadyExists);
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
                SetMetadataNotDeleted(key);

            return UpdateInner(local, key, value);
        }

        private RemoteResult CreateWhenDataDeleted(bool local, object key, object value)
        {
            var ret = SetMetadataNotDeleted(key);

            if (IsError(ref ret))
                return ret;            

            return UpdateInner(local, key, value);
        }

        private RemoteResult CreateDataWithoutMetaData(object key, object value)
        {
            var command = _userCommandCreator.Create((TKey) key, (TValue) value);
            var ret = _implModule.ExecuteNonQuery(command);

            IsError(ref ret);
            return ret;
        }

        private RemoteResult CreateMetaUpdateData(InnerData obj, bool local, object key, object value)
        {
            var metaCommand = _metaDataCommandCreator.CreateMetaData(local, obj.Transaction.DataHash, key);
            var ret = _implModule.ExecuteNonQuery(metaCommand);

            if (!IsError(ref ret))
            {
                var command = _userCommandCreator.Update((TKey) key, (TValue) value);
                ret = _implModule.ExecuteNonQuery(command);
                IsError(ref ret);
            }
            return ret;
        }

        private RemoteResult CreateInner(InnerData obj, bool local, object key, object value)
        {
            var timer = WriterCounters.Instance.CreateTimer.StartNew();

            var command = _userCommandCreator.Create((TKey)key, (TValue)value);
            var ret = _implModule.ExecuteNonQuery(command);

            if (!ret.IsError)
            {
                var metaTimer = WriterCounters.Instance.CreateMetaDataTimer.StartNew();

                var metaCommand = _metaDataCommandCreator.CreateMetaData(local, obj.Transaction.DataHash, key);
                ret = _implModule.ExecuteNonQuery(metaCommand);

                metaTimer.Complete();
            }

            IsError(ref ret);

            timer.Complete();
            return ret;
        }

        private RemoteResult UpdateInner(bool local, object key, object value)
        {
            var command = _userCommandCreator.Update((TKey)key, (TValue)value);
            var ret = _implModule.ExecuteNonQuery(command);

            if (!ret.IsError)
            {
                command = _metaDataCommandCreator.UpdateMetaData(local, key);                
                ret = _implModule.ExecuteNonQuery(command);
            }

            IsError(ref ret);
            return ret;
        }

        private RemoteResult SetMetadataNotDeleted(object key)
        {
            var metaCommand = _metaDataCommandCreator.SetDataNotDeleted(key);            
            return _implModule.ExecuteNonQuery(metaCommand);
        }

        private bool IsError(ref RemoteResult result)
        {
            var ret = result.IsError;
            if(ret)
                result = new InnerServerError(result);
            return ret;
        }
    }
}
