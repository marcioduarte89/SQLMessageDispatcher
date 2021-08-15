using Microsoft.Extensions.DependencyInjection;
using SQLMessageDispatcher.Configurations;
using SQLMessageDispatcher.HostedService;
using SQLMessageDispatcher.Interfaces;
using SQLMessageDispatcher.Models;
using System;

namespace SQLMessageDispatcher.Extensions
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
