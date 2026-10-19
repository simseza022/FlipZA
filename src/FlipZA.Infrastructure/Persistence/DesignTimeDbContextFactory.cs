using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FlipZA.Infrastructure.Persistence;

/// <summary>
/// Lets `dotnet ef` build the context without booting the Engine host.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<FlipZaDbContext>
{
    public FlipZaDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<FlipZaDbContext>()
            .UseSqlite("Data Source=flipza.db")
            .Options);
}
