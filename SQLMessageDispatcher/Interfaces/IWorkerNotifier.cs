using System;

namespace SQSMessageDispatcher.Interfaces
{
    public interface IWorkerNotifier
    {
        bool PauseWork();

        bool PauseWork(TimeSpan timeout);

        bool ResumeWork();
    }
}
