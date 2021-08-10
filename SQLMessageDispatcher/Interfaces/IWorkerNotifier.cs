using System;

namespace SQLMessageDispatcher.Interfaces
{
    public interface IWorkerNotifier
    {
        bool WaitForWork();

        bool WaitForWork(TimeSpan timeout);

        bool ResumeWork();
    }
}
