using System;

namespace MultiLogViewer.Services
{
    public interface IDispatcherService
    {
        void Invoke(Action action);
    }
}
