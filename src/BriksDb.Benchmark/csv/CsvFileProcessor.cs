using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;

namespace Qoollo.Benchmark.csv
{
    class CsvFileProcessor:IDisposable
    {
        private readonly Dictionary<string, object> _currentRow;
        private readonly StreamWriter _sw;
        private readonly CsvWriter _writer;

        public CsvFileProcessor(string fileName)
        {            
            Contract.Requires(!string.IsNullOrEmpty(fileName));
            _currentRow = new Dictionary<string, object>();
            _sw = new StreamWriter(fileName);
            _writer = new CsvWriter(_sw);
        }

        public void RegistrateColumn(string columnName)
        {
            _currentRow.Add(columnName.Trim().ToLower(), string.Empty);
        }

        public void Write(string columnName, object value)
        {
            _currentRow[columnName.Trim().ToLower()] = value;
        }

        private void Write(IEnumerable<object> values)
        {
            foreach (var o in values)
            {
                _writer.WriteField(o);
            }
            _writer.NextRecord();
            _sw.Flush();
        }

        public void WriteToFile()
        {
            Write(_currentRow.Values);
            ClearRow();
        }

        public void Start()
        {
            Write(_currentRow.Keys);
        }
        
        private void ClearRow()
        {
            foreach (var pair in _currentRow)
            {
                _currentRow[pair.Key] = string.Empty;
            }
        }

        public void Dispose()
        {         
            _sw.Close();
        }
    }
}
