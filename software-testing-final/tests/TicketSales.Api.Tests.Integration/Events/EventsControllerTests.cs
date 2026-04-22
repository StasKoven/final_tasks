using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using TicketSales.Application.Events.Requests;
using TicketSales.Application.Events.Responses;
using TicketSales.Domain;
using TicketSales.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace TicketSales.Api.Tests.Integration.Events;

public class EventsControllerTests : IClassFixture<EventsApiFactory>, IAsyncLifetime
{
    private readonly EventsApiFactory _factory;
    private readonly HttpClient _client;
    private readonly IFixture _fixture;

    public EventsControllerTests(EventsApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _fixture = new Fixture();
    }

    public async ValueTask InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Tickets.RemoveRange(db.Tickets);
        db.Events.RemoveRange(db.Events);
        db.Venues.RemoveRange(db.Venues);
        await db.SaveChangesAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    // ── GET /api/events ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_NoEvents_ReturnsEmptyListAsync()
    {
        var response = await _client.GetAsync("/api/events");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<List<EventResponse>>();
        items.ShouldNotBeNull();
        items.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAll_AfterCreating_ReturnsUpcomingEventsAsync()
    {
        var venueId = await CreateVenueAsync();
        await CreateEventAsync(venueId);
        await CreateEventAsync(venueId);

        var response = await _client.GetAsync("/api/events");
        var items = await response.Content.ReadFromJsonAsync<List<EventResponse>>();
        items!.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetAll_FilterByVenue_ReturnsMatchingOnlyAsync()
    {
        var venue1 = await CreateVenueAsync();
        var venue2 = await CreateVenueAsync();
        await CreateEventAsync(venue1);
        await CreateEventAsync(venue2);

        var response = await _client.GetAsync($"/api/events?venueId={venue1}");
        var items = await response.Content.ReadFromJsonAsync<List<EventResponse>>();
        items!.Count.ShouldBe(1);
        items[0].VenueId.ShouldBe(venue1);
    }

    // ── POST /api/events ─────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidRequest_Returns201Async()
    {
        var venueId = await CreateVenueAsync();
        var response = await _client.PostAsJsonAsync("/api/events", ValidCreateRequest(venueId));
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var item = await response.Content.ReadFromJsonAsync<EventResponse>();
        item.ShouldNotBeNull();
        item.AvailableTickets.ShouldBe(item.TotalTickets);
        response.Headers.Location.ShouldNotBeNull();
    }

    [Fact]
    public async Task Create_InvalidRequest_Returns400Async()
    {
        var venueId = await CreateVenueAsync();
        var request = ValidCreateRequest(venueId) with { Title = "" };
        var response = await _client.PostAsJsonAsync("/api/events", request);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ExceedsVenueCapacity_Returns409Async()
    {
        var venueId = await CreateVenueAsync(capacity: 5);
        var request = ValidCreateRequest(venueId) with { TotalTickets = 10 };
        var response = await _client.PostAsJsonAsync("/api/events", request);
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Create_NonExistentVenue_Returns404Async()
    {
        var request = ValidCreateRequest(99999);
        var response = await _client.PostAsJsonAsync("/api/events", request);
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── GET /api/events/{id} ─────────────────────────────────────────────────

    [Fact]
    public async Task GetById_Existing_ReturnsEventDetailAsync()
    {
        var venueId = await CreateVenueAsync();
        var created = await CreateEventAsync(venueId);

        var response = await _client.GetAsync($"/api/events/{created.Id}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var detail = await response.Content.ReadFromJsonAsync<EventDetailResponse>();
        detail.ShouldNotBeNull();
        detail.Id.ShouldBe(created.Id);
        detail.AvailableTickets.ShouldBe(created.TotalTickets);
    }

    [Fact]
    public async Task GetById_NonExistent_Returns404Async()
    {
        var response = await _client.GetAsync("/api/events/99999");
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── PUT /api/events/{id} ─────────────────────────────────────────────────

    [Fact]
    public async Task Update_Valid_Returns200Async()
    {
        var venueId = await CreateVenueAsync();
        var created = await CreateEventAsync(venueId);
        var updateRequest = ValidUpdateRequest(venueId) with { Title = "Updated Title" };

        var response = await _client.PutAsJsonAsync($"/api/events/{created.Id}", updateRequest);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var item = await response.Content.ReadFromJsonAsync<EventResponse>();
        item!.Title.ShouldBe("Updated Title");
    }

    [Fact]
    public async Task Update_NonExistent_Returns404Async()
    {
        var venueId = await CreateVenueAsync();
        var response = await _client.PutAsJsonAsync("/api/events/99999", ValidUpdateRequest(venueId));
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── POST /api/events/{id}/tickets ────────────────────────────────────────

    [Fact]
    public async Task PurchaseTickets_ValidRequest_Returns201Async()
    {
        var venueId = await CreateVenueAsync();
        var ev = await CreateEventAsync(venueId);

        var response = await _client.PostAsJsonAsync($"/api/events/{ev.Id}/tickets",
            new PurchaseTicketsRequest("John Doe", "john@example.com", 2));
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var tickets = await response.Content.ReadFromJsonAsync<List<TicketResponse>>();
        tickets!.Count.ShouldBe(2);
        tickets.All(t => !t.IsUsed).ShouldBeTrue();
        tickets.Select(t => t.TicketCode).Distinct().Count().ShouldBe(2);
    }

    [Fact]
    public async Task PurchaseTickets_ReducesAvailableTicketsAsync()
    {
        var venueId = await CreateVenueAsync();
        var ev = await CreateEventAsync(venueId);

        await _client.PostAsJsonAsync($"/api/events/{ev.Id}/tickets",
            new PurchaseTicketsRequest("John Doe", "john@example.com", 3));

        var response = await _client.GetAsync($"/api/events/{ev.Id}");
        var detail = await response.Content.ReadFromJsonAsync<EventDetailResponse>();
        detail!.AvailableTickets.ShouldBe(ev.TotalTickets - 3);
    }

    [Fact]
    public async Task PurchaseTickets_NotEnoughTickets_Returns409Async()
    {
        var venueId = await CreateVenueAsync();
        var ev = await CreateEventAsync(venueId, totalTickets: 2);

        var response = await _client.PostAsJsonAsync($"/api/events/{ev.Id}/tickets",
            new PurchaseTicketsRequest("John Doe", "john@example.com", 5));

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task PurchaseTickets_InvalidRequest_Returns400Async()
    {
        var venueId = await CreateVenueAsync();
        var ev = await CreateEventAsync(venueId);

        var response = await _client.PostAsJsonAsync($"/api/events/{ev.Id}/tickets",
            new PurchaseTicketsRequest("", "bad-email", 0));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // ── GET /api/tickets/{code} ──────────────────────────────────────────────

    [Fact]
    public async Task GetTicketByCode_ValidCode_Returns200Async()
    {
        var venueId = await CreateVenueAsync();
        var ev = await CreateEventAsync(venueId);
        var purchaseResponse = await _client.PostAsJsonAsync($"/api/events/{ev.Id}/tickets",
            new PurchaseTicketsRequest("Jane Doe", "jane@example.com", 1));
        var tickets = await purchaseResponse.Content.ReadFromJsonAsync<List<TicketResponse>>();
        var code = tickets![0].TicketCode;

        var response = await _client.GetAsync($"/api/tickets/{code}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var ticket = await response.Content.ReadFromJsonAsync<TicketResponse>();
        ticket!.TicketCode.ShouldBe(code);
        ticket.IsUsed.ShouldBeFalse();
    }

    [Fact]
    public async Task GetTicketByCode_InvalidCode_Returns404Async()
    {
        var response = await _client.GetAsync("/api/tickets/non-existent-code");
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── PATCH /api/tickets/{code}/use ────────────────────────────────────────

    [Fact]
    public async Task UseTicket_ValidTicket_Returns200Async()
    {
        var venueId = await CreateVenueAsync();
        var ev = await CreateEventAsync(venueId);
        var purchaseResponse = await _client.PostAsJsonAsync($"/api/events/{ev.Id}/tickets",
            new PurchaseTicketsRequest("Jane Doe", "jane@example.com", 1));
        var tickets = await purchaseResponse.Content.ReadFromJsonAsync<List<TicketResponse>>();
        var code = tickets![0].TicketCode;

        var response = await _client.PatchAsync($"/api/tickets/{code}/use", null);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var ticket = await response.Content.ReadFromJsonAsync<TicketResponse>();
        ticket!.IsUsed.ShouldBeTrue();
    }

    [Fact]
    public async Task UseTicket_AlreadyUsed_Returns409Async()
    {
        var venueId = await CreateVenueAsync();
        var ev = await CreateEventAsync(venueId);
        var purchaseResponse = await _client.PostAsJsonAsync($"/api/events/{ev.Id}/tickets",
            new PurchaseTicketsRequest("Jane Doe", "jane@example.com", 1));
        var tickets = await purchaseResponse.Content.ReadFromJsonAsync<List<TicketResponse>>();
        var code = tickets![0].TicketCode;

        await _client.PatchAsync($"/api/tickets/{code}/use", null);
        var response = await _client.PatchAsync($"/api/tickets/{code}/use", null);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UseTicket_NonExistent_Returns404Async()
    {
        var response = await _client.PatchAsync("/api/tickets/bad-code/use", null);
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── GET /api/events/{id}/attendees ───────────────────────────────────────

    [Fact]
    public async Task GetAttendees_AfterPurchase_ReturnsAttendeesAsync()
    {
        var venueId = await CreateVenueAsync();
        var ev = await CreateEventAsync(venueId);

        await _client.PostAsJsonAsync($"/api/events/{ev.Id}/tickets",
            new PurchaseTicketsRequest("John Doe", "john@example.com", 2));

        var response = await _client.GetAsync($"/api/events/{ev.Id}/attendees");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var attendees = await response.Content.ReadFromJsonAsync<List<AttendeeResponse>>();
        attendees!.Count.ShouldBe(2);
        attendees.All(a => a.BuyerName == "John Doe").ShouldBeTrue();
    }

    [Fact]
    public async Task GetAttendees_NoTickets_ReturnsEmptyAsync()
    {
        var venueId = await CreateVenueAsync();
        var ev = await CreateEventAsync(venueId);

        var response = await _client.GetAsync($"/api/events/{ev.Id}/attendees");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var attendees = await response.Content.ReadFromJsonAsync<List<AttendeeResponse>>();
        attendees!.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAttendees_NonExistent_Returns404Async()
    {
        var response = await _client.GetAsync("/api/events/99999/attendees");
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<int> CreateVenueAsync(int capacity = 1000)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var venue = new Venue
        {
            Name = _fixture.Create<string>(),
            Address = _fixture.Create<string>(),
            Capacity = capacity,
        };
        db.Venues.Add(venue);
        await db.SaveChangesAsync();
        return venue.Id;
    }

    private async Task<EventResponse> CreateEventAsync(int venueId, int totalTickets = 100)
    {
        var response = await _client.PostAsJsonAsync("/api/events", ValidCreateRequest(venueId, totalTickets));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EventResponse>())!;
    }

    private static CreateEventRequest ValidCreateRequest(int venueId, int totalTickets = 100) => new(
        Title: "Test Event",
        Description: "Test event description",
        VenueId: venueId,
        Date: new DateOnly(2027, 6, 15),
        StartTime: new TimeOnly(18, 0),
        EndTime: new TimeOnly(22, 0),
        TotalTickets: totalTickets,
        TicketPrice: 50m);

    private static UpdateEventRequest ValidUpdateRequest(int venueId) => new(
        Title: "Updated Event",
        Description: "Updated description",
        VenueId: venueId,
        Date: new DateOnly(2027, 6, 15),
        StartTime: new TimeOnly(18, 0),
        EndTime: new TimeOnly(22, 0),
        TotalTickets: 100,
        TicketPrice: 50m);
}
