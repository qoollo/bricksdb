using System;
using System.Threading.Tasks;
using Qoollo.Impl.Collector.Tasks;

namespace Qoollo.Impl.Collector.Interfaces
{
    internal interface IBackgroundModule
    {
        void Run(SearchTask sTask, Action action);
        Task RunAsTask(Action action);
    }
}