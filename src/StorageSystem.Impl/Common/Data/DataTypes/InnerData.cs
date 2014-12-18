using System.Runtime.Serialization;
using Qoollo.Impl.Common.Data.TransactionTypes;

namespace Qoollo.Impl.Common.Data.DataTypes
{
    [DataContract]
    public class InnerData
    {
        /// <summary>
        /// Data transaction (state)
        /// </summary>
        [DataMember]
        public Transaction Transaction { get; set; }
        /// <summary>
        /// Serialized data
        /// </summary>
        [DataMember]
        public byte[] Data { get; set; }
        /// <summary>
        /// Serialized key
        /// </summary>
        [DataMember]
        public byte[] Key { get; set; }
        /// <summary>
        /// Metadata
        /// </summary>
        [DataMember]
        public MetaData MetaData { get; set; }

        public InnerData(Transaction transaction)
        {
            Transaction = transaction;            
        }                
    }
}
