using TicketSales.Application.Events.Requests;
using TicketSales.Application.Events.Validators;
using FluentValidation.TestHelper;

namespace TicketSales.Api.Tests.Events.Validations;

public class CreateEventRequestValidatorTests
{
    private static readonly CreateEventRequestValidator Validator = new();
    private static readonly DateOnly FutureDate = new(2027, 6, 15);

    [Fact]
    public void ValidRequest_PassesAllRules()
    {
        var result = Validator.TestValidate(ValidRequest());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Title_Empty_HasError()
    {
        var result = Validator.TestValidate(ValidRequest() with { Title = "" });
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Title_TooLong_HasError()
    {
        var result = Validator.TestValidate(ValidRequest() with { Title = new string('A', 201) });
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Title_AtMaxLength_NoError()
    {
        var result = Validator.TestValidate(ValidRequest() with { Title = new string('A', 200) });
        result.ShouldNotHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Description_Empty_HasError()
    {
        var result = Validator.TestValidate(ValidRequest() with { Description = "" });
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Description_TooLong_HasError()
    {
        var result = Validator.TestValidate(ValidRequest() with { Description = new string('A', 2001) });
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void VenueId_Zero_HasError()
    {
        var result = Validator.TestValidate(ValidRequest() with { VenueId = 0 });
        result.ShouldHaveValidationErrorFor(x => x.VenueId);
    }

    [Fact]
    public void VenueId_Negative_HasError()
    {
        var result = Validator.TestValidate(ValidRequest() with { VenueId = -1 });
        result.ShouldHaveValidationErrorFor(x => x.VenueId);
    }

    [Fact]
    public void VenueId_Positive_NoError()
    {
        var result = Validator.TestValidate(ValidRequest() with { VenueId = 1 });
        result.ShouldNotHaveValidationErrorFor(x => x.VenueId);
    }

    [Fact]
    public void Date_InPast_HasError()
    {
        var result = Validator.TestValidate(ValidRequest() with { Date = new DateOnly(2020, 1, 1) });
        result.ShouldHaveValidationErrorFor(x => x.Date);
    }

    [Fact]
    public void EndTime_BeforeStartTime_HasError()
    {
        var result = Validator.TestValidate(ValidRequest() with { EndTime = new TimeOnly(9, 0) });
        result.ShouldHaveValidationErrorFor(x => x.EndTime);
    }

    [Fact]
    public void EndTime_AfterStartTime_NoError()
    {
        var result = Validator.TestValidate(ValidRequest() with { EndTime = new TimeOnly(22, 0) });
        result.ShouldNotHaveValidationErrorFor(x => x.EndTime);
    }

    [Fact]
    public void TotalTickets_Zero_HasError()
    {
        var result = Validator.TestValidate(ValidRequest() with { TotalTickets = 0 });
        result.ShouldHaveValidationErrorFor(x => x.TotalTickets);
    }

    [Fact]
    public void TotalTickets_Negative_HasError()
    {
        var result = Validator.TestValidate(ValidRequest() with { TotalTickets = -5 });
        result.ShouldHaveValidationErrorFor(x => x.TotalTickets);
    }

    [Fact]
    public void TotalTickets_Positive_NoError()
    {
        var result = Validator.TestValidate(ValidRequest() with { TotalTickets = 100 });
        result.ShouldNotHaveValidationErrorFor(x => x.TotalTickets);
    }

    [Fact]
    public void TicketPrice_Negative_HasError()
    {
        var result = Validator.TestValidate(ValidRequest() with { TicketPrice = -1m });
        result.ShouldHaveValidationErrorFor(x => x.TicketPrice);
    }

    [Fact]
    public void TicketPrice_Zero_NoError()
    {
        var result = Validator.TestValidate(ValidRequest() with { TicketPrice = 0m });
        result.ShouldNotHaveValidationErrorFor(x => x.TicketPrice);
    }

    [Fact]
    public void TicketPrice_Positive_NoError()
    {
        var result = Validator.TestValidate(ValidRequest() with { TicketPrice = 99.99m });
        result.ShouldNotHaveValidationErrorFor(x => x.TicketPrice);
    }

    private static CreateEventRequest ValidRequest() => new(
        Title: "Concert Night",
        Description: "An amazing concert",
        VenueId: 1,
        Date: FutureDate,
        StartTime: new TimeOnly(18, 0),
        EndTime: new TimeOnly(22, 0),
        TotalTickets: 100,
        TicketPrice: 50m);
}
