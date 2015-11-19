using Qoollo.Impl.Common.Support;

namespace Qoollo.Impl.Writer.AsyncDbWorks.Support
{    
    internal class RestoreStateHolder
    {
        public RestoreState State { get { return _state; } }

        public RestoreStateHolder(bool isNeedRestore)
        {
            _state = isNeedRestore ? RestoreState.SimpleRestoreNeed : RestoreState.Restored;
            _isRestoreFinish = false;
        }

        public RestoreStateHolder(RestoreState state)
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
            var state = isModelUpdate ? RestoreState.FullRestoreNeed : RestoreState.SimpleRestoreNeed;

            if (state > _state)
                _state = state;
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

        public void LocalSendState(RestoreState state)
        {
            if (state > _state && state != RestoreState.Default)
                _state = state;
        }

        public void FinishRestore(RestoreState state)
        {
            if (state == _state)
            {
                _state = RestoreState.Restored;
                _isRestoreFinish = true;
            }
        }
    }
}
