namespace TicketSales.Domain;

public class Event
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int VenueId { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int TotalTickets { get; set; }
    public int AvailableTickets { get; set; }
    public decimal TicketPrice { get; set; }

    public Venue Venue { get; set; } = null!;
    public List<Ticket> Tickets { get; set; } = [];
}
