using Amazon.SQS.Model;
using System;
using System.Collections.Generic;

namespace SQSMessageDispatcher.Interfaces
{
    public interface IWorkersManager
    {
        event EventHandler ReadyToWork;

        void AddWork(IEnumerable<Message> messages);

        void FinishWork();
    }
}
