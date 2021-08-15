using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SQLMessageDispatcher.Helpers;
using SQLMessageDispatcher.Interfaces;
using SQLMessageDispatcher.Models;
using SQLMessageDispatcher.Services;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SQLMessageDispatcher.Configurations
{
    internal static class DependencyConfiguration
    {
        public static Func<IServiceProvider, ReceiveMessageRequest> ReceiveMessageRequestBuilder = (serviceProvider) =>
        {
            var dispatcherConfiguration = serviceProvider.GetService<IOptions<SQSDispatcherConfiguration>>();
            return new ReceiveMessageRequest(dispatcherConfiguration.Value.QueueName)
            {
                WaitTimeSeconds = dispatcherConfiguration.Value.DefaultPolling,
                MaxNumberOfMessages = dispatcherConfiguration.Value.ConcurrencyLevel,
                VisibilityTimeout = dispatcherConfiguration.Value.DefaultVisibilityTimeout,
                //All needed attributes need to be specified, otherwise they won't show in the message
                MessageAttributeNames = new List<string>() { Constants.SQSMessageAttributeType, Constants.SQSMessageVisibilityTimeout },
            };
        };

        public static Func<IServiceProvider, MessageDispatcherService> MessageDispatcherBuilder = (serviceProvider) =>
        {
            var workersManager = serviceProvider.GetService<IWorkersManager>();
            var amazonSQS = serviceProvider.GetService<IAmazonSQS>();
            var receiveMessageRequest = serviceProvider.GetService<ReceiveMessageRequest>();
            var logger = serviceProvider.GetService<ILogger<MessageDispatcherService>>();

            return new MessageDispatcherService(workersManager, amazonSQS, WorkerNotifierBuilder, receiveMessageRequest, logger);
        };

        public static Func<IServiceProvider, WorkerMessageConfiguration> WorkerMessageConfigurationBuilder = (serviceProvider) =>
        {
            var dispatcherConfiguration = serviceProvider.GetService<IOptions<SQSDispatcherConfiguration>>();
            return new WorkerMessageConfiguration()
            {
                QueueName = dispatcherConfiguration.Value.QueueName,
                ConcurrencyLevel = dispatcherConfiguration.Value.ConcurrencyLevel,
                DefaultVisibilityTimeout = dispatcherConfiguration.Value.DefaultVisibilityTimeout,
                MessagesAssembly = dispatcherConfiguration.Value.MessagesAssembly,
            };
        };

        public static Func<IServiceProvider, WorkersManager> WorkersManagerBuilder = (serviceProvider) =>
        {
            var workerMessageConfiguration = serviceProvider.GetService<WorkerMessageConfiguration>();
            var amazonSQS = serviceProvider.GetService<IAmazonSQS>();
            var logger = serviceProvider.GetService<ILogger<WorkersManager>>();

            return new WorkersManager(workerMessageConfiguration, WorkerNotifierBuilder, amazonSQS, serviceProvider, logger);
        };

        private static WorkerNotifier WorkerNotifierBuilder => new WorkerNotifier(new AutoResetEvent(false));
    }
}
