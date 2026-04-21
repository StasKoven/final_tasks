using TicketSales.Application.Abstractions;
using TicketSales.Application.Events.Responses;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace TicketSales.Api.Tickets;

[ApiController]
[Route("api/tickets")]
public class TicketsController(IEventService eventService) : ControllerBase
{
    [HttpGet("{code}")]
    public async Task<Results<Ok<TicketResponse>, NotFound>> GetByCode(string code)
    {
        try
        {
            var result = await eventService.GetTicketByCodeAsync(code);
            return TypedResults.Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
    }

    [HttpPatch("{code}/use")]
    public async Task<Results<Ok<TicketResponse>, NotFound, Conflict<string>>> UseTicket(string code)
    {
        try
        {
            var result = await eventService.UseTicketAsync(code);
            return TypedResults.Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Conflict(ex.Message);
        }
    }
}
