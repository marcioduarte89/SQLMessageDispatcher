using Amazon.Runtime.Internal;
using Amazon.SQS;
using Amazon.SQS.Model;
using AutoMoq;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SQLMessageDispatcher.Interfaces;
using SQLMessageDispatcher.Services;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SQSMessageDispatcher.Tests.Services
{
    public class MessageDispatcherServiceTests
    {
        private MessageDispatcherService _sut;
        private AutoMoqer _mocker;
        private CancellationTokenSource _cancellationTokenSource;

       [SetUp]
        public void Setup()
        {
            _mocker = new AutoMoqer();
            _cancellationTokenSource = new CancellationTokenSource();
            _mocker.GetMock<IAmazonSQS>().Setup(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReceiveMessageResponse() 
                {
                    Messages = new List<Message>() 
                    { 
                        new Message() 
                    }, 
                    HttpStatusCode = System.Net.HttpStatusCode.OK 
                });

            // by default once the add work is called, mimic cancellation token being cancelled.
            _mocker.GetMock<IWorkersManager>().Setup(x => x.AddWork(It.IsAny<List<Message>>())).Callback(delegate () { _cancellationTokenSource.Cancel(); });

            _sut = new MessageDispatcherService(_mocker.GetMock<IWorkersManager>().Object, _mocker.GetMock<IAmazonSQS>().Object, _mocker.GetMock<IWorkerNotifier>().Object, _mocker.GetMock<ReceiveMessageRequest>().Object, _mocker.GetMock<ILogger<MessageDispatcherService>>().Object);
        }

        [Test]
        public async Task Execute_WhenProvidingValidMessageToWorkers_ShouldAddWorkAndContinueProcess()
        {
            await _sut.Execute(_cancellationTokenSource.Token);

            _mocker.GetMock<IAmazonSQS>().Verify(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()), Times.Once);
            _mocker.GetMock<IWorkersManager>().Verify(x => x.AddWork(It.IsAny<List<Message>>()), Times.Once);
        }

        [Test]
        public async Task Execute_WhenSQSIsNotConfigured_ShouldThrowHttpErrorResponseExceptionAndExitTheProcess()
        {
            _mocker.GetMock<IAmazonSQS>().Setup(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>())).ThrowsAsync(new HttpErrorResponseException(null));
            await _sut.Execute(_cancellationTokenSource.Token);

            _mocker.GetMock<IAmazonSQS>().Verify(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()), Times.Once);
            _mocker.GetMock<IWorkersManager>().Verify(x => x.AddWork(It.IsAny<List<Message>>()), Times.Never);
        }

        [Test]
        public async Task Execute_WheGenericError_ShouldTryToAddNewWorkAndContinueProcess()
        {
            _mocker.GetMock<IAmazonSQS>().SetupSequence(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception(null))
                .ReturnsAsync(new ReceiveMessageResponse()
                {
                    Messages = new List<Message>()
                    {
                        new Message()
                    },
                    HttpStatusCode = System.Net.HttpStatusCode.OK
                });

            await _sut.Execute(_cancellationTokenSource.Token);

            _mocker.GetMock<IAmazonSQS>().Verify(x => x.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            _mocker.GetMock<IWorkersManager>().Verify(x => x.AddWork(It.IsAny<List<Message>>()), Times.Once);
        }
    }
}
