using Qoollo.Impl.Common.Support;

namespace Qoollo.Impl.Writer.AsyncDbWorks.Support
{    
    internal class RestoreStateHelper
    {
        public RestoreState State { get { return _state; } }

        public RestoreStateHelper(bool isNeedRestore)
        {
            _state = isNeedRestore ? RestoreState.SimpleRestoreNeed : RestoreState.Restored;
        }

        private RestoreState _state;

        public void DistributorSendState(RestoreState state)
        {
            switch (_state)
            {
               case RestoreState.Restored:
                    _state = state;
                    break;
               case RestoreState.SimpleRestoreNeed:
                    if (state == RestoreState.FullRestoreNeed)
                        _state = state;
                    break;
            }
        }

        public void LocalSendState(bool isModelUpdate)
        {
            _state = isModelUpdate ? RestoreState.FullRestoreNeed : RestoreState.SimpleRestoreNeed;
        }

        public void FinishRestore(bool isModelUpdate)
        {
            if (isModelUpdate && _state == RestoreState.FullRestoreNeed)
                _state = RestoreState.Restored;

            if (!isModelUpdate && _state == RestoreState.SimpleRestoreNeed)
                _state = RestoreState.Restored;
        }
    }
}
