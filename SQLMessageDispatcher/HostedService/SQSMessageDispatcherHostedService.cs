using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SQLMessageDispatcher.Helpers;
using SQLMessageDispatcher.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SQLMessageDispatcher.HostedService
{
    public class SQSMessageDispatcherHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SQSMessageDispatcherHostedService> _logger;
        private readonly IAmazonSQS _amazonSqs;
        private readonly string _queueName;
        private readonly Assembly _messagesAssembly;

        public SQSMessageDispatcherHostedService(IServiceProvider serviceProvider, ILogger<SQSMessageDispatcherHostedService> logger, IConfiguration configuration, IAmazonSQS amazonSqs)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _amazonSqs = amazonSqs;
            _queueName = configuration.GetValue<string>("AWS:Queue");
            _messagesAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.GetName().Name == configuration.GetValue<string>("MessagesAssembly")) ?? Assembly.GetEntryAssembly();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var receiveMessage = new ReceiveMessageRequest(_queueName)
                {
                    WaitTimeSeconds = 20, // long polling,
                    MessageAttributeNames = new List<string>() { Constants.SQSMessageAttributeType },
                };

                var resultMessage = await _amazonSqs.ReceiveMessageAsync(receiveMessage, CancellationToken.None);

                foreach (var message in resultMessage.Messages)
                {

                    // Checks if the message has been assigned an attribute
                    if (!message.MessageAttributes.TryGetValue(Constants.SQSMessageAttributeType, out var messageAttributeValue))
                    {
                        // Don't know what to do with the message, ignore it
                        _logger.LogInformation($"Message with id { message.MessageId } does not have attributes -- Skipping message");
                        continue;
                    }

                    var genericType = _messagesAssembly.GetTypes().FirstOrDefault(x => x.AssemblyQualifiedName == messageAttributeValue.StringValue);

                    var deserializedMessage = JsonConvert.DeserializeObject(message.Body, genericType) as IMessage;

                    var handlerObject = _serviceProvider.GetService(typeof(IHandleMessage<>).MakeGenericType(genericType));

                    if (handlerObject == null)
                    {
                        _logger.LogError($"Message with id { message.MessageId } and type { messageAttributeValue.StringValue } does not have a registered implementation -- Skipping message");
                        continue;
                    }

                    var handleMethod = handlerObject.GetType().GetMethod("Handle");
                    var result = (Task)handleMethod.Invoke(handlerObject, new object[] { deserializedMessage, stoppingToken });

                    try
                    {
                        await result;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Message with id { message.MessageId } and type { messageAttributeValue.StringValue } could not be processed -- Will retry", ex);
                        continue;
                    }

                    var deleteMessage = new DeleteMessageRequest(_queueName, message.ReceiptHandle);
                    var response = await _amazonSqs.DeleteMessageAsync(deleteMessage, CancellationToken.None);
                    if (response.HttpStatusCode == HttpStatusCode.OK)
                    {
                        _logger.LogInformation($"Deleted message with Id {message.MessageId}");
                    }
                }
            }
        }
    }
}
