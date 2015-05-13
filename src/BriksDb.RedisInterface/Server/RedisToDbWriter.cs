using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BricksDb.RedisInterface.Server
{
    class RedisToDbWriter:RedisToSmthSystem
    {
        public RedisToDbWriter()
        {
        }

        protected override void InnerBuild(RedisMessageProcessor processor)
        {
            throw new NotImplementedException();
        }
    }
}
