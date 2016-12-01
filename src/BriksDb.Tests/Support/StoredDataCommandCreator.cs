using System;
using System.Collections.Generic;
using System.Data;
using Npgsql;
using NpgsqlTypes;
using Qoollo.Impl.Common;
using Qoollo.Impl.Postgre;

namespace Qoollo.Tests.Support
{
    public class StoredDataCommandCreator:PostgreUserCommandCreator<int, StoredData>
    {
        public override bool CreateDb(NpgsqlConnection connection)
        {
            throw new NotImplementedException();
        }

        public override string GetKeyName()
        {
            return "Id";
        }

        public override List<string> GetTableNameList()
        {
            return new List<string> {"TestStored"};
        }

        public override string GetKeyInitialization()
        {
            return "id integer";
        }

        public override void GetFieldsDescription(DataTable dataTable)
        {
            dataTable.Rows.Add("Id", typeof(int), NpgsqlDbType.Integer);
        }

        public override NpgsqlCommand Create(int key, StoredData value)
        {
            var command = new NpgsqlCommand("insert into TestStored(id) values(@id) ");
            command.Parameters.AddWithValue("@id", NpgsqlDbType.Integer, key);

            return new NpgsqlCommand();
        }

        public override NpgsqlCommand Update(int key, StoredData value)
        {
            return new NpgsqlCommand();
        }

        public override NpgsqlCommand Delete(int key)
        {
            return new NpgsqlCommand("delete from TestStored ");
        }

        public override NpgsqlCommand Read()
        {
            return new NpgsqlCommand("select id from TestStored");
        }

        public override StoredData ReadObjectFromSearchData(List<Tuple<object, string>> fields)
        {
            return new StoredData((int)fields[0].Item1);
        }

        public override CustomOperationResult CustomOperationRollback(NpgsqlConnection connection, int key, byte[] value, string description)
        {
            throw new NotImplementedException();
        }

        public override CustomOperationResult CustomOperation(NpgsqlConnection connection, int key, byte[] value, string description)
        {
            throw new NotImplementedException();
        }

        public override StoredData ReadObjectFromReader(NpgsqlDataReader reader, out int key)
        {
            key = 1;
            return new StoredData(1);
        }
    }
}