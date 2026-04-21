namespace TicketSales.Application.Events.Responses;

public record EventResponse(
    int Id,
    string Title,
    string Description,
    int VenueId,
    string VenueName,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int TotalTickets,
    int AvailableTickets,
    decimal TicketPrice);

public record EventDetailResponse(
    int Id,
    string Title,
    string Description,
    int VenueId,
    string VenueName,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int TotalTickets,
    int AvailableTickets,
    decimal TicketPrice,
    int AttendeeCount);

public record TicketResponse(
    int Id,
    int EventId,
    string BuyerName,
    string BuyerEmail,
    DateTime PurchaseDate,
    string TicketCode,
    bool IsUsed);

public record AttendeeResponse(
    string BuyerName,
    string BuyerEmail,
    string TicketCode,
    DateTime PurchaseDate,
    bool IsUsed);
