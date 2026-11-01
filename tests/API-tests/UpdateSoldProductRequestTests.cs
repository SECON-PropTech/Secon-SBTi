using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using SeconSbtiBackend.DTOs;

namespace api_tests;

public class UpdateSoldProductRequestTests
{
    /// <summary>
    /// Tests the validation of UpdateSoldProductRequest to make sure 0 kWh/year is allowed (product uses no electricity).
    /// </summary>
    [Fact]
    public void Zero_kwh_is_allowed()
    {
        InvalidFields(new UpdateSoldProductRequest { KwhPerYear = 0, MeasurementQuality = "estimated" }).Should().BeEmpty();
    }

    /// <summary>
    /// Tests the validation of UpdateSoldProductRequest to make sure missing values (null) are allowed.
    /// </summary>
    [Fact]
    public void Missing_values_are_allowed()
    {
        InvalidFields(new UpdateSoldProductRequest { MeasurementQuality = "estimated" }).Should().BeEmpty();
    }

    /// <summary>
    /// Tests the validation of UpdateSoldProductRequest to make sure negative kWh/year is rejected.
    /// </summary>
    [Fact]
    public void Negative_kwh_is_rejected()
    {
        InvalidFields(new UpdateSoldProductRequest { KwhPerYear = -1, MeasurementQuality = "estimated" })
            .Should().Equal(nameof(UpdateSoldProductRequest.KwhPerYear));
    }

    /// <summary>
    /// Tests the validation of UpdateSoldProductRequest to make sure a lifetime of 0 years is rejected, same rule as the database.
    /// </summary>
    [Fact]
    public void Zero_lifetime_is_rejected()
    {
        InvalidFields(new UpdateSoldProductRequest { LifetimeYears = 0, MeasurementQuality = "estimated" })
            .Should().Equal(nameof(UpdateSoldProductRequest.LifetimeYears));
    }

    /// <summary>
    /// Tests the validation of UpdateSoldProductRequest to make sure only "measured" and "estimated" are accepted.
    /// </summary>
    [Fact]
    public void Unknown_measurement_quality_is_rejected()
    {
        InvalidFields(new UpdateSoldProductRequest { MeasurementQuality = "guessed" })
            .Should().Equal(nameof(UpdateSoldProductRequest.MeasurementQuality));
    }

    /// <summary>
    /// Tests the validation of UpdateSoldProductRequest to make sure a too large kWh/year is rejected, since it could crash the report.
    /// </summary>
    [Fact]
    public void Too_large_kwh_is_rejected()
    {
        InvalidFields(new UpdateSoldProductRequest { KwhPerYear = 1_000_000_001, MeasurementQuality = "estimated" })
            .Should().Equal(nameof(UpdateSoldProductRequest.KwhPerYear));
    }

    /// <summary>
    /// Tests the validation of UpdateSoldProductRequest to make sure a too long lifetime is rejected, since it could crash the report.
    /// </summary>
    [Fact]
    public void Too_long_lifetime_is_rejected()
    {
        InvalidFields(new UpdateSoldProductRequest { LifetimeYears = 1_001, MeasurementQuality = "estimated" })
            .Should().Equal(nameof(UpdateSoldProductRequest.LifetimeYears));
    }


    /// <summary>
    /// Test helper: runs the same validation rules as the API and returns the names of the invalid fields.
    /// </summary>
    private static List<string> InvalidFields(UpdateSoldProductRequest request)
    {
        var validationResults = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), validationResults, validateAllProperties: true);
        return validationResults.SelectMany(validationResult => validationResult.MemberNames).ToList();
    }
}