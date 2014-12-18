using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using Qoollo.Impl.Collector;

namespace Qoollo.Client.CollectorGate
{
    public class StorageDbReader:DbDataReader
    {
        private readonly SelectReader _reader;

        internal StorageDbReader(SelectReader reader)
        {
            _reader = reader;
        }

        public SystemSearchState SystemSearchState {get { return (SystemSearchState) _reader.SearchState; }}

        #region Not implement
        
        public override Type GetFieldType(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override string GetName(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override int GetValues(object[] values)
        {
            throw new NotImplementedException();
        }

        public override bool IsDBNull(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override int GetOrdinal(string name)
        {
            throw new NotImplementedException();
        }

        public override bool GetBoolean(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override byte GetByte(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
        {
            throw new NotImplementedException();
        }

        public override char GetChar(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
        {
            throw new NotImplementedException();
        }

        public override Guid GetGuid(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override short GetInt16(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override int GetInt32(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override long GetInt64(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override DataTable GetSchemaTable()
        {
            throw new NotImplementedException();
        }

        public override string GetDataTypeName(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public override int Depth
        {
            get { throw new NotImplementedException(); }
        }

        public override bool IsClosed
        {
            get { throw new NotImplementedException(); }
        }

        public override int RecordsAffected
        {
            get { throw new NotImplementedException(); }
        }

        public override DateTime GetDateTime(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override string GetString(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override decimal GetDecimal(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override double GetDouble(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override float GetFloat(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override bool HasRows
        {
            get { throw new NotImplementedException(); }
        }

        #endregion
     
        public override object GetValue(int ordinal)
        {
            return _reader.GetValue(ordinal);
        }

        public override void Close()
        {
            _reader.Dispose();
        }

        public override bool NextResult()
        {
            _reader.ReadNext();
            return _reader.IsCanRead;
        }

        public override bool Read()
        {
            _reader.ReadNext();
            return _reader.IsCanRead;
        }      

        public override int FieldCount
        {
            get { return  _reader.CountFields(); }
        }      

        public override object this[int ordinal]
        {
            get { return _reader.GetValue(ordinal); }
        }

        public override object this[string name]
        {
            get { return _reader.GetValue(name); }
        }
    }
}
