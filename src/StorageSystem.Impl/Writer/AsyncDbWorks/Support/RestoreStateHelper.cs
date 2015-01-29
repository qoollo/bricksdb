namespace Qoollo.Impl.Writer.AsyncDbWorks.Support
{    
    internal class RestoreStateHelper
    {
        public bool IsNeedRestore { get { return _state != RestoreState.NotNeed; } }

        private RestoreState _state;

        public RestoreStateHelper(bool isNeedRestore)
        {
            _state = isNeedRestore ? RestoreState.StartNeed : RestoreState.NotNeed;
        }

        public void RestoreStart()
        {
            _state = RestoreState.InitiatorStart;
        }

        public void InitiatorState(bool isStart)
        {
            if(_state == RestoreState.InitiatorStart && !isStart)
                _state = RestoreState.NotNeed;
        }
    }
}
