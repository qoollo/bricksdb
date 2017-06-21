using System;
using System.Diagnostics.Contracts;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Queue;

namespace Qoollo.Impl.Writer.AsyncDbWorks.Readers
{
    internal abstract class ReaderFull<TType> : ReaderFullBase
    {
        private readonly Qoollo.Logger.Logger _logger = Logger.Logger.Instance.GetThisClassLogger();

        protected ReaderFull(Action<TType> process, QueueConfiguration queueConfiguration,
            QueueWithParam<TType> queue)
        {
            Contract.Requires(process != null);
            Contract.Requires(queueConfiguration != null);
            _process = process;
            _queueConfiguration = queueConfiguration;
            _queue = queue;
        }

        private SingleReaderBase _reader;
        private readonly Action<TType> _process;
        private readonly QueueConfiguration _queueConfiguration;
        private readonly QueueWithParam<TType> _queue;

        protected abstract SingleReaderBase CreateReader(int countElements);

        #region Public

        public override bool IsComplete
        {
            get
            {
                if(_logger.IsTraceEnabled)
                    _logger.Trace($"remote = {_reader.IsFinish}, queue = {_queue.Count}", "restore");
                return _reader.IsFinish && _queue.Count == 0;
            }
        }

        public override bool IsQueueEmpty
        {
            get
            {
                if (_logger.IsTraceEnabled)
                    _logger.Trace($"remote = {_reader.IsWait}, queue = {_queue.Count}", "restore");
                return _reader.IsWait && _queue.Count == 0;
            }
        }

        public override void Start()
        {
            _queue.RegistrateWithStart(_queueConfiguration, data =>
            {
                _process(data);
                _reader.GetAnotherData();
            });

            _reader = CreateReader(_queueConfiguration.MaxSizeQueue);
            _reader.Start();
        }

        public override void GetAnotherData()
        {
            _reader.GetAnotherData();
        }

        public void Stop()
        {
            Dispose();
        }

        protected Action<TType> ProcessDataWithQueue()
        {
            return _queue.Add;
        }

        #endregion

        protected override void Dispose(bool isUserCall)
        {
            if (isUserCall && _reader != null)
            {
                _reader.Dispose();
            }
            base.Dispose(isUserCall);
        }
    }
}