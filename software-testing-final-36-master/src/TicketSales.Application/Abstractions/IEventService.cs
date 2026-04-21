using TicketSales.Application.Events.Requests;
using TicketSales.Application.Events.Responses;

namespace TicketSales.Application.Abstractions;

public interface IEventService
{
    Task<IReadOnlyList<EventResponse>> GetUpcomingEventsAsync(DateOnly? date, int? venueId);
    Task<EventDetailResponse> GetEventByIdAsync(int id);
    Task<EventResponse> CreateEventAsync(CreateEventRequest request);
    Task<EventResponse> UpdateEventAsync(int id, UpdateEventRequest request);
    Task<IReadOnlyList<TicketResponse>> PurchaseTicketsAsync(int eventId, PurchaseTicketsRequest request);
    Task<TicketResponse> GetTicketByCodeAsync(string code);
    Task<TicketResponse> UseTicketAsync(string code);
    Task<IReadOnlyList<AttendeeResponse>> GetAttendeesAsync(int eventId);
}
