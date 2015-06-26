using System;
using System.Collections.Generic;
using System.Linq;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Modules.Db.Impl;
using Qoollo.Impl.Writer.Db.Commands;

namespace Qoollo.Tests.TestWriter
{
    internal class TestMetaDataCommandCreator : IMetaDataCommandCreator<TestCommand, TestDbReader>
    {
        public void SetKeyName(string keyName)
        {
        }

        public void SetTableName(List<string> tableName)
        {
        }

        public TestCommand InitMetaDataDb(string idInit)
        {
            return new TestCommand();
        }

        public TestCommand CreateMetaData(bool remote, string dataHash)
        {
            return new TestCommand { Command = "createmeta", Local = remote , Hash = dataHash};
        }

        public TestCommand DeleteMetaData()
        {
            return new TestCommand { Command = "deletemeta" };
        }

        public TestCommand UpdateMetaData(bool local)
        {
            return new TestCommand { Command = "updatemeta", Local = local };
        }

        public TestCommand SetDataDeleted()
        {
            return new TestCommand { Command = "setdatadeleted" };
        }

        public TestCommand SetDataNotDeleted()
        {
            return new TestCommand { Command = "setdatanotdeleted" };
        }

        public TestCommand ReadMetaData(TestCommand userRead)
        {
            return new TestCommand { Command = "readMeta" };
        }

        Tuple<MetaData, bool> IMetaDataCommandCreator<TestCommand, TestDbReader>.ReadMetaDataFromReader(
            DbReader<TestDbReader> reader, bool readuserId)
        {
            var command = (TestCommand) reader.GetValue(1);
            return new Tuple<MetaData, bool>(
                    new MetaData(command.Local, command.DeleteTime, command.IsDeleted, command.Hash), false);
        }

        public string ReadWithDeleteAndLocal(bool isDelete, bool local)
        {
            return string.Format("ReadWithDeleteAndLocal%{0}%{1}", local, isDelete);
        }

        public TestCommand ReadWithDelete(TestCommand userRead, bool idDelete)
        {
            var ret = new TestCommand
            {
                Command = "ReadAllElementsAndMergeWhereStatemenetForKey",
                Local = true,
                Support = 10,
                IsDeleted = idDelete
            };

            return ret;
        }

        public TestCommand ReadWithDeleteAndLocal(TestCommand userRead, bool isDelete, bool local)
        {
            return new TestCommand { Command = string.Format("ReadWithDeleteAndLocal%{0}%{1}", local, isDelete) };
        }

        public TestCommand CreateSelectCommand(string script, FieldDescription idDescription, List<FieldDescription> userParameters)
        {
            return new TestCommand { Command = script, IsDeleted = false };
        }

        public TestCommand CreateSelectCommand(TestCommand script, FieldDescription idDescription, List<FieldDescription> userParameters)
        {
            return new TestCommand { Command = script.Command, IsDeleted = false };
        }

        public Dictionary<string, Type> GetFieldsDescription()
        {
            return new Dictionary<string, Type>
            {
                {"id", typeof(int)},
                {"isdeleted", typeof(bool)},
                {"local", typeof(bool)},
                {"deletetime", typeof(DateTime)},
            };
        }

        public TestCommand SetKeytoCommand(TestCommand command, object key)
        {
            return new TestCommand
            {
                Command = command.Command,
                Value = key == null ? -1 : (int) key,
                Support = command.Support,
                IsDeleted = command.IsDeleted,
                Local = command.Local,
                Hash = command.Hash
            };
        }

        public List<Tuple<object, string>> SelectProcess(DbReader<TestDbReader> reader)
        {
            var fields = new List<Tuple<object, string>>();

            for (int i = 0; i < reader.CountFields(); i++)
            {
                fields.Add(new Tuple<object, string>(((TestCommand)reader.GetValue(i)).Value, ""));
                fields.Add(new Tuple<object, string>(((TestCommand)reader.GetValue(i)).Local, "local"));
                fields.Add(new Tuple<object, string>(((TestCommand)reader.GetValue(i)).IsDeleted, "isdelete"));
                fields.Add(new Tuple<object, string>(((TestCommand)reader.GetValue(i)).DeleteTime, "time"));
                fields.Add(new Tuple<object, string>(((TestCommand)reader.GetValue(i)).Hash, "hash"));
            }

            return fields;
        }

        public FieldDescription GetKeyDescription()
        {
            return new FieldDescription("", typeof(int));
        }

        public MetaData ReadMetaFromSearchData(SearchData data)
        {
            var local = (bool)data.Fields.First(x => x.Item2 == "local").Item1;
            var isdelete = (bool)data.Fields.First(x => x.Item2 == "isdelete").Item1;
            var time = (DateTime)data.Fields.First(x => x.Item2 == "time").Item1;
            var hash = (string)data.Fields.First(x => x.Item2 == "hash").Item1;

            return new MetaData(local, time, isdelete, hash) {Id = data.Fields.First(x => x.Item2 == "").Item1};
        }
    }
}