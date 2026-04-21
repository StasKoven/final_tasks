using TicketSales.Application.Abstractions;
using TicketSales.Application.Events.Requests;
using TicketSales.Application.Events.Responses;
using TicketSales.Domain;
using TicketSales.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TicketSales.Infrastructure.Services;

public class EventService(AppDbContext db, TimeProvider timeProvider) : IEventService
{
    public async Task<IReadOnlyList<EventResponse>> GetUpcomingEventsAsync(DateOnly? date, int? venueId)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var query = db.Events.Include(e => e.Venue).Where(e => e.Date >= today);

        if (date.HasValue)
            query = query.Where(e => e.Date == date.Value);

        if (venueId.HasValue)
            query = query.Where(e => e.VenueId == venueId.Value);

        query = query.OrderBy(e => e.Date).ThenBy(e => e.StartTime);

        var events = await query.ToListAsync();
        return events.Select(ToResponse).ToList();
    }

    public async Task<EventDetailResponse> GetEventByIdAsync(int id)
    {
        var ev = await db.Events
            .Include(e => e.Venue)
            .Include(e => e.Tickets)
            .FirstOrDefaultAsync(e => e.Id == id)
            ?? throw new KeyNotFoundException($"Event {id} not found.");

        return ToDetailResponse(ev);
    }

    public async Task<EventResponse> CreateEventAsync(CreateEventRequest request)
    {
        var venue = await db.Venues.FindAsync(request.VenueId)
            ?? throw new KeyNotFoundException($"Venue {request.VenueId} not found.");

        if (request.TotalTickets > venue.Capacity)
            throw new InvalidOperationException(
                $"TotalTickets ({request.TotalTickets}) exceeds venue capacity ({venue.Capacity}).");

        var ev = new Event
        {
            Title = request.Title,
            Description = request.Description,
            VenueId = request.VenueId,
            Date = request.Date,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            TotalTickets = request.TotalTickets,
            AvailableTickets = request.TotalTickets,
            TicketPrice = request.TicketPrice,
        };

        db.Events.Add(ev);
        await db.SaveChangesAsync();
        await db.Entry(ev).Reference(e => e.Venue).LoadAsync();
        return ToResponse(ev);
    }

    public async Task<EventResponse> UpdateEventAsync(int id, UpdateEventRequest request)
    {
        var ev = await db.Events.Include(e => e.Venue).FirstOrDefaultAsync(e => e.Id == id)
            ?? throw new KeyNotFoundException($"Event {id} not found.");

        var venue = await db.Venues.FindAsync(request.VenueId)
            ?? throw new KeyNotFoundException($"Venue {request.VenueId} not found.");

        if (request.TotalTickets > venue.Capacity)
            throw new InvalidOperationException(
                $"TotalTickets ({request.TotalTickets}) exceeds venue capacity ({venue.Capacity}).");

        var soldTickets = ev.TotalTickets - ev.AvailableTickets;
        ev.Title = request.Title;
        ev.Description = request.Description;
        ev.VenueId = request.VenueId;
        ev.Date = request.Date;
        ev.StartTime = request.StartTime;
        ev.EndTime = request.EndTime;
        ev.TotalTickets = request.TotalTickets;
        ev.AvailableTickets = request.TotalTickets - soldTickets;
        ev.TicketPrice = request.TicketPrice;
        ev.Venue = venue;

        await db.SaveChangesAsync();
        return ToResponse(ev);
    }

    public async Task<IReadOnlyList<TicketResponse>> PurchaseTicketsAsync(int eventId, PurchaseTicketsRequest request)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var ev = await db.Events.FindAsync(eventId)
            ?? throw new KeyNotFoundException($"Event {eventId} not found.");

        if (ev.Date < today)
            throw new InvalidOperationException("Cannot purchase tickets for past events.");

        if (ev.AvailableTickets < request.Quantity)
            throw new InvalidOperationException(
                $"Not enough tickets available. Requested: {request.Quantity}, Available: {ev.AvailableTickets}.");

        var purchaseDate = timeProvider.GetUtcNow().UtcDateTime;
        var tickets = new List<Ticket>(request.Quantity);

        for (var i = 0; i < request.Quantity; i++)
        {
            tickets.Add(new Ticket
            {
                EventId = eventId,
                BuyerName = request.BuyerName,
                BuyerEmail = request.BuyerEmail,
                PurchaseDate = purchaseDate,
                TicketCode = Guid.NewGuid().ToString(),
                IsUsed = false,
            });
        }

        ev.AvailableTickets -= request.Quantity;
        db.Tickets.AddRange(tickets);
        await db.SaveChangesAsync();

        return tickets.Select(ToTicketResponse).ToList();
    }

    public async Task<TicketResponse> GetTicketByCodeAsync(string code)
    {
        var ticket = await db.Tickets.FirstOrDefaultAsync(t => t.TicketCode == code)
            ?? throw new KeyNotFoundException($"Ticket with code '{code}' not found.");

        return ToTicketResponse(ticket);
    }

    public async Task<TicketResponse> UseTicketAsync(string code)
    {
        var ticket = await db.Tickets.FirstOrDefaultAsync(t => t.TicketCode == code)
            ?? throw new KeyNotFoundException($"Ticket with code '{code}' not found.");

        if (ticket.IsUsed)
            throw new InvalidOperationException($"Ticket '{code}' has already been used.");

        ticket.IsUsed = true;
        await db.SaveChangesAsync();
        return ToTicketResponse(ticket);
    }

    public async Task<IReadOnlyList<AttendeeResponse>> GetAttendeesAsync(int eventId)
    {
        var exists = await db.Events.AnyAsync(e => e.Id == eventId);
        if (!exists)
            throw new KeyNotFoundException($"Event {eventId} not found.");

        var tickets = await db.Tickets
            .Where(t => t.EventId == eventId)
            .OrderBy(t => t.PurchaseDate)
            .ToListAsync();

        return tickets.Select(t => new AttendeeResponse(
            t.BuyerName,
            t.BuyerEmail,
            t.TicketCode,
            t.PurchaseDate,
            t.IsUsed)).ToList();
    }

    private static EventResponse ToResponse(Event ev) => new(
        ev.Id,
        ev.Title,
        ev.Description,
        ev.VenueId,
        ev.Venue?.Name ?? string.Empty,
        ev.Date,
        ev.StartTime,
        ev.EndTime,
        ev.TotalTickets,
        ev.AvailableTickets,
        ev.TicketPrice);

    private static EventDetailResponse ToDetailResponse(Event ev) => new(
        ev.Id,
        ev.Title,
        ev.Description,
        ev.VenueId,
        ev.Venue?.Name ?? string.Empty,
        ev.Date,
        ev.StartTime,
        ev.EndTime,
        ev.TotalTickets,
        ev.AvailableTickets,
        ev.TicketPrice,
        ev.Tickets.Count);

    private static TicketResponse ToTicketResponse(Ticket t) => new(
        t.Id,
        t.EventId,
        t.BuyerName,
        t.BuyerEmail,
        t.PurchaseDate,
        t.TicketCode,
        t.IsUsed);
}
