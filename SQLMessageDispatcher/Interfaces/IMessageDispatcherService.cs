using System.Threading;
using System.Threading.Tasks;

namespace SQLMessageDispatcher.Interfaces
{
    public interface IMessageDispatcherService
    {
        Task Execute(CancellationToken cancellationToken);
    }
}
