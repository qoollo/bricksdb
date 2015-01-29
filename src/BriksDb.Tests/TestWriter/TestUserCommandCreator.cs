using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Impl.Common;
using Qoollo.Impl.Modules.Db.Impl;
using Qoollo.Impl.Writer.Db.Commands;

namespace Qoollo.Tests.TestWriter
{
    internal class TestUserCommandCreator : IUserCommandCreator<TestCommand, TestCommand, int, int, TestDbReader>
    {
        private readonly string _tableName;

        public TestUserCommandCreator()
        {
            _tableName = "";
        }

        public TestUserCommandCreator(string tableName)
        {
            _tableName = tableName;
        }

        public TestCommand CreateDb()
        {
            return new TestCommand();
        }

        public bool CreateDb(TestCommand connection)
        {
            return true;
        }

        public string GetKeyName()
        {
            return "id";
        }

        public List<string> GetTableNameList()
        {
            if (_tableName == "")
                return new List<string>() { "Int" };
            return new List<string>() { _tableName };
        }

        public string GetKeyInitialization()
        {
            return "";
        }

        public void GetFieldsDescription(DataTable dataTable)
        {
            dataTable.Rows.Add(new object[] { "id", typeof(int), typeof(int) });
            dataTable.Rows.Add(new object[] { "date", typeof(int), typeof(int) });
            dataTable.Rows.Add(new object[] { "str", typeof(string), typeof(string) });
        }


        public int GetDefaultKey()
        {
            return 0;
        }

        public TestCommand Create(int key, int value)
        {
            return new TestCommand() { Command = "create", Value = (int)value };
        }

        public TestCommand Update(int key, int value)
        {
            return new TestCommand() { Command = "update", Value = (int)value };
        }

        public TestCommand Delete(int key)
        {
            return new TestCommand() { Command = "delete", Value = (int)key };
        }

        public TestCommand Read()
        {
            return new TestCommand() { Command = "read" };
        }

        public int ReadObjectFromReader(DbReader<TestDbReader> reader, out int key)
        {
            key = (int)reader.GetValue(0);
            return key;
        }

        public int ReadObjectFromSearchData(List<Tuple<object, string>> fields)
        {
            var key = (int)fields[0].Item1;
            return key;
        }


        public RemoteResult CustomOperation(TestCommand connection, int key, byte[] value, string description)
        {
            throw new NotImplementedException();
        }

        public RemoteResult CustomOperationRollback(TestCommand connection, int key, byte[] value, string description)
        {
            throw new NotImplementedException();
        }
    }
}
