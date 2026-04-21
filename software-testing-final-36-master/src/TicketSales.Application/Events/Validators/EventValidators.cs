using FluentValidation;
using TicketSales.Application.Events.Requests;

namespace TicketSales.Application.Events.Validators;

public class CreateEventRequestValidator : AbstractValidator<CreateEventRequest>
{
    public CreateEventRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.");

        RuleFor(x => x.VenueId)
            .GreaterThan(0).WithMessage("VenueId must be a positive integer.");

        RuleFor(x => x.Date)
            .Must(d => d >= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Event date must not be in the past.");

        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("StartTime is required.");

        RuleFor(x => x.EndTime)
            .NotEmpty().WithMessage("EndTime is required.")
            .GreaterThan(x => x.StartTime).WithMessage("EndTime must be after StartTime.");

        RuleFor(x => x.TotalTickets)
            .GreaterThan(0).WithMessage("TotalTickets must be greater than 0.");

        RuleFor(x => x.TicketPrice)
            .GreaterThanOrEqualTo(0).WithMessage("TicketPrice must be non-negative.");
    }
}

public class UpdateEventRequestValidator : AbstractValidator<UpdateEventRequest>
{
    public UpdateEventRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.");

        RuleFor(x => x.VenueId)
            .GreaterThan(0).WithMessage("VenueId must be a positive integer.");

        RuleFor(x => x.TotalTickets)
            .GreaterThan(0).WithMessage("TotalTickets must be greater than 0.");

        RuleFor(x => x.TicketPrice)
            .GreaterThanOrEqualTo(0).WithMessage("TicketPrice must be non-negative.");
    }
}

public class PurchaseTicketsRequestValidator : AbstractValidator<PurchaseTicketsRequest>
{
    public PurchaseTicketsRequestValidator()
    {
        RuleFor(x => x.BuyerName)
            .NotEmpty().WithMessage("BuyerName is required.")
            .MaximumLength(100).WithMessage("BuyerName must not exceed 100 characters.");

        RuleFor(x => x.BuyerEmail)
            .NotEmpty().WithMessage("BuyerEmail is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than 0.")
            .LessThanOrEqualTo(10).WithMessage("You can purchase at most 10 tickets at once.");
    }
}
