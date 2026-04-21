using Bogus;
using TicketSales.Domain;
using TicketSales.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace TicketSales.Api.Tests.Database;

public class EventsDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .Build();

    public AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        return new AppDbContext(options);
    }

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();

        using var db = CreateDbContext();
        await db.Database.EnsureCreatedAsync();

        await SeedDataAsync(db);
    }

    private static async Task SeedDataAsync(AppDbContext db)
    {
        var faker = new Faker();
        var random = new Random(42);

        const int totalVenues = 50;
        const int totalEvents = 500;
        const int totalTickets = 10_000;
        const int batchSize = 500;

        // Seed Venues
        var venues = new List<Venue>(totalVenues);
        for (var i = 0; i < totalVenues; i++)
        {
            venues.Add(new Venue
            {
                Name = faker.Company.CompanyName(),
                Address = faker.Address.FullAddress(),
                Capacity = random.Next(100, 5000),
            });
        }
        db.Venues.AddRange(venues);
        await db.SaveChangesAsync();

        // Seed Events
        var events = new List<Event>(totalEvents);
        for (var i = 0; i < totalEvents; i++)
        {
            var venue = venues[i % totalVenues];
            var totalTix = random.Next(10, Math.Min(venue.Capacity, 500));
            var available = random.Next(0, totalTix + 1);
            var startHour = random.Next(9, 20);
            events.Add(new Event
            {
                Title = faker.Lorem.Sentence(4),
                Description = faker.Lorem.Paragraph(),
                VenueId = venue.Id,
                Date = DateOnly.FromDateTime(faker.Date.Future(2).ToUniversalTime()),
                StartTime = new TimeOnly(startHour, 0),
                EndTime = new TimeOnly(Math.Min(startHour + 2, 23), 59),
                TotalTickets = totalTix,
                AvailableTickets = available,
                TicketPrice = Math.Round((decimal)(random.NextDouble() * 200), 2),
            });
        }

        for (var batch = 0; batch < totalEvents; batch += batchSize)
        {
            var chunk = events.Skip(batch).Take(batchSize).ToList();
            db.Events.AddRange(chunk);
            await db.SaveChangesAsync();
        }

        // Seed Tickets (10,000 tickets spread across first 200 events)
        var eventSlice = events.Take(200).ToList();
        var allTickets = new List<Ticket>(totalTickets);

        for (var i = 0; i < totalTickets; i++)
        {
            var ev = eventSlice[i % eventSlice.Count];
            allTickets.Add(new Ticket
            {
                EventId = ev.Id,
                BuyerName = faker.Name.FullName(),
                BuyerEmail = faker.Internet.Email(),
                PurchaseDate = faker.Date.Past(1).ToUniversalTime(),
                TicketCode = Guid.NewGuid().ToString(),
                IsUsed = random.Next(2) == 0,
            });
        }

        for (var batch = 0; batch < totalTickets; batch += batchSize)
        {
            var chunk = allTickets.Skip(batch).Take(batchSize).ToList();
            db.Tickets.AddRange(chunk);
            await db.SaveChangesAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }
}
