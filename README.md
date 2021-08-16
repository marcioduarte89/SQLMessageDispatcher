SQSMessageDispatcher
=======

## Overview
Simple, easy to use, scalable message dispatcher for AWS SQS for .NET.

I work quite a lot with [SQS](https://aws.amazon.com/sqs/) in my personal projects. SQS is a (cheap enough) fully managed message queuing service which eliminates the complexity and overhead associated with managing and operating message-oriented middlewares. Yet, I've got tired of writting background processes to process messages out of SQS queues. This code might sound familiar.

```c#
while (!stoppingToken.IsCancellationRequested) {

    var receiveMessage = new ReceiveMessageRequest(_queueName) {
        // messageRequestDetails
    };

    var resultMessage = await _amazonSqs.ReceiveMessageAsync(receiveMessage, stoppingToken);

    foreach (var message in resultMessage.Messages) {

        // Do some processing..

        var deleteMessage = new DeleteMessageRequest(_queueName, message.ReceiptHandle);
        var response = await _amazonSqs.DeleteMessageAsync(deleteMessage, stoppingToken);

        //..
    }
}
```

I also work quite a lot with [NSB](https://particular.net/nservicebus) and I really like the way I only have to be concerned about infrastructure when I first setup and configure my endpoints, apart from that is always "business" as usual - just want to be concerned about features and domain code. So I thought about implementing the same approach on top of SQS API's.

### How it works:
The same old HostedService is used to process the messages out of the queue, but we don't have to write or maintain any of its code. We also don't have to add new queues, or extend the hosted service if we want to process new messages types.

### What it is not:
The package doesn't try to extend or improve SQS delivery guarantees. Standard queues provide at-least-once delivery, which means that each message is delivered at least once. FIFO queues provide exactly-once processing, which means that each message is delivered once and remains available until a consumer processes it and deletes it. Duplicates are not introduced into the queue. Message handlers should still be idempotent in case the same message is received and delivered for processing more than once (in case of standard queues). 

#### How to configure it:

You can just install it using:  ```Install-Package SQSMessageDispatcher -Version 0.1.0```

A new extension of ```IServiceCollection``` was added: ```AddSQSMessageDispatcherHostedService``` and can be configured using ```SQSDispatcherConfiguration```, ex:

```c#
services.AddSQSMessageDispatcherHostedService(x =>
{
    x.ConcurrencyLevel = 5;
    x.QueueName = Configuration.GetValue<string>("AWS:Queue");
});
```
##### SQSDispatcherConfiguration properties:

```QueueName```: The SQS queue name.

```MessagesAssembly```: The assembly where the messages are located. Default is the Entry Assembly.

```ConcurrencyLevel```: Concurrency level to process messages out of the queue. This setting should be used with caution, throwing more threads into processing messages out of the queue doesn't necessarily mean better performance. One should measure what's the optimal number of threads to use. Defaults to 2.

```DefaultPolling```: [Message polling](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/sqs-short-and-long-polling.html). Defaults is 20 sec.

```DefaultVisibilityTimeout```: [Message visibility timeout.](https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/sqs-visibility-timeout.html). Defaults to 30 seconds. This value can also be set per message. ([see bellow](#sending-a-message))

##### appSettings.json:
```json
  "AWS": {
    ...
    "Queue": "https://.../QueueName"
  }
```

##### Injecting Message handlers:
You can use any dependency injection container you are familiar with to register message handlers, here's an example of the registration using Autofac:

```c#
var mediatrOpenTypes = new[] {
    typeof(IHandleMessage<>),
};

foreach (var mediatrOpenType in mediatrOpenTypes)
{
    builder
       .RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
       .AsClosedTypesOf(mediatrOpenType)
       .AsImplementedInterfaces()
       .InstancePerLifetimeScope();
}
```

#### New types:

Two interfaces were introduced:

A ```IMessage``` marker interface, which all of our messages will implement.

A ```IHandleMessage<TMessage>``` which is the handler which will accept an instance of type ```TMessage : IMessage```

#### How to use it:

##### Ex of message and message handler:

```c#
public class PlaceOrder : IMessage
{
    public string Quantity { get; set; }

    public string Price { get; set; }
}
```

```c#
 public class PlaceOrderHandler : IHandleMessage<PlaceOrder>
    {
        public async Task Handle(PlaceOrder message, CancellationToken token)
        {
            // Handle the message here
        }
    }
```

##### Sending a message:
Two new extensions of ```IAmazonSQS``` were created:

```SendMessageAsync<TMessage>(this IAmazonSQS amazonSQS, string queue, TMessage message)```: This extension is the simplest, just needs the name of the queue and the message we want to process.

```SendMessageAsync<TMessage>(this IAmazonSQS amazonSQS, Models.SendMessageRequest sendMessageRequest)```. This extension allows for more customization. ```Models.SendMessageRequest``` Offers all functionality that ```Amazon.SQS.Model.SendMessageRequest``` offers but also extends it with ```VisibilityTimeout```. If the message type we want to process is expected to take more than the polling time to process, the message could be received from the queue again and be sent to processing. In order to mitigate this, one could increase this value when sending a message to the queue.
