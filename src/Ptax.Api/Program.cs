using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Polly;
using Polly.Registry;
using Ptax.Api;
using Ptax.Data;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();

builder.Services.AddDbContext<PtaxDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("Ptax")));

builder.Services.AddStackExchangeRedisCache(options =>
{
    var redis = ConfigurationOptions.Parse(builder.Configuration.GetConnectionString("Redis")!);

    redis.AbortOnConnectFail = false;
    redis.ConnectTimeout = 1000;
    redis.AsyncTimeout = 250;
    redis.SyncTimeout = 250;

    options.ConfigurationOptions = redis;
    options.InstanceName = "ptax:";
});

builder.Services.AddScoped<DailyRateRepository>();

builder.Services.AddResiliencePipeline(CacheCircuitBreaker.PipelineName, pipeline => pipeline.AddCacheCircuitBreaker());

builder.Services.AddScoped<IDailyRateReader>(services => new CachedDailyRateReader(
    services.GetRequiredService<DailyRateRepository>(),
    services.GetRequiredService<IDistributedCache>(),
    services.GetRequiredService<ResiliencePipelineProvider<string>>().GetPipeline(CacheCircuitBreaker.PipelineName),
    services.GetRequiredService<ILogger<CachedDailyRateReader>>()));

builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();

app.UseExceptionHandler(new ExceptionHandlerOptions
{
    StatusCodeSelector = exception => exception is BadHttpRequestException bad
        ? bad.StatusCode
        : StatusCodes.Status500InternalServerError
});

app.UseStatusCodePages();
app.MapRateEndpoints();

app.Run();