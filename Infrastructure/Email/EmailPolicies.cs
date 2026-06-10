using System.Net;
using Polly;
using Polly.Retry;

namespace Infrastructure.Email;

public static class EmailPolicies
{
    public static ResiliencePipeline<HttpResponseMessage> RetryPipeline { get; } =
        new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(200),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(r => !r.IsSuccessStatusCode && r.StatusCode != HttpStatusCode.Unauthorized)
            })
            .Build();
}
