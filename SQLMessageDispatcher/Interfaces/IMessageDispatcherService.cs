using System.Threading;
using System.Threading.Tasks;

namespace SQSMessageDispatcher.Interfaces
{
    public interface IMessageDispatcherService
    {
        Task Execute(CancellationToken cancellationToken);
    }
}
