using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Qoollo.Impl.Writer.Db.Commands;

namespace Qoollo.Impl.Collector.Parser
{
    internal class UserCommandsHandler<TCommand, TType, TConnection, TKey, TValue, TReader> : IUserCommandsHandler
    {
        private IUserCommandCreator<TCommand, TConnection, TKey, TValue, TReader> _userCommandCreator;
        private IMetaDataCommandCreator<TCommand, TReader> _metaDataCommandCreator;
        private UserCommandCreatorBase<TCommand, TConnection, TType, TKey, TValue, TReader> _commandCreatorBase;

        public UserCommandsHandler(IUserCommandCreator<TCommand, TConnection, TKey, TValue, TReader> userCommandCreator,
            IMetaDataCommandCreator<TCommand, TReader> metaDataCommandCreator)
        {
            _userCommandCreator = userCommandCreator;
            _metaDataCommandCreator = metaDataCommandCreator;
            _commandCreatorBase = new UserCommandCreatorBase<TCommand, TConnection, TType, TKey, TValue, TReader>();
        }

        public List<Tuple<string, Type>> GetDbFieldsDescription()
        {
            var dt = _commandCreatorBase.GetFieldsDescription(_userCommandCreator);
            return
                (from DataRow row in dt.Rows select new Tuple<string, Type>((string) row["id"], (Type) row["type"]))
                    .ToList();
        }

        public List<Tuple<string, Type, TType>> GetFieldsDescription()
        {
            var dt = _commandCreatorBase.GetFieldsDescription(_userCommandCreator);
            return
                (from DataRow row in dt.Rows
                    select new Tuple<string, Type, TType>((string) row["id"], (Type) row["type"], (TType) row["dtype"]))
                    .ToList();
        }

        public Dictionary<string, Type> GetMetaDescription()
        {
            return _metaDataCommandCreator.GetFieldsDescription();
        }

        public string GetKeyName()
        {
            return _userCommandCreator.GetKeyName();
        }
    }
}
