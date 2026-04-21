using TicketSales.Domain;
using Microsoft.EntityFrameworkCore;

namespace TicketSales.Api.Tests.Database.Tickets;

public class TicketAvailabilityTests : IClassFixture<EventsDatabaseFixture>
{
    private readonly EventsDatabaseFixture _fixture;

    public TicketAvailabilityTests(EventsDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SeededData_HasExpectedCountsAsync()
    {
        await using var db = _fixture.CreateDbContext();

        var eventCount = await db.Events.CountAsync();
        eventCount.ShouldBeGreaterThanOrEqualTo(500);

        var ticketCount = await db.Tickets.CountAsync();
        ticketCount.ShouldBeGreaterThanOrEqualTo(10_000);

        var venueCount = await db.Venues.CountAsync();
        venueCount.ShouldBeGreaterThanOrEqualTo(50);
    }

    [Fact]
    public async Task AvailableTickets_NeverExceedsTotalTicketsAsync()
    {
        await using var db = _fixture.CreateDbContext();

        var violatingEvents = await db.Events
            .Where(e => e.AvailableTickets > e.TotalTickets)
            .CountAsync();

        violatingEvents.ShouldBe(0);
    }

    [Fact]
    public async Task AvailableTickets_ManualDecrease_PersistsAsync()
    {
        await using var db = _fixture.CreateDbContext();
        var ev = await db.Events.FirstAsync(e => e.AvailableTickets > 0);
        var before = ev.AvailableTickets;

        ev.AvailableTickets -= 1;
        await db.SaveChangesAsync();

        await using var db2 = _fixture.CreateDbContext();
        var updated = await db2.Events.FindAsync(ev.Id);
        updated!.AvailableTickets.ShouldBe(before - 1);
    }

    [Fact]
    public async Task Ticket_PurchaseDate_IsInPastOrPresentAsync()
    {
        await using var db = _fixture.CreateDbContext();

        var futurePurchases = await db.Tickets
            .Where(t => t.PurchaseDate > DateTime.UtcNow.AddMinutes(1))
            .CountAsync();

        futurePurchases.ShouldBe(0);
    }

    [Fact]
    public async Task Ticket_IsUsed_CanBeUpdatedAsync()
    {
        await using var db = _fixture.CreateDbContext();
        var ticket = await db.Tickets.FirstAsync(t => !t.IsUsed);

        ticket.IsUsed = true;
        await db.SaveChangesAsync();

        await using var db2 = _fixture.CreateDbContext();
        var updated = await db2.Tickets.FindAsync(ticket.Id);
        updated!.IsUsed.ShouldBeTrue();
    }
}
