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

        TCommand CreateMetaData(bool remote, string dataHash, object key);
        TCommand DeleteMetaData(object key);
        TCommand UpdateMetaData(bool local, object key);
        TCommand SetDataDeleted(object key);
        TCommand SetDataNotDeleted(object key);

        TCommand ReadMetaData(TCommand userRead, object key);

        Tuple<MetaData, bool> ReadMetaDataFromReader(DbReader<TReader> reader, bool readuserId = true);
        MetaData ReadMetaFromSearchData(SearchData data);
        string ReadWithDeleteAndLocal(bool isDelete, bool local);

        TCommand ReadWithDelete(TCommand userRead, bool isDelete, object key);
        TCommand ReadWithDeleteAndLocal(TCommand userRead, bool isDelete, bool local);

        TCommand CreateSelectCommand(string script, FieldDescription idDescription,
            List<FieldDescription> userParameters);

        TCommand CreateSelectCommand(SelectDescription description);

        TCommand CreateSelectCommand(TCommand script, FieldDescription idDescription,
            List<FieldDescription> userParameters);

        List<Tuple<object, string>> SelectProcess(DbReader<TReader> reader);

        Dictionary<string, Type> GetFieldsDescription();
        
        FieldDescription GetKeyDescription();
    }
}
