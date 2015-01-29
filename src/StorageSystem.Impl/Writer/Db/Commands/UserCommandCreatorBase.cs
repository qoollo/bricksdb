using System;
using System.Data;

namespace Qoollo.Impl.Writer.Db.Commands
{
    internal class UserCommandCreatorBase<TCommand, TConnection, TType, TKey, TValue, TReader>
    {
        private DataTable _dataTable;
        private bool _isRead ;

        public UserCommandCreatorBase()
        {
            _isRead = false;
            _dataTable = new DataTable();

            _dataTable.Columns.Add("id", typeof (string));
            _dataTable.Columns.Add("type", typeof(Type));
            _dataTable.Columns.Add("dtype", typeof(TType));
        }


        public DataTable GetFieldsDescription(
            IUserCommandCreator<TCommand, TConnection, TKey, TValue, TReader> userCommandCreator)
        {
            if (_isRead)
                return _dataTable;

            _isRead = true;

            userCommandCreator.GetFieldsDescription(_dataTable);

            return _dataTable;
        }
    }
}
