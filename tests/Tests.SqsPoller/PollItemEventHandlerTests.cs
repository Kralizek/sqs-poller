using NUnit.Framework;
using AutoFixture;
using SqsPoller;
using Amazon.Lambda;
using Moq;
using Amazon.SQS;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using Amazon.SQS.Model;
using System.Collections.Generic;
using System.Threading;
using Amazon.Lambda.Model;

namespace Tests
{
    [TestFixture]
    public class PollItemEventHandlerTests
    {
        private IFixture fixture;

        private Mock<IAmazonSQS> mockSqs;

        private Mock<IAmazonLambda> mockLambda;

        private ILambdaContext lambdaContext;

        [SetUp]
        public void Initialize()
        {
            fixture = new Fixture();

            fixture.Customize<Message>(c => c.OmitAutoProperties()
                                                .With(p => p.Body)
                                                .With(p => p.MessageId)
                                                .With(p => p.ReceiptHandle));

            mockSqs = new Mock<IAmazonSQS>();

            mockLambda = new Mock<IAmazonLambda>();

            lambdaContext = new TestLambdaContext();
        }

        private PollItemEventHandler CreateSystemUnderTest()
        {
            return new PollItemEventHandler(mockSqs.Object, mockLambda.Object, Mock.Of<ILogger<PollItemEventHandler>>());
        }

        [Test]
        public async Task Specified_empty_SQS_queue_is_inspected()
        {
            var item = fixture.Create<PollItem>();

            var receiveMessageResponse = fixture.Build<ReceiveMessageResponse>()
                                            .OmitAutoProperties()
                                            .With(p => p.Messages, new List<Message>())
                                            .Create();

            mockSqs.Setup(p => p.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(receiveMessageResponse);

            var sut = CreateSystemUnderTest();

            await sut.HandleAsync(item, lambdaContext);

            mockSqs.Verify(p => p.ReceiveMessageAsync(It.Is<ReceiveMessageRequest>(r => r.QueueUrl == item.QueueUrl), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Specified_SQS_queue_is_inspected()
        {
            var item = fixture.Create<PollItem>();

            var message = fixture.Create<Message>();

            var receiveMessageResponse = fixture.Build<ReceiveMessageResponse>()
                                            .OmitAutoProperties()
                                            .With(p => p.Messages, new List<Message>{ message })
                                            .Create();

            mockSqs.Setup(p => p.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(receiveMessageResponse);

            var sut = CreateSystemUnderTest();

            await sut.HandleAsync(item, lambdaContext);

            mockSqs.Verify(p => p.ReceiveMessageAsync(It.Is<ReceiveMessageRequest>(r => r.QueueUrl == item.QueueUrl), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Lambda_is_invoked_with_message_body_as_payload()
        {
            var item = fixture.Create<PollItem>();

            var message = fixture.Create<Message>();

            var receiveMessageResponse = fixture.Build<ReceiveMessageResponse>()
                                            .OmitAutoProperties()
                                            .With(p => p.Messages, new List<Message>{ message })
                                            .Create();

            mockSqs.Setup(p => p.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(receiveMessageResponse);

            var sut = CreateSystemUnderTest();

            await sut.HandleAsync(item, lambdaContext);

            mockLambda.Verify(p => p.InvokeAsync(It.Is<InvokeRequest>(r => r.FunctionName == item.FunctionName && r.Payload == message.Body), It.IsAny<CancellationToken>()), Times.Once);
        }

    }
}