using Qoollo.Impl.Common.Support;

namespace Qoollo.Impl.Writer.AsyncDbWorks.Support
{    
    internal class RestoreStateHelper
    {
        public RestoreState State { get { return _state; } }

        public RestoreStateHelper(bool isNeedRestore)
        {
            _state = isNeedRestore ? RestoreState.SimpleRestoreNeed : RestoreState.Restored;
            _isRestoreFinish = false;
        }

        public RestoreStateHelper(RestoreState state)
        {
            _state = state; 
            _isRestoreFinish = false;
        }

        private bool _isRestoreFinish;
        private RestoreState _state;

        public void DistributorSendState(RestoreState state)
        {
            if (state == RestoreState.Restored && !_isRestoreFinish)
                return;

            switch (_state)
            {
               case RestoreState.Restored:
                    if(!_isRestoreFinish)
                        _state = state;
                    break;
               case RestoreState.SimpleRestoreNeed:
                    if (state == RestoreState.FullRestoreNeed)
                        _state = state;
                    break;
            }

            _isRestoreFinish = false;
        }

        public void LocalSendState(bool isModelUpdate)
        {
            _state = isModelUpdate ? RestoreState.FullRestoreNeed : RestoreState.SimpleRestoreNeed;
        }

        public void FinishRestore(bool isModelUpdate)
        {
            if (isModelUpdate && _state == RestoreState.FullRestoreNeed ||
                !isModelUpdate && _state == RestoreState.SimpleRestoreNeed)
            {
                _state = RestoreState.Restored;
                _isRestoreFinish = true;
            }
        }
    }
}
