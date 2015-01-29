using System;
using System.Collections.Generic;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Modules.Db.Impl;

namespace Qoollo.Impl.Writer.Db.Commands
{
    internal interface IMetaDataCommandCreator<TCommand, TReader>
    {
        void SetKeyName(string keyName);
        void SetTableName(List<string> tableName);

        TCommand InitMetaDataDb(string idInit);
        TCommand CreateMetaData(bool remote);
        TCommand DeleteMetaData();
        TCommand UpdateMetaData(bool local);
        TCommand SetDataDeleted();
        TCommand SetDataNotDeleted();

        TCommand ReadMetaData(TCommand userRead);
        Tuple<MetaData, bool> ReadMetaDataFromReader(DbReader<TReader> reader, bool readuserId = true);
        MetaData ReadMetaFromSearchData(SearchData data);
        string ReadWithDeleteAndLocal(bool isDelete, bool local);

        TCommand ReadWithDelete(TCommand userRead, bool isDelete);
        TCommand ReadWithDeleteAndLocal(TCommand userRead, bool isDelete, bool local);

        TCommand CreateSelectCommand(string script, FieldDescription idDescription,
            List<FieldDescription> userParameters);

        TCommand CreateSelectCommand(TCommand script, FieldDescription idDescription,
            List<FieldDescription> userParameters);

        List<Tuple<object, string>> SelectProcess(DbReader<TReader> reader);

        Dictionary<string, Type> GetFieldsDescription();
        TCommand SetKeytoCommand(TCommand command, object key);
        FieldDescription GetKeyDescription();        
    }
}
