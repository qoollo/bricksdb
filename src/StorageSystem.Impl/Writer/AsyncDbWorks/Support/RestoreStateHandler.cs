using Qoollo.Impl.Common.Support;

namespace Qoollo.Impl.Writer.AsyncDbWorks.Support
{
    internal class RestoreStateHandler
    {
        private readonly WriterStateFileLogger _saver;
        public RestoreState RequiredRestoreState { get; private set; }
        private RestoreState _currentRestoreRunState;
        private RestoreType _type;
        public WriterUpdateState UpdateState { get; private set; }

        public RestoreStateHandler(WriterStateFileLogger saver)
        {
            _saver = saver;
            RequiredRestoreState = RestoreState.Restored;
            _currentRestoreRunState = RestoreState.Restored;
            _type = RestoreType.None;
            UpdateState = WriterUpdateState.Free;
        }

        public bool TryUpdateState(RestoreState state, WriterUpdateState updateState)
        {
            if(updateState == WriterUpdateState.Force)
                return TryUpdateState(state);

            switch (UpdateState)
            {
                case WriterUpdateState.Free:
                    switch (updateState)
                    {
                        case WriterUpdateState.Free:
                            return TryUpdateState(state);
                    }
                    break;
                case WriterUpdateState.RestoreInProgress:
                    switch (updateState)
                    {
                        case WriterUpdateState.Free:
                        case WriterUpdateState.RestoreInProgress:
                            return TryUpdateState(state);
                    }
                    break;
                case WriterUpdateState.AfterRestore:
                    switch (updateState)
                    {
                        case WriterUpdateState.RestoreInProgress:
                            UpdateState = WriterUpdateState.Free;
                            return false;
                    }
                    break;
            }

            return false;
        }

        private bool TryUpdateState(RestoreState state)
        {
            if (state < RequiredRestoreState)
                return false;
            RequiredRestoreState = state;

            Save();
            return true;
        }

        public void StartRestore(RestoreState runState, RestoreType type)
        {
            _currentRestoreRunState = runState;
            _type = type;
            UpdateState = WriterUpdateState.RestoreInProgress;

            Save();
        }

        public void CompleteRestore(bool isForceFinish = false)
        {
            if (_currentRestoreRunState >= RequiredRestoreState || isForceFinish)
            {
                _currentRestoreRunState = RestoreState.Restored;
                RequiredRestoreState = RestoreState.Restored;
            }
            else
                _currentRestoreRunState = RestoreState.Restored;

            _type = RestoreType.None;
            UpdateState = WriterUpdateState.AfterRestore;

            Save();
        }

        public bool IsNeedRestore()
        {
            return RequiredRestoreState != RestoreState.Restored;
        }

        public bool IsEqualState(RestoreState state)
        {
            return state >= RequiredRestoreState;
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