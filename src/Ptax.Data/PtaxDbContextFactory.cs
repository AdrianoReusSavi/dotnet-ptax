using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ptax.Data;

internal class PtaxDbContextFactory : IDesignTimeDbContextFactory<PtaxDbContext>
{
    public PtaxDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PtaxDbContext>()
            .UseNpgsql("Host=localhost;Database=ptax")
            .Options;

        return new PtaxDbContext(options);
    }
}