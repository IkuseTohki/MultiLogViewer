using System;
using System.Threading.Tasks;

namespace MultiLogViewer.Services
{
    public interface ITaskRunner
    {
        Task Run(Action action);
    }

    public class TaskRunner : ITaskRunner
    {
        public Task Run(Action action)
        {
            return Task.Run(action);
        }
    }
}
