using System;
using System.Collections.Generic;
using Qoollo.Impl.Common.Server;

namespace Qoollo.Impl.Common.Data.DataTypes
{    
    public class MetaData
    {
        public MetaData(bool isLocal, DateTime? deleteTime, bool isDeleted, string hash)
        {
            IsDeleted = isDeleted;
            DeleteTime = deleteTime;
            IsLocal = isLocal;     
            Hash = hash;
        }

        public MetaData(object key)
        {
            Id = key;            
        }

        public object Value { get; set; }
        public bool IsLocal { get; private set; }

        public DateTime? DeleteTime { get; private set; }
        
        public bool IsDeleted { get; private set; }
        
        public object Id { get; set; }        
        public string Hash { get; private set; }      
        
        /// <summary>
        /// Server for restore process
        /// </summary>
        public List<RestoreServer> ServersToSend { get; set; }   
    }
}
