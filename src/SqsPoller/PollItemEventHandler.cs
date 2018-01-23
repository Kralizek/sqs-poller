using System;
using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.Lambda.Core;
using Amazon.SQS;
using Amazon.SQS.Model;
using Kralizek.Lambda;
using Microsoft.Extensions.Logging;

namespace SqsPoller
{
    public class PollItemEventHandler : IEventHandler<PollItem>
    {
        private readonly ILogger<PollItemEventHandler> _logger;
        private readonly IAmazonSQS _sqs;
        private readonly IAmazonLambda _lambda;

        public PollItemEventHandler(IAmazonSQS sqs, IAmazonLambda lambda, ILogger<PollItemEventHandler> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sqs = sqs ?? throw new ArgumentNullException(nameof(sqs));
            _lambda = lambda ?? throw new ArgumentNullException(nameof(lambda));
        }

        public async Task HandleAsync(PollItem input, ILambdaContext context)
        {
            try
            {
                var response = await _sqs.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = input.QueueUrl
                });

                foreach (var message in response.Messages)
                {
                    try
                    {
                        await _lambda.InvokeAsync(new Amazon.Lambda.Model.InvokeRequest
                        {
                            FunctionName = input.FunctionName,
                            InvocationType = InvocationType.RequestResponse,
                            Payload = message.Body
                        });

                        await _sqs.DeleteMessageAsync(input.QueueUrl, message.ReceiptHandle);
                    }
                    catch (Exception ex)
                    {
                        var state = new 
                        {
                            queueUrl = input.QueueUrl,
                            functionName = input.FunctionName,
                            payload = message.Body,
                            messageId = message.MessageId
                        };

                        _logger.LogError(state, ex, (s,e) => $"{s.messageId} couldn't be processed.");
                    }
                }
            }
            catch (Exception ex)
            {
                var state = new 
                {
                    queueUrl = input.QueueUrl,
                    functionName = input.FunctionName,
                };

                _logger.LogCritical(state, ex, (s,e) => $"Something went really bad when processing queue {input.QueueUrl}");

                throw;
            }
        }
    }

    public class PollItem
    {
        public string QueueUrl { get; set; }
        public string FunctionName { get; set; }
    }
}