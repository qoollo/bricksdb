using System.Runtime.Serialization;
using Qoollo.Impl.Collector.Tasks;
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
                
        [DataMember]
        public byte[] Data { get; set; }
        
        [DataMember]
        public byte[] Key { get; set; }

        public MetaData MetaData { get; set; }

        public DistributorData DistributorData { get; set; }

        public InnerData(Transaction transaction)
        {
            Transaction = transaction;            
        }                
    }
}
