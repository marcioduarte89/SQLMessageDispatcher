using System.Threading;
using System.Threading.Tasks;

namespace SQSMessageDispatcher.Interfaces
{
    /// <summary>
    /// Interface to Handle Messages
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    public interface IHandleMessage<TMessage> where TMessage : IMessage {

        /// <summary>
        /// Handles messages of type <typeparamref name="TMessage"/>
        /// </summary>
        /// <param name="message">Message to be processed</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Returns a <see cref="Task"/></returns>
        Task Handle(TMessage message, CancellationToken cancellationToken);
    }
}
