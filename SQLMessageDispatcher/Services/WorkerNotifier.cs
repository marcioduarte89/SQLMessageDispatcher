namespace SQLMessageDispatcher.Services
{
    using SQLMessageDispatcher.Interfaces;
    using System;
    using System.Threading;

    public class WorkerNotifier : IWorkerNotifier
    {
        private readonly EventWaitHandle _mainHandle;

        public WorkerNotifier(EventWaitHandle mainHandle)
        {
            _mainHandle = mainHandle;
        }


        public bool PauseWork()
        {
            return _mainHandle.WaitOne();
        }

        public bool PauseWork(TimeSpan timeout)
        {
            return _mainHandle.WaitOne(timeout);
        }

        public bool ResumeWork()
        {
            return _mainHandle.Set();
        }
    }
}
