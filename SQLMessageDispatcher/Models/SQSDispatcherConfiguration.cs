using System.Reflection;

namespace SQSMessageDispatcher.Models
{
    public class SQSDispatcherConfiguration
    {
        /// <summary>
        /// SQS queue name
        /// </summary>
        public string QueueName { get; set; }

        /// <summary>
        /// Assembly where the messages and handlers are defined. If no value provided, the default Entry Assembly will be used
        /// </summary>
        public Assembly MessagesAssembly { get; set; } = Assembly.GetEntryAssembly();

        /// <summary>
        /// Concurrency level to process the queue. Default is 2.
        /// </summary>
        public int ConcurrencyLevel { get; set; } = 2;

        /// <summary>
        /// Message polling. Defaults to long polling - 20 seconds
        /// </summary>
        public int DefaultPolling { get; set; } = 20;

        /// <summary>
        /// Message visibility timeout. Defaults is 60 seconds
        /// </summary>
        public int DefaultVisibilityTimeout { get; set; } = 20;
    }
}
