using Microsoft.EntityFrameworkCore;
using Ptax.Data;
using Ptax.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddOptions<PtaxSyncOptions>()
    .Bind(builder.Configuration.GetSection("PtaxSync"))
    .Validate(options => options.Currencies.Length > 0,
        "PtaxSync: Currencies must list at least one currency.")
    .Validate(options => options.Currencies.All(currency => currency.Length == 3 && currency.All(char.IsAsciiLetterUpper)),
        "PtaxSync: Currencies must be 3-letter uppercase codes.")
    .ValidateOnStart();

builder.Services.AddDbContext<PtaxDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Ptax")));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IDailyRateRepository, DailyRateRepository>();
builder.Services.AddScoped<IPtaxSyncService, PtaxSyncService>();
builder.Services.AddPtaxClient();
builder.Services.AddHostedService<PtaxSyncWorker>();

builder.Build().Run();