namespace SQLMessageDispatcher.Services
{
    using Amazon.SQS;
    using Amazon.SQS.Model;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using SQLMessageDispatcher.Helpers;
    using SQLMessageDispatcher.Interfaces;
    using SQLMessageDispatcher.Models;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    public class WorkersManager : IWorkersManager
    {
        private readonly WorkerMessageConfiguration _workerMessageConfiguration;
        private readonly IWorkerNotifier _workerNotifier;
        private readonly IAmazonSQS _amazonSqs;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<WorkersManager> _logger;
        private readonly ConcurrentQueue<Message> _messageQueue;
        private Thread[] _threadList;
        private CancellationTokenSource _stoppingTokenSource;

        public event EventHandler ReadyToWork;

        public WorkersManager(
            WorkerMessageConfiguration workerMessageConfiguration,
            IWorkerNotifier workerNotifier, 
            IAmazonSQS amazonSqs, 
            IServiceProvider serviceProvider, 
            ILogger<WorkersManager> logger)
        {
            _workerMessageConfiguration = workerMessageConfiguration;
            _workerNotifier = workerNotifier;
            _amazonSqs = amazonSqs;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _messageQueue = new ConcurrentQueue<Message>();
            _stoppingTokenSource = new CancellationTokenSource();

            SetConcurrency(workerMessageConfiguration.ConcurrencyLevel);
        }

        public void AddWork(IEnumerable<Message> messages)
        {
            foreach (var message in messages)
            {
                _messageQueue.Enqueue(message);
                _workerNotifier.ResumeWork();
            }
        }

        private void SetConcurrency(int concurrencyLevel)
        {
            _threadList = new Thread[concurrencyLevel];

            for (int i = 0; i < concurrencyLevel; i++)
            {
                _threadList[i] = new Thread(Process) { Name = $"SQS_Dispatcher_Thread_{i}" };
                _threadList[i].Start();
            }
        }

        private async void Process()
        {
            _logger.LogInformation($"Thread { Thread.CurrentThread.Name } is starting...");

            while (!_stoppingTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    // Waits for work..
                    _workerNotifier.WaitForWork();

                    // If after waiting, there has been the cancellation request or a forced request for abortion, exit
                    if (_stoppingTokenSource.Token.IsCancellationRequested)
                    {
                        break;
                    }

                    if (!_messageQueue.TryDequeue(out var message))
                    {
                        // If there are no messages continue and wait
                        RaiseReadyToReceiveWorkEvent();
                        continue;
                    }

                    var processingResult = await ProcessMessage(message);

                    if (!processingResult)
                    {
                        RaiseReadyToReceiveWorkEvent();
                        continue;
                    }

                    await DeleteMessage(message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error has ocurred processing a message");
                }

                RaiseReadyToReceiveWorkEvent();
            }

            _logger.LogInformation($"Exiting thread {Thread.CurrentThread.Name}");
        }

        private void RaiseReadyToReceiveWorkEvent()
        {
            var handler = ReadyToWork;
            handler?.Invoke(this, null);
        }

        /// <summary>
        /// Processes the message
        /// </summary>
        /// <param name="message">Message to be processed</param>
        /// <returns></returns>
        private async Task<bool> ProcessMessage(Message message)
        {
            var messageType = TryGetMessageType(message);

            if (messageType == null)
            {
                // Don't know what to do with the message, ignore it
                _logger.LogInformation($"Message with id { message.MessageId } does not have attributes -- Skipping message");
                return false;
            }

            // Checks if the message has been assigned an attribute with the type
            await CheckAndIncreaseVisibilityTimout(message);

            var genericType = _workerMessageConfiguration.MessagesAssembly.GetTypes().FirstOrDefault(x => x.AssemblyQualifiedName == messageType);

            var deserializedMessage = JsonConvert.DeserializeObject(message.Body, genericType) as IMessage;

            var handlerObject = _serviceProvider.GetService(typeof(IHandleMessage<>).MakeGenericType(genericType));

            if (handlerObject == null)
            {
                _logger.LogError($"Message with id { message.MessageId } and type { messageType } does not have a registered implementation -- Skipping message");
                return false;
            }

            var handleMethod = handlerObject.GetType().GetMethod("Handle");

            try
            {
                await (Task)handleMethod.Invoke(handlerObject, new object[] { deserializedMessage, _stoppingTokenSource.Token });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Message with id { message.MessageId } and type { messageType } could not be processed -- Will retry", ex);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Deletes the Message
        /// </summary>
        /// <param name="message">Message to be deleted</param>
        private async Task DeleteMessage(Message message)
        {
            var deleteMessage = new DeleteMessageRequest(_workerMessageConfiguration.QueueName, message.ReceiptHandle);
            var response = await _amazonSqs.DeleteMessageAsync(deleteMessage, CancellationToken.None);
            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                _logger.LogInformation($"Deleted message with Id {message.MessageId}");
            }
        }

        /// <summary>
        /// Checks if specific message has visibility timeout requirements that are not of the default ones. Increases visibility timeout if necessary.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private async Task CheckAndIncreaseVisibilityTimout(Message message)
        {
            if (message.MessageAttributes.TryGetValue(Constants.SQSMessageVisibilityTimeout, out var visibilityTimeout))
            {
                _logger.LogInformation($"Changing visibility timeout to {visibilityTimeout.StringValue} in message {message.MessageId}");

                var visibilityRequest = new ChangeMessageVisibilityRequest
                {
                    QueueUrl = _workerMessageConfiguration.QueueName,
                    ReceiptHandle = message.ReceiptHandle,
                    VisibilityTimeout = int.TryParse(visibilityTimeout.StringValue, out var visibilityTimeoutValue) ? visibilityTimeoutValue : _workerMessageConfiguration.DefaultVisibilityTimeout
                };

                var changeVisibilityResult = await _amazonSqs.ChangeMessageVisibilityAsync(visibilityRequest, _stoppingTokenSource.Token);
                if (changeVisibilityResult.HttpStatusCode != HttpStatusCode.OK)
                {
                    _logger.LogInformation($"Changing visibility timemout failed in message {message.MessageId} - Default will be used");
                }
            }
        }

        /// <summary>
        /// Tries to get Message type from Message attributes
        /// </summary>
        /// <param name="message">Message to get the type</param>
        /// <returns>Returns the message type or null</returns>
        private string TryGetMessageType(Message message)
        {
            return message.MessageAttributes != null && message.MessageAttributes.Any() && message.MessageAttributes.TryGetValue(Constants.SQSMessageAttributeType, out var messageAttributeValue)
                ? messageAttributeValue.StringValue
                : null;
        }

        public void FinishWork()
        {
            _stoppingTokenSource.Cancel();

            foreach (var _ in _threadList)
            {
                // Let's try and have all the remaining threads to continue whatever they are so they can exit gracefully
                _workerNotifier.ResumeWork();
            }
        }
    }
}
