using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using SeconSbtiBackend.DTOs;

namespace tests.DTOsTests;

public class MaterialLineRequestTests
{
    /// <summary>
    /// Tests the validation of MaterialLineRequest to make sure whole numbers are accepted, including 0.
    /// </summary>
    [Theory]
    [InlineData(0d)]
    [InlineData(3d)]
    public void Whole_number_quantity_is_allowed(double quantity)
    {
        InvalidFields(ValidRequest((decimal)quantity))
            .Should().BeEmpty();
    }

    /// <summary>
    /// Tests the validation of MaterialLineRequest to make sure decimals are rejected, since products are counted in pieces.
    /// </summary>
    [Theory]
    [InlineData(3.5d)]
    [InlineData(0.1d)]
    public void Decimal_quantity_is_rejected(double quantity)
    {
        InvalidFields(ValidRequest((decimal)quantity))
            .Should().Equal(nameof(MaterialLineRequest.Quantity));
    }

    /// <summary>
    /// Tests the validation of MaterialLineRequest to make sure a missing quantity gives one error from [Required], not two.
    /// </summary>
    [Fact]
    public void Missing_quantity_gives_one_error()
    {
        InvalidFields(ValidRequest(quantity: null))
            .Should().Equal(nameof(MaterialLineRequest.Quantity));
    }

    /// <summary>
    /// Test helper: a request where every field except the quantity is valid.
    /// </summary>
    private static MaterialLineRequest ValidRequest(decimal? quantity) => new()
    {
        ProductId = Guid.NewGuid(),
        Quantity = quantity,
        Date = new DateOnly(2025, 4, 1),
        MeasurementQuality = "measured"
    };

    /// <summary>
    /// Test helper: runs the same validation rules as the API and returns the names of the invalid fields.
    /// </summary>
    private static List<string> InvalidFields(MaterialLineRequest request)
    {
        var validationResults = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), validationResults, validateAllProperties: true);
        return validationResults.SelectMany(validationResult => validationResult.MemberNames).ToList();
    }
}
