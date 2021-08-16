using Amazon.SQS;
using Amazon.SQS.Model;
using Newtonsoft.Json;
using SQSMessageDispatcher.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SQSMessageDispatcher.Extensions {
    public static class IAmazonSQLExtensions
    {
        public static async Task<SendMessageResponse> SendMessageAsync<TMessage>(this IAmazonSQS amazonSQS, string queue, TMessage message)
        {
            var messageType = message.GetType();
            var sendMessageRequest = new SendMessageRequest(queue, JsonConvert.SerializeObject(message))
            {
                MessageAttributes = new Dictionary<string, MessageAttributeValue>()
                {
                    { Constants.SQSMessageAttributeType, new MessageAttributeValue() {
                        StringValue = messageType.AssemblyQualifiedName,
                        DataType = "String"
                    }}
                }
            };

            return await amazonSQS.SendMessageAsync(sendMessageRequest);
        }

        public static async Task<SendMessageResponse> SendMessageAsync<TMessage>(this IAmazonSQS amazonSQS, Models.SendMessageRequest sendMessageRequest)
        {
            var messageType = typeof(TMessage);

            var serializer = new JsonSerializerSettings();
            serializer.Error += delegate (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
            {
                args.ErrorContext.Handled = true;
            };

            var message = JsonConvert.DeserializeObject<TMessage>(sendMessageRequest.MessageBody, serializer);

            if (message is null)
            {
                throw new ArgumentException("Type provided in MessageBody is not the same type as TMessage");
            }

            sendMessageRequest.MessageAttributes[Constants.SQSMessageAttributeType] =
                new MessageAttributeValue()
                {
                    StringValue = messageType.AssemblyQualifiedName,
                    DataType = "String"
                };

            sendMessageRequest.MessageAttributes[Constants.SQSMessageVisibilityTimeout] =
                new MessageAttributeValue()
                {
                    StringValue = sendMessageRequest.VisibilityTimeout.ToString(),
                    DataType = "Number"
                };

            return await amazonSQS.SendMessageAsync(sendMessageRequest);
        }
    }
}
