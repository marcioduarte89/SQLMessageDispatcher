using Amazon.SQS;
using Amazon.SQS.Model;
using Newtonsoft.Json;
using SQLMessageDispatcher.Helpers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SQLMessageDispatcher.Extensions
{
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
    }
}
