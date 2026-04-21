using AutoFixture;
using TicketSales.Application.Events.Requests;
using TicketSales.Domain;
using TicketSales.Infrastructure.Persistence;
using TicketSales.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace TicketSales.Api.Tests.Events;

public class EventServiceTests : IDisposable
{
    private readonly FakeTimeProvider _timeProvider;
    private readonly AppDbContext _db;
    private readonly EventService _sut;
    private readonly IFixture _fixture;

    public EventServiceTests()
    {
        // Set time to a fixed future date so "today" is 2026-06-15
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 6, 15, 10, 0, 0, TimeSpan.Zero));

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);
        _sut = new EventService(_db, _timeProvider);

        _fixture = new Fixture();
    }

    public void Dispose() => _db.Dispose();

    // ── Ticket Code Generation ─────────────────────────────────────────────────

    [Fact]
    public async Task PurchaseTickets_GeneratesUniqueTicketCodesAsync()
    {
        var (_, ev) = await SeedEventAsync();

        var tickets = await _sut.PurchaseTicketsAsync(ev.Id,
            new PurchaseTicketsRequest("John Doe", "john@example.com", 5));

        var codes = tickets.Select(t => t.TicketCode).ToList();
        codes.Distinct().Count().ShouldBe(codes.Count);
    }

    [Fact]
    public async Task PurchaseTickets_TicketCodeIsValidGuidAsync()
    {
        var (_, ev) = await SeedEventAsync();

        var tickets = await _sut.PurchaseTicketsAsync(ev.Id,
            new PurchaseTicketsRequest("Jane Doe", "jane@example.com", 1));

        Guid.TryParse(tickets[0].TicketCode, out _).ShouldBeTrue();
    }

    // ── Availability Check ─────────────────────────────────────────────────────

    [Fact]
    public async Task PurchaseTickets_ReducesAvailableTicketsAsync()
    {
        var (_, ev) = await SeedEventAsync(totalTickets: 10);

        await _sut.PurchaseTicketsAsync(ev.Id,
            new PurchaseTicketsRequest("John", "john@example.com", 3));

        var updated = await _db.Events.FindAsync(ev.Id);
        updated!.AvailableTickets.ShouldBe(7);
    }

    [Fact]
    public async Task PurchaseTickets_WhenNotEnoughTickets_ThrowsAsync()
    {
        var (_, ev) = await SeedEventAsync(totalTickets: 2);

        var ex = await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.PurchaseTicketsAsync(ev.Id,
                new PurchaseTicketsRequest("John", "john@example.com", 5)));

        ex.Message.ShouldContain("Not enough tickets");
    }

    [Fact]
    public async Task PurchaseTickets_ExactlyAvailable_SucceedsAsync()
    {
        var (_, ev) = await SeedEventAsync(totalTickets: 3);

        var tickets = await _sut.PurchaseTicketsAsync(ev.Id,
            new PurchaseTicketsRequest("John", "john@example.com", 3));

        tickets.Count.ShouldBe(3);
        var updated = await _db.Events.FindAsync(ev.Id);
        updated!.AvailableTickets.ShouldBe(0);
    }

    // ── Past Event Validation ──────────────────────────────────────────────────

    [Fact]
    public async Task PurchaseTickets_PastEvent_ThrowsAsync()
    {
        var (_, ev) = await SeedEventAsync(date: new DateOnly(2025, 1, 1));

        var ex = await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.PurchaseTicketsAsync(ev.Id,
                new PurchaseTicketsRequest("John", "john@example.com", 1)));

        ex.Message.ShouldContain("past events");
    }

    [Fact]
    public async Task PurchaseTickets_TodayEvent_SucceedsAsync()
    {
        // Today is 2026-06-15
        var (_, ev) = await SeedEventAsync(date: new DateOnly(2026, 6, 15));

        var tickets = await _sut.PurchaseTicketsAsync(ev.Id,
            new PurchaseTicketsRequest("John", "john@example.com", 1));

        tickets.Count.ShouldBe(1);
    }

    [Fact]
    public async Task PurchaseTickets_FutureEvent_SucceedsAsync()
    {
        var (_, ev) = await SeedEventAsync(date: new DateOnly(2027, 1, 1));

        var tickets = await _sut.PurchaseTicketsAsync(ev.Id,
            new PurchaseTicketsRequest("John", "john@example.com", 1));

        tickets.Count.ShouldBe(1);
    }

    // ── Use Ticket ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task UseTicket_MarksAsUsedAsync()
    {
        var (_, ev) = await SeedEventAsync();
        var purchased = await _sut.PurchaseTicketsAsync(ev.Id,
            new PurchaseTicketsRequest("John", "john@example.com", 1));
        var code = purchased[0].TicketCode;

        var used = await _sut.UseTicketAsync(code);

        used.IsUsed.ShouldBeTrue();
    }

    [Fact]
    public async Task UseTicket_AlreadyUsed_ThrowsAsync()
    {
        var (_, ev) = await SeedEventAsync();
        var purchased = await _sut.PurchaseTicketsAsync(ev.Id,
            new PurchaseTicketsRequest("John", "john@example.com", 1));
        var code = purchased[0].TicketCode;

        await _sut.UseTicketAsync(code);

        var ex = await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.UseTicketAsync(code));
        ex.Message.ShouldContain("already been used");
    }

    [Fact]
    public async Task UseTicket_NotFound_ThrowsAsync()
    {
        await Should.ThrowAsync<KeyNotFoundException>(
            () => _sut.UseTicketAsync("non-existent-code"));
    }

    // ── Venue Capacity ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateEvent_ExceedsVenueCapacity_ThrowsAsync()
    {
        var venue = new Venue { Name = "Small Venue", Address = "123 Street", Capacity = 5 };
        _db.Venues.Add(venue);
        await _db.SaveChangesAsync();

        var ex = await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.CreateEventAsync(new CreateEventRequest(
                "Test", "Desc", venue.Id,
                new DateOnly(2027, 1, 1),
                new TimeOnly(10, 0),
                new TimeOnly(12, 0),
                10, 50m)));

        ex.Message.ShouldContain("exceeds venue capacity");
    }

    [Fact]
    public async Task CreateEvent_WithinVenueCapacity_SucceedsAsync()
    {
        var venue = new Venue { Name = "Big Venue", Address = "456 Street", Capacity = 100 };
        _db.Venues.Add(venue);
        await _db.SaveChangesAsync();

        var ev = await _sut.CreateEventAsync(new CreateEventRequest(
            "Test", "Desc", venue.Id,
            new DateOnly(2027, 1, 1),
            new TimeOnly(10, 0),
            new TimeOnly(12, 0),
            50, 25m));

        ev.TotalTickets.ShouldBe(50);
        ev.AvailableTickets.ShouldBe(50);
    }

    // ── Upcoming Events Filter ─────────────────────────────────────────────────

    [Fact]
    public async Task GetUpcomingEvents_ExcludesPastEventsAsync()
    {
        await SeedEventAsync(date: new DateOnly(2025, 1, 1)); // past
        await SeedEventAsync(date: new DateOnly(2027, 1, 1)); // future

        var result = await _sut.GetUpcomingEventsAsync(null, null);

        result.Count.ShouldBe(1);
        result[0].Date.ShouldBe(new DateOnly(2027, 1, 1));
    }

    // ── Helper ─────────────────────────────────────────────────────────────────

    private async Task<(Venue venue, Event ev)> SeedEventAsync(
        int totalTickets = 10,
        DateOnly? date = null)
    {
        var venue = new Venue
        {
            Name = _fixture.Create<string>(),
            Address = _fixture.Create<string>(),
            Capacity = 1000
        };
        _db.Venues.Add(venue);
        await _db.SaveChangesAsync();

        var eventDate = date ?? new DateOnly(2027, 6, 15);
        var ev = new Event
        {
            Title = _fixture.Create<string>(),
            Description = _fixture.Create<string>(),
            VenueId = venue.Id,
            Date = eventDate,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(12, 0),
            TotalTickets = totalTickets,
            AvailableTickets = totalTickets,
            TicketPrice = 50m,
        };
        _db.Events.Add(ev);
        await _db.SaveChangesAsync();
        await _db.Entry(ev).Reference(e => e.Venue).LoadAsync();
        return (venue, ev);
    }
}
