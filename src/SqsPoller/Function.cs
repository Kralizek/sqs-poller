using System.IO;
using Amazon.Lambda;
using Amazon.Lambda.Core;
using Amazon.SQS;
using Kralizek.Lambda;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace SqsPoller
{
    public class Function : EventFunction<PollItem>
    {
        protected override void Configure(IConfigurationBuilder builder)
        {
            builder.SetBasePath(Directory.GetCurrentDirectory());

            builder.AddJsonFile("application.json", optional: true);
            builder.AddEnvironmentVariables();
        }

        protected override void ConfigureLogging(ILoggerFactory loggerFactory, IExecutionEnvironment executionEnvironment)
        {
            loggerFactory.AddLambdaLogger(new LambdaLoggerOptions
            {
                IncludeCategory = true,
                IncludeLogLevel = true,
                IncludeNewline = true,
                Filter = (categoryName, logLevel) => logLevel >= LogLevel.Information
            });
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            RegisterHandler<PollItemEventHandler>(services);

            services.AddDefaultAWSOptions(Configuration.GetSection("AWS").GetAWSOptions());

            services.AddAWSService<IAmazonSQS>();
            services.AddAWSService<IAmazonLambda>();
        }
    }
}
