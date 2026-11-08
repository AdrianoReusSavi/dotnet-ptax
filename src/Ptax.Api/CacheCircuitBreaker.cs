using Polly;
using Polly.CircuitBreaker;

namespace Ptax.Api;

public static class CacheCircuitBreaker
{
    public const string PipelineName = "redis-cache";

    public static ResiliencePipelineBuilder AddCacheCircuitBreaker(this ResiliencePipelineBuilder builder)
        => builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            MinimumThroughput = 2,
            SamplingDuration = TimeSpan.FromSeconds(30),
            BreakDuration = TimeSpan.FromSeconds(30),
        });
}