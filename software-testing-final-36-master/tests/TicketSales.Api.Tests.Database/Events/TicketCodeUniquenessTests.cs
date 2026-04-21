using TicketSales.Domain;
using Microsoft.EntityFrameworkCore;

namespace TicketSales.Api.Tests.Database.Tickets;

public class TicketCodeUniquenessTests : IClassFixture<EventsDatabaseFixture>
{
    private readonly EventsDatabaseFixture _fixture;

    public TicketCodeUniquenessTests(EventsDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task TicketCode_UniqueConstraint_PreventsDuplicatesAtDatabaseLevelAsync()
    {
        await using var db = _fixture.CreateDbContext();
        var existingTicket = await db.Tickets.FirstAsync();
        var existingCode = existingTicket.TicketCode;
        var eventId = existingTicket.EventId;

        await using var db2 = _fixture.CreateDbContext();
        db2.Tickets.Add(new Ticket
        {
            EventId = eventId,
            BuyerName = "Duplicate Buyer",
            BuyerEmail = "dup@example.com",
            PurchaseDate = DateTime.UtcNow,
            TicketCode = existingCode,
            IsUsed = false,
        });

        var ex = await Should.ThrowAsync<Exception>(() => db2.SaveChangesAsync());
        ex.ShouldNotBeNull();
    }

    [Fact]
    public async Task TicketCode_DifferentCodes_AreAllowedAsync()
    {
        await using var db = _fixture.CreateDbContext();
        var ev = await db.Events.FirstAsync();

        var code1 = Guid.NewGuid().ToString();
        var code2 = Guid.NewGuid().ToString();

        db.Tickets.AddRange(
            new Ticket { EventId = ev.Id, BuyerName = "A", BuyerEmail = "a@example.com", PurchaseDate = DateTime.UtcNow, TicketCode = code1, IsUsed = false },
            new Ticket { EventId = ev.Id, BuyerName = "B", BuyerEmail = "b@example.com", PurchaseDate = DateTime.UtcNow, TicketCode = code2, IsUsed = false }
        );

        await db.SaveChangesAsync();

        var count = await db.Tickets.CountAsync(t => t.TicketCode == code1 || t.TicketCode == code2);
        count.ShouldBe(2);
    }

    [Fact]
    public async Task TicketCode_SameCodeDifferentEvents_IsRejectedAsync()
    {
        await using var db = _fixture.CreateDbContext();
        var events = await db.Events.Take(2).ToListAsync();
        events.Count.ShouldBe(2);

        var existingTicket = await db.Tickets.FirstAsync();
        var duplicateCode = existingTicket.TicketCode;

        await using var db2 = _fixture.CreateDbContext();
        db2.Tickets.Add(new Ticket
        {
            EventId = events[1].Id,
            BuyerName = "Cross Event",
            BuyerEmail = "cross@example.com",
            PurchaseDate = DateTime.UtcNow,
            TicketCode = duplicateCode,
            IsUsed = false,
        });

        // Unique constraint applies globally, not per event
        await Should.ThrowAsync<Exception>(() => db2.SaveChangesAsync());
    }
}
