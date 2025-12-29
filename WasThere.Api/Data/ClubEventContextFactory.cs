using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WasThere.Api.Data;

public class ClubEventContextFactory : IDesignTimeDbContextFactory<ClubEventContext>
{
    public ClubEventContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ClubEventContext>();
        
        // Use connection string from environment variable or a default for migrations
        // This is only used at design time for generating migrations
        var connectionString = Environment.GetEnvironmentVariable("DESIGN_TIME_CONNECTION_STRING")
            ?? "Host=localhost;Database=wasthere;Username=postgres;Password=postgres";
        
        optionsBuilder.UseNpgsql(connectionString);
        
        return new ClubEventContext(optionsBuilder.Options);
    }
}
