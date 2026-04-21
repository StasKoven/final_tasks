using TicketSales.Application.Abstractions;
using TicketSales.Application.Events.Requests;
using TicketSales.Application.Events.Responses;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace TicketSales.Api.Events;

[ApiController]
[Route("api/events")]
public class EventsController(
    IEventService eventService,
    IValidator<CreateEventRequest> createValidator,
    IValidator<UpdateEventRequest> updateValidator,
    IValidator<PurchaseTicketsRequest> purchaseValidator) : ControllerBase
{
    [HttpGet]
    public async Task<Ok<IReadOnlyList<EventResponse>>> GetAll(
        [FromQuery] DateOnly? date,
        [FromQuery] int? venueId)
    {
        var result = await eventService.GetUpcomingEventsAsync(date, venueId);
        return TypedResults.Ok(result);
    }

    [HttpPost]
    public async Task<Results<Created<EventResponse>, ValidationProblem, NotFound, Conflict<string>>> Create(
        [FromBody] CreateEventRequest request)
    {
        var validation = await createValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return TypedResults.ValidationProblem(validation.ToDictionary());

        try
        {
            var result = await eventService.CreateEventAsync(request);
            return TypedResults.Created($"/api/events/{result.Id}", result);
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

    [HttpGet("{id:int}")]
    public async Task<Results<Ok<EventDetailResponse>, NotFound>> GetById(int id)
    {
        try
        {
            var result = await eventService.GetEventByIdAsync(id);
            return TypedResults.Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
    }

    [HttpPut("{id:int}")]
    public async Task<Results<Ok<EventResponse>, NotFound, ValidationProblem, Conflict<string>>> Update(
        int id, [FromBody] UpdateEventRequest request)
    {
        var validation = await updateValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return TypedResults.ValidationProblem(validation.ToDictionary());

        try
        {
            var result = await eventService.UpdateEventAsync(id, request);
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

    [HttpPost("{id:int}/tickets")]
    public async Task<Results<Created<IReadOnlyList<TicketResponse>>, NotFound, ValidationProblem, Conflict<string>>> PurchaseTickets(
        int id, [FromBody] PurchaseTicketsRequest request)
    {
        var validation = await purchaseValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return TypedResults.ValidationProblem(validation.ToDictionary());

        try
        {
            var result = await eventService.PurchaseTicketsAsync(id, request);
            return TypedResults.Created($"/api/events/{id}/tickets", result);
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

    [HttpGet("{id:int}/attendees")]
    public async Task<Results<Ok<IReadOnlyList<AttendeeResponse>>, NotFound>> GetAttendees(int id)
    {
        try
        {
            var result = await eventService.GetAttendeesAsync(id);
            return TypedResults.Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
    }
}
