using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Tests.Support
{
    [Serializable]
    class StoredData
    {
        public int Id { get; set; }

        public StoredData(int id)
        {
            Id = id;
        }
    }
}
