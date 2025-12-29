using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WasThere.Api.Data;

public class ClubEventContextFactory : IDesignTimeDbContextFactory<ClubEventContext>
{
    public ClubEventContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ClubEventContext>();
        
        // Use connection string from environment variable for design-time operations
        // This is only used for generating migrations, not at runtime
        // Set DESIGN_TIME_CONNECTION_STRING environment variable with your connection details
        var connectionString = Environment.GetEnvironmentVariable("DESIGN_TIME_CONNECTION_STRING");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                "DESIGN_TIME_CONNECTION_STRING environment variable must be set for migrations. " +
                "Example: export DESIGN_TIME_CONNECTION_STRING='Host=localhost;Database=wasthere;Username=postgres;Password=yourpassword'");
        }
        
        optionsBuilder.UseNpgsql(connectionString);
        
        return new ClubEventContext(optionsBuilder.Options);
    }
}
