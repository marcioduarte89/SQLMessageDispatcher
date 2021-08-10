using Microsoft.Extensions.Hosting;
using SQLMessageDispatcher.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace SQLMessageDispatcher.HostedService
{
    public class SQSMessageDispatcherHostedService : BackgroundService
    {
        private readonly IMessageDispatcherService _messageDispatcherService;

        public SQSMessageDispatcherHostedService(IMessageDispatcherService messageDispatcherService)
        {
            _messageDispatcherService = messageDispatcherService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _messageDispatcherService.Execute(stoppingToken);
        }
    }
}
