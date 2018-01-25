using System;
using System.Net;
using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.Lambda.Core;
using Amazon.Lambda.Model;
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
                        var executionResult = await _lambda.InvokeAsync(new InvokeRequest
                        {
                            FunctionName = input.FunctionName,
                            InvocationType = InvocationType.RequestResponse,
                            Payload = message.Body
                        });

                        executionResult.EnsureSuccess();

                        await _sqs.DeleteMessageAsync(input.QueueUrl, message.ReceiptHandle);

                        var state = new
                        {
                            queueUrl = input.QueueUrl,
                            functionName = input.FunctionName,
                            payload = message.Body,
                            messageId = message.MessageId
                        };

                        _logger.LogInformation(state, s => $"{s.messageId} processed.");
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

                        _logger.LogError(state, ex, (s,e) => $"{s.messageId} couldn't be processed. {ex.ToString()}");
                    }
                }

                _logger.LogInformation($"Processed {response.Messages.Count} messages");
            }
            catch (Exception ex)
            {
                var state = new 
                {
                    queueUrl = input.QueueUrl,
                    functionName = input.FunctionName,
                };

                _logger.LogCritical(state, ex, (s,e) => $"Something went really bad when processing queue {input.QueueUrl}. {ex.ToString()}");

                throw new Exception($"An error occurred: queue: {state.queueUrl}, {state.functionName}", ex);
            }
        }
    }

    public class PollItem
    {
        public string QueueUrl { get; set; }

        public string FunctionName { get; set; }
    }

    public static class Extensions
    {
        public static void EnsureSuccess(this InvokeResponse response)
        {
            if (response.HttpStatusCode != HttpStatusCode.OK)
            {
                throw new Exception();
            }

            if (response.FunctionError != null)
            {
                throw new Exception(response.FunctionError);
            }
        }
    }
}