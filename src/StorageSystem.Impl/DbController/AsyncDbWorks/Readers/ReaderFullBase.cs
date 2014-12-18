using System;
using System.Diagnostics.Contracts;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules;
using Qoollo.Impl.Modules.Queue;

namespace Qoollo.Impl.DbController.AsyncDbWorks.Readers
{
    internal abstract class ReaderFullBase:ControlModule
    {
        private SingleReaderBase _reader;
        private Action<InnerData> _process;
        private QueueConfiguration _queueConfiguration;
        private QueueWithParam<InnerData> _queue;
        private bool _isBothTables;

        protected ReaderFullBase(Action<InnerData> process,
            QueueConfiguration queueConfiguration,
            bool isBothTables,
            QueueWithParam<InnerData> queue)
        {
            Contract.Requires(process != null);
            Contract.Requires(queueConfiguration != null);
            _process = process;
            _queueConfiguration = queueConfiguration;
            _isBothTables = isBothTables;
            _queue = queue;
        }

        protected abstract SingleReaderBase CreateReader(bool isLocal, int countElements, Action<InnerData> process);

        #region Public 

        public bool IsComplete
        {
            get
            {
                Logger.Logger.Instance.Trace(string.Format("remote = {0}, queue = {1}", _reader.IsFinish,
                                                           _queue.Count),"restore");
                return _reader.IsFinish && _queue.Count == 0;
            }
        }

        public bool IsQueueEmpty
        {
            get
            {
                Logger.Logger.Instance.Trace(string.Format("remote = {0}, queue = {1}", _reader.IsWait,
                    _queue.Count), "restore");
                return _reader.IsWait && _queue.Count == 0;
            }
        }

        public override void Start()
        {            
            //todo 
            _queue.RegistrateWithStart(_queueConfiguration, (data) =>
            {
                _process(data);
                _reader.GetAnotherData();
            });

            _reader = CreateReader(_isBothTables, _queueConfiguration.MaxSizeQueue, (data) => _queue.Add(data));
            _reader.Start();
        }

        public void GetAnotherData()
        {
            _reader.GetAnotherData();
        }

        public void Stop()
        {
            Dispose();
        }

        #endregion

        protected override void Dispose(bool isUserCall)
        {
            if (isUserCall && _reader!=null)
            {
                _reader.Dispose();
            }
            base.Dispose(isUserCall);
        }
    }
}
