using System.ComponentModel.DataAnnotations;
using SeconSbtiBackend.DTOs.Scope312;

namespace tests.DTOsTests;

public class UpdateProductMaterialWeightRequestTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    public void Weight_must_be_present_and_non_negative(
        int? weightKg,
        bool expectedValid)
    {
        var request = new UpdateProductMaterialWeightRequest
        {
            WeightKg = weightKg
        };

        var errors = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            errors,
            validateAllProperties: true);

        Assert.Equal(expectedValid, valid);
    }

    [Fact]
    public void Weight_accepts_decimal_values()
    {
        var request = new UpdateProductMaterialWeightRequest
        {
            WeightKg = 0.45m
        };

        var errors = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            errors,
            validateAllProperties: true);

        Assert.True(valid);
    }
}