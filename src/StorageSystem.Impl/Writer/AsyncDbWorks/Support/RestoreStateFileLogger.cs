using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Impl.Writer.AsyncDbWorks.Support
{
    internal class RestoreStateFileLogger
    {        
        public RestoreStateFileLogger(string filename)
        {
            Contract.Requires(!string.IsNullOrEmpty(filename));
            _filename = filename;
        }

        private readonly string _filename;


    }
}
