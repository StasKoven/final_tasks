namespace TicketSales.Application.Events.Requests;

public record CreateEventRequest(
    string Title,
    string Description,
    int VenueId,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int TotalTickets,
    decimal TicketPrice);

public record UpdateEventRequest(
    string Title,
    string Description,
    int VenueId,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int TotalTickets,
    decimal TicketPrice);

public record PurchaseTicketsRequest(
    string BuyerName,
    string BuyerEmail,
    int Quantity);
