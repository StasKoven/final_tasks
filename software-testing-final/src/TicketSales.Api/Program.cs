using TicketSales.Application.Abstractions;
using TicketSales.Infrastructure.Persistence;
using TicketSales.Infrastructure.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddValidatorsFromAssemblyContaining<TicketSales.Application.ApplicationAssembly>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();

    if (!db.Venues.Any())
    {
        db.Venues.AddRange(
            new TicketSales.Domain.Venue { Name = "Main Arena", Address = "1 Arena Blvd", Capacity = 10000 },
            new TicketSales.Domain.Venue { Name = "City Hall", Address = "2 City Center", Capacity = 500 },
            new TicketSales.Domain.Venue { Name = "Open Air Stage", Address = "3 Park Ave", Capacity = 5000 }
        );
        await db.SaveChangesAsync();
    }
}

app.MapControllers();

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => true
});

app.Run();

public partial class Program;
