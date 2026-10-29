using FluentAssertions;
using SeconSbtiBackend.Calculations;
using SeconSbtiBackend.Dtos;
using SeconSbtiBackend.Models;

namespace api_tests;

public class Scope311CalculatorTests
{
    /// <summary>
    /// Tests the Activity method of the Scope311Calculator class to make sure math is correct.
    /// </summary>
    [Fact]
    public void Activity_multiplies_quantity_kwh_and_lifetime()
    {
        Scope311Calculator.Activity(10, 50, 15).Should().Be(7500);
    }

    /// <summary>
    /// Tests the Activity method of the Scope311Calculator class to make sure it returns null when kWh/year is missing.
    /// </summary>
    [Fact]
    public void Activity_is_null_when_kwh_is_missing()
    {
        Scope311Calculator.Activity(10, null, 15).Should().BeNull();
    }

    /// <summary>
    /// Tests the Activity method of the Scope311Calculator class to make sure it returns null when lifetime is missing.
    /// </summary>
    [Fact]
    public void Activity_is_null_when_lifetime_is_missing()
    {
        Scope311Calculator.Activity(10, 50, null).Should().BeNull();
    }

    /// <summary>
    /// Tests the Activity method of the Scope311Calculator class to make sure it returns 0 when kWh/year is 0, even if lifetime is missing.
    /// </summary>
    [Fact]
    public void Activity_is_zero_when_kwh_is_zero_even_without_lifetime()
    {
        Scope311Calculator.Activity(10, 0, null).Should().Be(0);
    }

    /// <summary>
    /// Tests the Missing method of the Scope311Calculator class to make sure it returns an empty list when the product is complete.
    /// </summary>
    [Fact]
    public void Missing_is_empty_when_product_is_complete()
    {
        Scope311Calculator.Missing(50, 15, Guid.NewGuid()).Should().BeEmpty();
    }

    /// <summary>
    /// Tests the Missing method of the Scope311Calculator class to make sure it lists everything that is missing for a new product.
    /// </summary>
    [Fact]
    public void Missing_lists_everything_for_a_new_product()
    {
        Scope311Calculator.Missing(null, null, null).Should().Equal("kwh", "lifetime", "group");
    }

    /// <summary>
    /// Tests the Missing method of the Scope311Calculator class to make sure lifetime is not required when kWh/year is 0.
    /// </summary>
    [Fact]
    public void Missing_does_not_require_lifetime_when_kwh_is_zero()
    {
        Scope311Calculator.Missing(0, null, Guid.NewGuid()).Should().BeEmpty();
    }

    /// <summary>
    /// Test helper: product group shared by the test products.
    /// </summary>
    private static readonly ProductGroup FansGroup = new() { Id = Guid.NewGuid(), Name = "Fläktar" };

    /// <summary>
    /// Test helper: creates a sold product with only the fields the calculator needs.
    /// </summary>
    private static SoldProduct CreateProduct(string name, decimal? kwhPerYear, decimal? lifetimeYears, ProductGroup? group) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        KwhPerYear = kwhPerYear,
        LifetimeYears = lifetimeYears,
        GroupId = group?.Id,
        Group = group,
        MeasurementQuality = "estimated"
    };

    /// <summary>
    /// Test helper: two complete fans, one fan missing kWh and one pump without a group. Total 5000 kWh.
    /// </summary>
    private static Scope311Response CreateSampleReport() => Scope311Calculator.Build(2025,
    [
        (CreateProduct("Fläkt A", 50, 10, FansGroup), 2),         // 2 × 50 × 10 = 1000 kWh
        (CreateProduct("Fläkt B", 100, 10, FansGroup), 3),        // 3 × 100 × 10 = 3000 kWh
        (CreateProduct("Fläkt C", null, 10, FansGroup), 5),       // missing kWh, not counted
        (CreateProduct("Pump utan grupp", 10, 10, null), 10)      // 10 × 10 × 10 = 1000 kWh
    ]);

    /// <summary>
    /// Tests the Build method of the Scope311Calculator class to make sure the total only includes products with a calculated activity.
    /// </summary>
    [Fact]
    public void Build_total_only_counts_products_with_activity()
    {
        CreateSampleReport().TotalActivityKwh.Should().Be(5000);
    }

    /// <summary>
    /// Tests the Build method of the Scope311Calculator class to make sure a group gets the right sum, share, product count and number of incomplete products.
    /// </summary>
    [Fact]
    public void Build_sums_group_and_counts_incomplete_products()
    {
        var fansSummary = CreateSampleReport().Groups.Single(groupSummary => groupSummary.GroupName == "Fläktar");

        fansSummary.ActivityKwh.Should().Be(4000);
        fansSummary.SharePercent.Should().Be(80);
        fansSummary.ProductCount.Should().Be(3);
        fansSummary.IncompleteCount.Should().Be(1);
    }

    /// <summary>
    /// Tests the Build method of the Scope311Calculator class to make sure products without a group end up in "Ingen grupp".
    /// </summary>
    [Fact]
    public void Build_puts_products_without_group_in_ingen_grupp()
    {
        var noGroupSummary = CreateSampleReport().Groups.Single(groupSummary => groupSummary.GroupId == null);

        noGroupSummary.GroupName.Should().Be("Ingen grupp");
        noGroupSummary.SharePercent.Should().Be(20);
    }

    /// <summary>
    /// Tests the Build method of the Scope311Calculator class to make sure the share is 0 instead of dividing by zero when no product has activity.
    /// </summary>
    [Fact]
    public void Build_gives_zero_share_when_nothing_has_activity()
    {
        var report = Scope311Calculator.Build(2025, [(CreateProduct("Okänd produkt", null, null, FansGroup), 1)]);

        report.Groups.Single().SharePercent.Should().Be(0);
    }
}