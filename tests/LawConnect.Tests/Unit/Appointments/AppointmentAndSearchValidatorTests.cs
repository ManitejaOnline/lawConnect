using FluentValidation.TestHelper;
using LawConnect.Application.Features.Appointments.Commands.BookAppointment;
using LawConnect.Application.Features.Lawyers.Queries.SearchLawyers;
using LawConnect.Application.Common.Geo;

namespace LawConnect.Tests.Unit.Appointments;

public sealed class AppointmentAndSearchValidatorTests
{
    [Fact]
    public void BookAppointment_EndBeforeStart_Fails()
    {
        var validator = new BookAppointmentCommandValidator();
        var start = DateTime.UtcNow.AddDays(1);
        var result = validator.TestValidate(new BookAppointmentCommand(
            Guid.NewGuid(),
            start,
            start.AddMinutes(-30),
            null,
            "Asia/Kolkata"));

        result.ShouldHaveValidationErrorFor(x => x);
    }

    [Fact]
    public void SearchLawyers_InvalidSort_Fails()
    {
        var validator = new SearchLawyersQueryValidator();
        var result = validator.TestValidate(new SearchLawyersQuery(
            null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
            "unknown",
            false,
            1,
            20));

        result.ShouldHaveValidationErrorFor(x => x.SortBy);
    }

    [Fact]
    public void GeoDistance_SamePoint_IsZero()
    {
        var distance = GeoDistance.Kilometers(12.97, 77.59, 12.97, 77.59);
        Assert.True(distance < 0.001);
    }
}
