using Microsoft.EntityFrameworkCore;
using WasThere.Api.Models;

namespace WasThere.Api.Data;

public class ClubEventContext : DbContext
{
    public ClubEventContext(DbContextOptions<ClubEventContext> options) : base(options)
    {
    }
    
    public DbSet<Event> Events { get; set; } = null!;
    public DbSet<Venue> Venues { get; set; } = null!;
    public DbSet<Act> Acts { get; set; } = null!;
    public DbSet<ClubNight> ClubNights { get; set; } = null!;
    public DbSet<ClubNightAct> ClubNightActs { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure many-to-many relationship
        modelBuilder.Entity<ClubNightAct>()
            .HasKey(cna => new { cna.ClubNightId, cna.ActId });
            
        modelBuilder.Entity<ClubNightAct>()
            .HasOne(cna => cna.ClubNight)
            .WithMany(cn => cn.ClubNightActs)
            .HasForeignKey(cna => cna.ClubNightId);
            
        modelBuilder.Entity<ClubNightAct>()
            .HasOne(cna => cna.Act)
            .WithMany(a => a.ClubNightActs)
            .HasForeignKey(cna => cna.ActId);
    }
}
