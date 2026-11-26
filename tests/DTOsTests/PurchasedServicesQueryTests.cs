using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using SeconSbtiBackend.DTOs;

namespace tests.DTOsTests;

public class PurchasedServicesQueryTests
{
    /// <summary>
    /// Test helper: runs the same validation as the API (attributes first, then IValidatableObject).
    /// </summary>
    private static List<ValidationResult> Validate(PurchasedServicesQuery query)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(query, new ValidationContext(query), results, validateAllProperties: true);
        return results;
    }

    /****************Valid combinations*******************/
    [Fact]
    public void No_filter_is_valid()
    {
        Validate(new PurchasedServicesQuery()).Should().BeEmpty();
    }

    [Fact]
    public void Year_only_is_valid()
    {
        Validate(new PurchasedServicesQuery { Year = 2026 }).Should().BeEmpty();
    }

    [Fact]
    public void Year_and_month_is_valid()
    {
        Validate(new PurchasedServicesQuery { Year = 2026, Month = 11 }).Should().BeEmpty();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(9999)]
    public void Year_on_the_boundaries_is_valid(int year)
    {
        Validate(new PurchasedServicesQuery { Year = year }).Should().BeEmpty();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(12)]
    public void Month_on_the_boundaries_is_valid(int month)
    {
        Validate(new PurchasedServicesQuery { Year = 2026, Month = month }).Should().BeEmpty();
    }

    /****************Month without year (IValidatableObject)*******************/
    [Fact]
    public void Month_without_year_is_invalid_with_error_on_year()
    {
        var results = Validate(new PurchasedServicesQuery { Month = 3 });

        var error = results.Should().ContainSingle().Subject;
        error.ErrorMessage.Should().Be("Ange ett år när du filtrerar på månad.");
        error.MemberNames.Should().Equal("year");
    }

    [Fact]
    public void Validate_returns_error_for_month_without_year()
    {
        var query = new PurchasedServicesQuery { Month = 12 };
        var results = query.Validate(new ValidationContext(query)).ToList();
        results.Should().ContainSingle().Which.MemberNames.Should().Equal("year");
    }

    [Fact]
    public void Validate_returns_nothing_when_year_is_given()
    {
        var withMonth = new PurchasedServicesQuery { Year = 2026, Month = 3 };
        var withoutMonth = new PurchasedServicesQuery { Year = 2026 };
        var empty = new PurchasedServicesQuery();

        withMonth.Validate(new ValidationContext(withMonth)).Should().BeEmpty();
        withoutMonth.Validate(new ValidationContext(withoutMonth)).Should().BeEmpty();
        empty.Validate(new ValidationContext(empty)).Should().BeEmpty();
    }

    /****************Range attributes*******************/
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10000)]
    public void Year_outside_range_is_invalid(int year)
    {
        var results = Validate(new PurchasedServicesQuery { Year = year });

        var error = results.Should().ContainSingle().Subject;
        error.ErrorMessage.Should().Be("Ange ett år mellan 1 och 9999.");
        error.MemberNames.Should().Equal(nameof(PurchasedServicesQuery.Year));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(13)]
    public void Month_outside_range_is_invalid(int month)
    {
        var results = Validate(new PurchasedServicesQuery { Year = 2026, Month = month });
        var error = results.Should().ContainSingle().Subject;
        error.ErrorMessage.Should().Be("Ange en månad mellan 1 och 12.");
        error.MemberNames.Should().Equal(nameof(PurchasedServicesQuery.Month));
    }

    [Fact]
    public void Invalid_year_and_month_gives_one_error_per_field()
    {
        var results = Validate(new PurchasedServicesQuery { Year = 0, Month = 13 });
        results.SelectMany(result => result.MemberNames).Should()
            .BeEquivalentTo(nameof(PurchasedServicesQuery.Year), nameof(PurchasedServicesQuery.Month));
    }
}