using Microsoft.EntityFrameworkCore;

namespace Ptax.Data;

public class PtaxDbContext(DbContextOptions<PtaxDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PtaxDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public DbSet<DailyRate> DailyRates { get; set; }
}