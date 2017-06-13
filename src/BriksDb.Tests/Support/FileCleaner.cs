using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Tests.Support
{
    internal class FileCleaner : IDisposable
    {
        private readonly string _filename;
        private readonly bool _isDebug;
        public FileCleaner(string filename, bool isDebug = false)
        {
            _filename = filename;
            _isDebug = isDebug;
            RemoveFile(filename);
        }

        private void RemoveFile(string filename)
        {
            File.Delete(filename);
        }

        public void Dispose()
        {
            if (!_isDebug)
                RemoveFile(_filename);
        }
    }
}
