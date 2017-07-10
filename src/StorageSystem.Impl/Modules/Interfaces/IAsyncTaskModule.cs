using Qoollo.Impl.Modules.Async;

namespace Qoollo.Impl.Modules.Interfaces
{
    internal interface IAsyncTaskModule
    {
        void AddAsyncTask(AsyncData asyncData, bool isforceStart);
        void DeleteTask(string taskName);
        void RestartTask(string taskName, bool isForceStart = false);
        void StartTask(string taskName, bool isForceStart = false);
        void StopTask(string taskName);
    }
}