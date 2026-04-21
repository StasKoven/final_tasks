using TicketSales.Domain;
using Microsoft.EntityFrameworkCore;

namespace TicketSales.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Venue> Venues => Set<Venue>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Venue>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Address).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Capacity).IsRequired();

            entity.HasMany(e => e.Events).WithOne(ev => ev.Venue).HasForeignKey(ev => ev.VenueId);
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.TicketPrice).HasPrecision(18, 2);

            entity.HasMany(e => e.Tickets).WithOne(t => t.Event).HasForeignKey(t => t.EventId);
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.BuyerName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.BuyerEmail).HasMaxLength(200).IsRequired();
            entity.Property(e => e.TicketCode).HasMaxLength(36).IsRequired();
            // Enforce unique ticket code at DB level
            entity.HasIndex(e => e.TicketCode).IsUnique();
        });
    }
}
