using Qoollo.Impl.Common.Support;

namespace Qoollo.Impl.Writer.AsyncDbWorks.Support
{    
    internal class RestoreStateHolder
    {
        public RestoreState State { get { return _state; } }

        public RestoreStateHolder(bool needRestore)
        {
            _state = needRestore ? RestoreState.SimpleRestoreNeed : RestoreState.Restored;
            _canRemoteStateUpdate = true;
        }

        public RestoreStateHolder(RestoreState state)
        {
            _state = state; 
            _canRemoteStateUpdate = true;
        }

        private bool _canRemoteStateUpdate;
        private RestoreState _state;

        public void DistributorSendState(RestoreState state)
        {
            if (_canRemoteStateUpdate)
                LocalSendState(state);

            _canRemoteStateUpdate = true;
        }

        public void ModelUpdate()
        {            
            var state = RestoreState.FullRestoreNeed;

            if (state > _state)
                _state = state;
        }        

        public void LocalSendState(RestoreState state)
        {
            if (state > _state)
                _state = state;
        }

        public void FinishRestore(RestoreState state)
        {
            if (state == _state)
            {
                _state = RestoreState.Restored;
                _canRemoteStateUpdate = false;
            }
        }
    }
}
