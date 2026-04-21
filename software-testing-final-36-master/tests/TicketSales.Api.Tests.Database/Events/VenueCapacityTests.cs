using TicketSales.Domain;
using Microsoft.EntityFrameworkCore;

namespace TicketSales.Api.Tests.Database.Events;

public class VenueCapacityTests : IClassFixture<EventsDatabaseFixture>
{
    private readonly EventsDatabaseFixture _fixture;

    public VenueCapacityTests(EventsDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Events_TotalTicketsDoNotExceedVenueCapacityAsync()
    {
        await using var db = _fixture.CreateDbContext();

        var violations = await db.Events
            .Include(e => e.Venue)
            .Where(e => e.TotalTickets > e.Venue.Capacity)
            .CountAsync();

        violations.ShouldBe(0);
    }

    [Fact]
    public async Task Venue_Capacity_IsPositiveAsync()
    {
        await using var db = _fixture.CreateDbContext();

        var invalid = await db.Venues.Where(v => v.Capacity <= 0).CountAsync();
        invalid.ShouldBe(0);
    }

    [Fact]
    public async Task Events_AllHaveValidVenueAsync()
    {
        await using var db = _fixture.CreateDbContext();

        var events = await db.Events.Include(e => e.Venue).ToListAsync();

        foreach (var ev in events)
        {
            ev.Venue.ShouldNotBeNull();
            ev.TotalTickets.ShouldBeLessThanOrEqualTo(ev.Venue.Capacity);
        }
    }

    [Fact]
    public async Task Venue_CanHaveMultipleEventsAsync()
    {
        await using var db = _fixture.CreateDbContext();

        var venueWithMultipleEvents = await db.Venues
            .Include(v => v.Events)
            .FirstOrDefaultAsync(v => v.Events.Count > 1);

        venueWithMultipleEvents.ShouldNotBeNull();
        venueWithMultipleEvents.Events.Count.ShouldBeGreaterThan(1);
    }

    [Fact]
    public async Task Events_HaveValidDateTimeRangesAsync()
    {
        await using var db = _fixture.CreateDbContext();

        var events = await db.Events.ToListAsync();
        foreach (var ev in events)
        {
            ev.EndTime.ShouldBeGreaterThan(ev.StartTime);
            ev.TotalTickets.ShouldBeGreaterThan(0);
            ev.AvailableTickets.ShouldBeGreaterThanOrEqualTo(0);
        }
    }
}
