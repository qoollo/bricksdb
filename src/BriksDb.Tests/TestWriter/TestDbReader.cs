using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Impl.Modules.Db.Impl;

namespace Qoollo.Tests.TestWriter
{
    internal class TestDbReader : DbReader<TestDbReader>
    {
        public List<int> Data;
        public List<TestCommand> Meta;

        public int CurrentIndex = -1;
        private bool _readMode = false;
        private bool _metaMode = false;

        public TestDbReader(List<int> data, List<TestCommand> meta, bool readMode = false, bool metamode = false)
        {
            this._readMode = readMode;
            Data = new List<int>(data);
            Meta = new List<TestCommand>(meta);
            _metaMode = metamode;
        }

        public TestDbReader(int data, TestCommand meta, bool readMode = false)
        {
            this._readMode = readMode;
            Data = new List<int> { data };

            Meta = new List<TestCommand> { meta };
        }

        protected override object GetValueInner(int index)
        {
            if (_metaMode)
                return Meta[CurrentIndex];

            if (_readMode)
                return Data[CurrentIndex];

            if (index == 0)
                return Data[CurrentIndex];

            return Meta[CurrentIndex];
        }

        protected override object GetValueInner(string index)
        {
            return Data[CurrentIndex];
        }

        protected override bool IsValidRead()
        {
            return true;
        }

        public override TestDbReader Reader
        {
            get { return this; }
        }

        public override bool IsCanRead
        {
            get { return CurrentIndex < Data.Count - 1; }
        }

        protected override int CountFieldsInner()
        {
            return 1;
        }

        protected override void ReadNextInner()
        {
            CurrentIndex++;
        }

        protected override void StartInner()
        {
        }
    }
}
