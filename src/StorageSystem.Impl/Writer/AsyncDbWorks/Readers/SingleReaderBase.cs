﻿using System;
using System.Threading;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Modules;

namespace Qoollo.Impl.Writer.AsyncDbWorks.Readers
{
    internal abstract class SingleReaderBase:ControlModule
    {
        private readonly Qoollo.Logger.Logger _logger = Logger.Logger.Instance.GetThisClassLogger();

        private readonly Thread _thread;
        private bool _isFinish;
        private bool _isWait;
        private readonly AutoResetEvent _reset;

        public bool IsFinish => _isFinish;

        public bool IsWait
        {
            get
            {
                bool ret = false;
                lock (_lock)
                {
                    ret = _isWait;
                }
                return ret;
            }
        }

        private readonly object _lock = new object();

        protected SingleReaderBase()
        {            
            _isFinish = false;
            _isWait = false;
            _thread = new Thread(ThreadProcess);
            _reset = new AutoResetEvent(false);
        }

        public override void Start()
        {
            _thread.Start();
        }

        public void GetAnotherData()
        {
            _reset.Set();
        }

        private void ThreadProcess()
        {
            try
            {
                while (true)
                {
                    lock (_lock)
                    {
                        _isWait = false;                        
                    }
                    var result = Read();
                    if (result is FailNetResult)
                    {
                        if(_logger.IsDebugEnabled)
                            _logger.Debug("Thread process exit", "restore");
                        break;
                    }

                    if (_logger.IsDebugEnabled)
                        _logger.Debug("Thread process wait", "restore");

                    lock (_lock)
                    {
                        _isWait = true;
                    }

                    _reset.WaitOne();
                    _reset.Reset();
                }
            }
            catch (Exception e)
            {
                if (_logger.IsWarnEnabled)
                    _logger.Warn(e, "in restore");
                //todo dispose
            }
            
            _isFinish = true;
        }

        protected abstract RemoteResult Read();

        protected override void Dispose(bool isUserCall)
        {
            if (isUserCall)
            {
                _reset.Dispose();
                if (_thread.ThreadState == ThreadState.Running && !_thread.Join(500))
                    _thread.Abort();
            }

            base.Dispose(isUserCall);
        }
    }
}
