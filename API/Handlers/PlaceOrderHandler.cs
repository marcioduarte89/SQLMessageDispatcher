namespace API.Handlers
{
    using API.Messages;
    using SQSMessageDispatcher.Interfaces;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public class PlaceOrderHandler : IHandleMessage<PlaceOrder>
    {
        public async Task Handle(PlaceOrder message, CancellationToken token)
        {
            Console.WriteLine($"Property one: { message.PropertyOne }");
            Console.WriteLine($"Property two: { message.PropertyTwo }");
        }
    }
}
