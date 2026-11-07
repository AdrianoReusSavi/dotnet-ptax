using Microsoft.EntityFrameworkCore;
using Ptax.Api;
using Ptax.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();

builder.Services.AddDbContext<PtaxDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("Ptax")));

builder.Services.AddScoped<IDailyRateReader, DailyRateRepository>();
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