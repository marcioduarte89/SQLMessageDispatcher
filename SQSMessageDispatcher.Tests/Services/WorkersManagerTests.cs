﻿using Amazon.SQS;
using Amazon.SQS.Model;
using AutoMoq;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using SQSMessageDispatcher.Helpers;
using SQSMessageDispatcher.Interfaces;
using SQSMessageDispatcher.Models;
using SQSMessageDispatcher.Services;
using SQSMessageDispatcher.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace SQSMessageDispatcher.Tests.Services
{
    public class WorkersManagerTests
    {

        private WorkersManager _sut;
        private AutoMoqer _mocker;
        private static EventWaitHandle _mainHandle = new AutoResetEvent(false);
        private WorkerMessageConfiguration _workerConfiguration;

       [SetUp]
        public void Setup()
        {
            _mocker = new AutoMoqer();
            _mocker.GetMock<IAmazonSQS>().Setup(x => x.DeleteMessageAsync(It.IsAny<DeleteMessageRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(new DeleteMessageResponse() { HttpStatusCode = System.Net.HttpStatusCode.OK });
            _mocker.GetMock<IServiceProvider>().Setup(x => x.GetService(It.IsAny<Type>())).Returns(_mocker.GetMock<TestMessageHandler>().Object);

            _workerConfiguration = new WorkerMessageConfiguration()
            {
                ConcurrencyLevel = 1,
                MessagesAssembly = Assembly.GetExecutingAssembly()
            };

            _sut = new WorkersManager(_workerConfiguration, _mocker.GetMock<IWorkerNotifier>().Object, _mocker.GetMock<IAmazonSQS>().Object, _mocker.GetMock<IServiceProvider>().Object, _mocker.GetMock<ILogger<WorkersManager>>().Object);
            _sut.ReadyToWork += WorkersManager_ReadyToWork;
        }


        [Test]
        public void ProcessingWork_WhenProvidingValidMessageToProcess_ShouldProcessMessages()
        {
            _sut.AddWork(new List<Message>()
            {
                new Message()
                {
                    MessageAttributes = new Dictionary<string, MessageAttributeValue>()
                    {
                        { Constants.SQSMessageAttributeType, new MessageAttributeValue(){
                            DataType = "String",
                            StringValue = typeof(TestMessage).AssemblyQualifiedName

                        }}
                    },
                    Body = JsonConvert.SerializeObject(new TestMessage())
                }
            });

            _mainHandle.WaitOne();
            _sut.FinishWork();
            EnsureWorkersFinishedWork(_sut);
            
            _mocker.GetMock<TestMessageHandler>().Verify(x => x.Handle(It.IsAny<TestMessage>(), It.IsAny<CancellationToken>()), Times.Once);
            _mocker.GetMock<IAmazonSQS>().Verify(x => x.ChangeMessageVisibilityAsync(It.IsAny<ChangeMessageVisibilityRequest>(), It.IsAny<CancellationToken>()), Times.Never);
            _mocker.GetMock<IAmazonSQS>().Verify(x => x.DeleteMessageAsync(It.IsAny<DeleteMessageRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void ProcessingWork_WhenProvidingMessageWithoutType_ShouldNotProcessOrDeleteMessage()
        {
            _sut.AddWork(new List<Message>()
            {
                new Message()
                {
                    Body = JsonConvert.SerializeObject(new TestMessage())
                }
            });

            _mainHandle.WaitOne();
            _sut.FinishWork();
            EnsureWorkersFinishedWork(_sut);

            _mocker.GetMock<TestMessageHandler>().Verify(x => x.Handle(It.IsAny<TestMessage>(), It.IsAny<CancellationToken>()), Times.Never);
            _mocker.GetMock<IAmazonSQS>().Verify(x => x.ChangeMessageVisibilityAsync(It.IsAny<ChangeMessageVisibilityRequest>(), It.IsAny<CancellationToken>()), Times.Never);
            _mocker.GetMock<IAmazonSQS>().Verify(x => x.DeleteMessageAsync(It.IsAny<DeleteMessageRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public void ProcessingWork_WhenProvidingMessageWithIncreasedVisibilityTimeout_ShouldIncreaseVisibilityTimoutOnProcessingMessage()
        {
            _mocker.GetMock<IAmazonSQS>().Setup(x => x.ChangeMessageVisibilityAsync(It.IsAny<ChangeMessageVisibilityRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(new ChangeMessageVisibilityResponse() { HttpStatusCode = System.Net.HttpStatusCode.OK });

            _sut.AddWork(new List<Message>()
            {
                new Message()
                {
                     MessageAttributes = new Dictionary<string, MessageAttributeValue>()
                     {
                        { 
                            Constants.SQSMessageAttributeType, 
                            new MessageAttributeValue()
                            {
                                DataType = "String",
                                StringValue = typeof(TestMessage).AssemblyQualifiedName

                            }
                         },
                        {
                            Constants.SQSMessageVisibilityTimeout,
                            new MessageAttributeValue()
                            {
                                DataType = "Number",
                                StringValue = "60"

                            }
                         }
                    },
                    Body = JsonConvert.SerializeObject(new TestMessage())
                }
            });

            _mainHandle.WaitOne();
            _sut.FinishWork();
            EnsureWorkersFinishedWork(_sut);

            _mocker.GetMock<TestMessageHandler>().Verify(x => x.Handle(It.IsAny<TestMessage>(), It.IsAny<CancellationToken>()), Times.Once);
            _mocker.GetMock<IAmazonSQS>().Verify(x => x.ChangeMessageVisibilityAsync(It.IsAny<ChangeMessageVisibilityRequest>(), It.IsAny<CancellationToken>()), Times.Once);
            _mocker.GetMock<IAmazonSQS>().Verify(x => x.DeleteMessageAsync(It.IsAny<DeleteMessageRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void ProcessingWork_WhenProvidingMessageTypeWhichDoesNotImplementIMessageButValidAttribute_ShouldNotProcessOrDeleteMessage()
        {
            _sut.AddWork(new List<Message>()
            {
                new Message()
                {
                     MessageAttributes = new Dictionary<string, MessageAttributeValue>()
                     {
                        {
                            Constants.SQSMessageAttributeType,
                            new MessageAttributeValue()
                            {
                                DataType = "String",
                                StringValue = typeof(MessageNotImplementingInterface).AssemblyQualifiedName

                            }
                         }
                    },
                    Body = JsonConvert.SerializeObject(new MessageNotImplementingInterface())
                }
            });

            _mainHandle.WaitOne();
            _sut.FinishWork();
            EnsureWorkersFinishedWork(_sut);

            _mocker.GetMock<TestMessageHandler>().Verify(x => x.Handle(It.IsAny<TestMessage>(), It.IsAny<CancellationToken>()), Times.Never);
            _mocker.GetMock<IAmazonSQS>().Verify(x => x.ChangeMessageVisibilityAsync(It.IsAny<ChangeMessageVisibilityRequest>(), It.IsAny<CancellationToken>()), Times.Never);
            _mocker.GetMock<IAmazonSQS>().Verify(x => x.DeleteMessageAsync(It.IsAny<DeleteMessageRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public void ProcessingWork_WhenProvidingAssemblyWithoutHandlers_ShouldNotProcessOrDeleteMessage()
        {
            _workerConfiguration = new WorkerMessageConfiguration()
            {
                ConcurrencyLevel = 1,
                MessagesAssembly = typeof(AutoMoqer).Assembly
            };

            _sut = new WorkersManager(_workerConfiguration, _mocker.GetMock<IWorkerNotifier>().Object, _mocker.GetMock<IAmazonSQS>().Object, _mocker.GetMock<IServiceProvider>().Object, _mocker.GetMock<ILogger<WorkersManager>>().Object);
            _sut.ReadyToWork += WorkersManager_ReadyToWork;

            _sut.AddWork(new List<Message>()
            {
                new Message()
                {
                     MessageAttributes = new Dictionary<string, MessageAttributeValue>()
                     {
                        {
                            Constants.SQSMessageAttributeType,
                            new MessageAttributeValue()
                            {
                                DataType = "String",
                                StringValue = typeof(MessageNotImplementingInterface).AssemblyQualifiedName

                            }
                         }
                    },
                    Body = JsonConvert.SerializeObject(new MessageNotImplementingInterface())
                }
            });

            _mainHandle.WaitOne();
            _sut.FinishWork();
            EnsureWorkersFinishedWork(_sut);

            _mocker.GetMock<TestMessageHandler>().Verify(x => x.Handle(It.IsAny<TestMessage>(), It.IsAny<CancellationToken>()), Times.Never);
            _mocker.GetMock<IAmazonSQS>().Verify(x => x.ChangeMessageVisibilityAsync(It.IsAny<ChangeMessageVisibilityRequest>(), It.IsAny<CancellationToken>()), Times.Never);
            _mocker.GetMock<IAmazonSQS>().Verify(x => x.DeleteMessageAsync(It.IsAny<DeleteMessageRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        private void WorkersManager_ReadyToWork(object sender, EventArgs e)
        {
            _mainHandle.Set();
        }

        private void EnsureWorkersFinishedWork(WorkersManager workersManager)
        {
            var threadList = workersManager.GetType().GetField("_threadList", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(workersManager) as Thread[];

            // worker threads should exist right away..
            while (threadList.Any(x => x.IsAlive)) { }
        }
    }
}
