using System.Collections.Generic;
using Qoollo.Impl.DbController.Db;

namespace Qoollo.Impl.DbController.AsyncDbWorks.Support
{
    internal class AsyncDbHolder
    {
        private List<DbModule> _dbModules;
        private int _current;

        public AsyncDbHolder(List<DbModule> dbModules)
        {
            _dbModules = dbModules;
            _current = 0;
        }

        public bool HasAnother
        {
            get { return _current < _dbModules.Count - 1; }
        }

        public DbModule GetElement
        {
            get { return _dbModules[_current]; }
        }

        public void Switch()
        {
            _current++;
        }
    }
}
