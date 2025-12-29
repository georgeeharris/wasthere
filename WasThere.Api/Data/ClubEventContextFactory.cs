using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WasThere.Api.Data;

public class ClubEventContextFactory : IDesignTimeDbContextFactory<ClubEventContext>
{
    public ClubEventContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ClubEventContext>();
        
        // Use a default connection string for migrations
        // This won't be used at runtime, only for generating migrations
        optionsBuilder.UseNpgsql("Host=localhost;Database=wasthere;Username=postgres;Password=postgres");
        
        return new ClubEventContext(optionsBuilder.Options);
    }
}
