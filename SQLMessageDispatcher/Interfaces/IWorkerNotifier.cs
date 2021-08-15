using System;

namespace SQLMessageDispatcher.Interfaces
{
    public interface IWorkerNotifier
    {
        bool PauseWork();

        bool PauseWork(TimeSpan timeout);

        bool ResumeWork();
    }
}
