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


        public bool WaitForWork()
        {
            return _mainHandle.WaitOne();
        }

        public bool WaitForWork(TimeSpan timeout)
        {
            return _mainHandle.WaitOne(timeout);
        }

        public bool ResumeWork()
        {
            return _mainHandle.Set();
        }
    }
}
