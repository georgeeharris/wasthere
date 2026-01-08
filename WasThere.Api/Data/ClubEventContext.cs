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
    public DbSet<Flyer> Flyers { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<UserClubNightAttendance> UserClubNightAttendances { get; set; } = null!;
    
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
        
        // Configure Flyer relationships with cascade delete
        modelBuilder.Entity<Flyer>()
            .HasOne(f => f.Event)
            .WithMany()
            .HasForeignKey(f => f.EventId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<Flyer>()
            .HasOne(f => f.Venue)
            .WithMany()
            .HasForeignKey(f => f.VenueId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<ClubNight>()
            .HasOne(cn => cn.Flyer)
            .WithMany(f => f.ClubNights)
            .HasForeignKey(cn => cn.FlyerId)
            .OnDelete(DeleteBehavior.SetNull);
        
        // Configure User entity
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();
        
        // Configure UserClubNightAttendance many-to-many relationship
        modelBuilder.Entity<UserClubNightAttendance>()
            .HasKey(ucna => new { ucna.UserId, ucna.ClubNightId });
            
        modelBuilder.Entity<UserClubNightAttendance>()
            .HasOne(ucna => ucna.User)
            .WithMany(u => u.Attendances)
            .HasForeignKey(ucna => ucna.UserId);
            
        modelBuilder.Entity<UserClubNightAttendance>()
            .HasOne(ucna => ucna.ClubNight)
            .WithMany(cn => cn.Attendances)
            .HasForeignKey(ucna => ucna.ClubNightId);
    }
}
