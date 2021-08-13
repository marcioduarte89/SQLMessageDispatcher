namespace SQSMessageDispatcher.Tests.Helpers
{
    using SQLMessageDispatcher.Interfaces;
    using System.Threading;
    using System.Threading.Tasks;

    public class MessageNotImplementingInterface
    {
    }

    public class TestMessage : IMessage
    {
    }

    public class TestMessageHandler : IHandleMessage<TestMessage>
    {
        public virtual Task Handle(TestMessage message, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
