namespace TicketSales.Domain;

public class Ticket
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public string BuyerEmail { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; }
    public string TicketCode { get; set; } = string.Empty;
    public bool IsUsed { get; set; }

    public Event Event { get; set; } = null!;
}
