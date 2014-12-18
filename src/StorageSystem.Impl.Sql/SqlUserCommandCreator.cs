using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace Qoollo.Impl.Sql
{
    public abstract class SqlUserCommandCreator<TKey, TValue>
    {
        /// <summary>
        /// Init Db
        /// </summary>
        /// <param name="connection">Dont close connection</param>
        /// <returns>False - if was error
        /// Is db exists - return True</returns>
        public abstract bool CreateDb(SqlConnection connection);

        /// <summary>
        /// Get primary key name
        /// </summary>
        /// <returns></returns>
        public abstract string GetKeyName();

        /// <summary>
        /// Get main table name
        /// </summary>
        /// <returns></returns>
        public abstract List<string> GetTableNameList();

        /// <summary>
        /// Get primary key initialization
        /// </summary>
        /// <returns></returns>
        public abstract string GetKeyInitialization();

        /// <summary>
        /// Get fields description that will be searched        
        /// field name / type / type in db
        /// </summary>
        /// <returns></returns>
        public abstract void GetFieldsDescription(DataTable dataTable);

        public abstract SqlCommand Create(TKey key, TValue value);

        public abstract SqlCommand Update(TKey key, TValue value);

        public abstract SqlCommand Delete(TKey key);

        public abstract SqlCommand Read();

        /// <summary>
        /// Read object
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract TValue ReadObjectFromReader(SqlDataReader reader, out TKey key);

        public abstract TValue ReadObjectFromSearchData(List<Tuple<object, string>> fields);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection">Dont close connection</param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public abstract CustomOperationResult CustomOperation(SqlConnection connection, TKey key, byte[] value,
            string description);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection">Dont close connection</param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public abstract CustomOperationResult CustomOperationRollback(SqlConnection connection, TKey key,
            byte[] value, string description);
    }
}
