using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SQLMessageDispatcher.HostedService;

namespace SQLMessageDispatcher.Extensions
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder AddSQSMessageDispatcherHostedService(this IHostBuilder hostBuilder)
        {
            hostBuilder.ConfigureServices(services =>
            {
                services.AddHostedService<SQSMessageDispatcherHostedService>();
            });

            return hostBuilder;
        }
    }
}
