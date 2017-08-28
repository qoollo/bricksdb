using Qoollo.Impl.Common.Support;

namespace Qoollo.Impl.Writer.AsyncDbWorks.Support
{
    internal class RestoreStateHandler
    {
        private readonly WriterStateFileLogger _saver;
        public RestoreState RequiredRestoreState { get; private set; }
        private RestoreState _currentRestoreRunState;
        private RestoreType _type;
        private bool _isRestoreRun;

        public RestoreStateHandler(WriterStateFileLogger saver)
        {
            _saver = saver;
            _isRestoreRun = false;
            RequiredRestoreState = RestoreState.Restored;
            _currentRestoreRunState = RestoreState.Restored;
            _type = RestoreType.None;
        }

        public bool TryUpdateState(RestoreState state)
        {
            if (state < RequiredRestoreState)
                return false;
            RequiredRestoreState = state;

            Save();
            return true;
        }

        public void StartRestore(RestoreState runState, RestoreType type)
        {
            if (_isRestoreRun)
                return;

            _isRestoreRun = true;
            _currentRestoreRunState = runState;
            _type = type;

            Save();
        }

        public void CompleteRestore()
        {
            if (_currentRestoreRunState >= RequiredRestoreState)
            {
                _currentRestoreRunState = RestoreState.Restored;
                RequiredRestoreState = RestoreState.Restored;
            }
            else
                _currentRestoreRunState = RestoreState.Restored;

            _type = RestoreType.None;
            _isRestoreRun = false;
            Save();
        }

        public bool IsNeedRestore()
        {
            return _type != RestoreType.None;
        }

        private void Save()
        {
            _saver.RestoreType = _type;
            _saver.RestoreStateRun = _currentRestoreRunState;
            _saver.WriterState = RequiredRestoreState;

            _saver.Save();
        }
    }
}