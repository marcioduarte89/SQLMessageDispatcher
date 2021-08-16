using Microsoft.Extensions.DependencyInjection;
using SQSMessageDispatcher.Configurations;
using SQSMessageDispatcher.HostedService;
using SQSMessageDispatcher.Interfaces;
using SQSMessageDispatcher.Models;
using System;

namespace SQSMessageDispatcher.Extensions
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddSQSMessageDispatcherHostedService(this IServiceCollection serviceCollection, Action<SQSDispatcherConfiguration> setupAction)
        {
            serviceCollection.AddHostedService<SQSMessageDispatcherHostedService>().Configure(setupAction);
            serviceCollection.AddSingleton(DependencyConfiguration.ReceiveMessageRequestBuilder);
            serviceCollection.AddSingleton(DependencyConfiguration.WorkerMessageConfigurationBuilder);
            serviceCollection.AddSingleton<IMessageDispatcherService>(DependencyConfiguration.MessageDispatcherBuilder);
            serviceCollection.AddSingleton<IWorkersManager>(DependencyConfiguration.WorkersManagerBuilder);
            return serviceCollection;
        }
    }
}
