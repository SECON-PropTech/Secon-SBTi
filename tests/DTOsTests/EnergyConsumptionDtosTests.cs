using FluentAssertions;
using SeconSbtiBackend.DTOs;

namespace tests.DTOsTests;

public class EnergyConsumptionDtosTests
{
    private static readonly Guid ProductId = Guid.NewGuid();
    private static readonly Guid GroupId = Guid.NewGuid();

    /// <summary>
    /// Test helper: a complete product row.
    /// </summary>
    private static EnergyProductRow CreateRow(IReadOnlyList<string>? missing = null) => new(
        ProductId, "ART-1", "Fläkt A", GroupId, "Fläktar",
        5m, 50m, 10m, "measured", "Uppmätt 2025", 2500m, missing ?? []);

    /************************EnergyProductRow************************/
    [Fact]
    public void EnergyProductRow_stores_all_values()
    {
        var missing = new List<string> { "kwh", "group" };
        var row = new EnergyProductRow(
            ProductId, "ART-1", "Fläkt A", GroupId, "Fläktar",
            5m, 50m, 10m, "measured", "Kommentar", 2500m, missing);

        row.ProductId.Should().Be(ProductId);
        row.ArticleNo.Should().Be("ART-1");
        row.Name.Should().Be("Fläkt A");
        row.GroupId.Should().Be(GroupId);
        row.GroupName.Should().Be("Fläktar");
        row.Quantity.Should().Be(5m);
        row.KwhPerYear.Should().Be(50m);
        row.LifetimeYears.Should().Be(10m);
        row.MeasurementQuality.Should().Be("measured");
        row.Comment.Should().Be("Kommentar");
        row.ActivityKwh.Should().Be(2500m);
        row.Missing.Should().Equal("kwh", "group");
    }

    [Fact]
    public void EnergyProductRow_allows_null_for_values_that_are_not_set()
    {
        var row = new EnergyProductRow(
            ProductId, null, "Fläkt A", null, null,
            1m, null, null, "estimated", null, null, ["kwh", "lifetime", "group"]);

        row.ArticleNo.Should().BeNull();
        row.GroupId.Should().BeNull();
        row.GroupName.Should().BeNull();
        row.KwhPerYear.Should().BeNull();
        row.LifetimeYears.Should().BeNull();
        row.Comment.Should().BeNull();
        row.ActivityKwh.Should().BeNull();
        row.Missing.Should().HaveCount(3);
    }

    [Fact]
    public void EnergyProductRow_with_expression_copies_the_record_and_changes_one_value()
    {
        var original = CreateRow();
        var copy = original with { Quantity = 99m };
        copy.Quantity.Should().Be(99m);
        copy.Name.Should().Be(original.Name);
        original.Quantity.Should().Be(5m);
    }

    [Fact]
    public void EnergyProductRow_is_equal_when_all_values_are_equal()
    {
        var missing = new List<string>();
        var first = CreateRow(missing);
        var second = CreateRow(missing);

        first.Should().Be(second);
        (first == second).Should().BeTrue();
        (first != second).Should().BeFalse();
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void EnergyProductRow_is_not_equal_when_a_value_differs()
    {
        var missing = new List<string>();
        var first = CreateRow(missing);
        var second = first with { Name = "Fläkt B" };
        first.Should().NotBe(second);
        (first != second).Should().BeTrue();
    }

    [Fact]
    public void EnergyProductRow_ToString_contains_type_name_and_values()
    {
        var text = CreateRow().ToString();
        text.Should().Contain(nameof(EnergyProductRow));
        text.Should().Contain("Fläkt A");
        text.Should().Contain("ART-1");
    }

    [Fact]
    public void EnergyProductRow_can_be_deconstructed()
    {
        var (productId, articleNo, name, groupId, groupName, quantity, kwh, lifetime, quality, comment, activity, missing)
            = CreateRow(["group"]);

        productId.Should().Be(ProductId);
        articleNo.Should().Be("ART-1");
        name.Should().Be("Fläkt A");
        groupId.Should().Be(GroupId);
        groupName.Should().Be("Fläktar");
        quantity.Should().Be(5m);
        kwh.Should().Be(50m);
        lifetime.Should().Be(10m);
        quality.Should().Be("measured");
        comment.Should().Be("Uppmätt 2025");
        activity.Should().Be(2500m);
        missing.Should().Equal("group");
    }

    /************************EnergyGroupSummary************************/
    [Fact]
    public void EnergyGroupSummary_stores_all_values()
    {
        var summary = new EnergyGroupSummary(GroupId, "Fläktar", 4000m, 80m, 3, 1);

        summary.GroupId.Should().Be(GroupId);
        summary.GroupName.Should().Be("Fläktar");
        summary.ActivityKwh.Should().Be(4000m);
        summary.SharePercent.Should().Be(80m);
        summary.ProductCount.Should().Be(3);
        summary.IncompleteCount.Should().Be(1);
    }

    [Fact]
    public void EnergyGroupSummary_allows_null_group_id_for_products_without_group()
    {
        var summary = new EnergyGroupSummary(null, "Ingen grupp", 1000m, 20m, 1, 0);
        summary.GroupId.Should().BeNull();
        summary.GroupName.Should().Be("Ingen grupp");
    }

    [Fact]
    public void EnergyGroupSummary_is_equal_when_all_values_are_equal()
    {
        var first = new EnergyGroupSummary(GroupId, "Fläktar", 4000m, 80m, 3, 1);
        var second = new EnergyGroupSummary(GroupId, "Fläktar", 4000m, 80m, 3, 1);
        first.Should().Be(second);
        (first == second).Should().BeTrue();
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void EnergyGroupSummary_is_not_equal_when_a_value_differs()
    {
        var first = new EnergyGroupSummary(GroupId, "Fläktar", 4000m, 80m, 3, 1);
        var second = first with { IncompleteCount = 2 };
        first.Should().NotBe(second);
        (first != second).Should().BeTrue();
        second.IncompleteCount.Should().Be(2);
    }

    [Fact]
    public void EnergyGroupSummary_ToString_contains_type_name_and_values()
    {
        var text = new EnergyGroupSummary(GroupId, "Fläktar", 4000m, 80m, 3, 1).ToString();
        text.Should().Contain(nameof(EnergyGroupSummary));
        text.Should().Contain("Fläktar");
        text.Should().Contain("4000");
    }

    [Fact]
    public void EnergyGroupSummary_can_be_deconstructed()
    {
        var (groupId, groupName, activity, share, productCount, incompleteCount) =
            new EnergyGroupSummary(GroupId, "Fläktar", 4000m, 80m, 3, 1);

        groupId.Should().Be(GroupId);
        groupName.Should().Be("Fläktar");
        activity.Should().Be(4000m);
        share.Should().Be(80m);
        productCount.Should().Be(3);
        incompleteCount.Should().Be(1);
    }

    /************************EnergyConsumptionResponse************************/
    [Fact]
    public void EnergyConsumptionResponse_stores_all_values()
    {
        var groups = new List<EnergyGroupSummary> { new(GroupId, "Fläktar", 2500m, 100m, 1, 0) };
        var products = new List<EnergyProductRow> { CreateRow() };
        var response = new EnergyConsumptionResponse(2025, 2500m, groups, products);

        response.Year.Should().Be(2025);
        response.TotalActivityKwh.Should().Be(2500m);
        response.Groups.Should().BeSameAs(groups);
        response.Products.Should().BeSameAs(products);
    }

    [Fact]
    public void EnergyConsumptionResponse_can_be_empty()
    {
        var response = new EnergyConsumptionResponse(2025, 0, [], []);
        response.TotalActivityKwh.Should().Be(0);
        response.Groups.Should().BeEmpty();
        response.Products.Should().BeEmpty();
    }

    [Fact]
    public void EnergyConsumptionResponse_is_equal_when_values_and_lists_are_the_same()
    {
        IReadOnlyList<EnergyGroupSummary> groups = [];
        IReadOnlyList<EnergyProductRow> products = [];

        var first = new EnergyConsumptionResponse(2025, 0, groups, products);
        var second = new EnergyConsumptionResponse(2025, 0, groups, products);

        first.Should().Be(second);
        (first == second).Should().BeTrue();
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void EnergyConsumptionResponse_is_not_equal_when_year_differs()
    {
        IReadOnlyList<EnergyGroupSummary> groups = [];
        IReadOnlyList<EnergyProductRow> products = [];

        var first = new EnergyConsumptionResponse(2025, 0, groups, products);
        var second = first with { Year = 2026 };

        first.Should().NotBe(second);
        (first != second).Should().BeTrue();
        second.Year.Should().Be(2026);
    }

    [Fact]
    public void EnergyConsumptionResponse_ToString_contains_type_name_and_year()
    {
        var text = new EnergyConsumptionResponse(2025, 0, [], []).ToString();
        text.Should().Contain(nameof(EnergyConsumptionResponse));
        text.Should().Contain("2025");
    }

    [Fact]
    public void EnergyConsumptionResponse_can_be_deconstructed()
    {
        IReadOnlyList<EnergyGroupSummary> groups = [];
        IReadOnlyList<EnergyProductRow> products = [];

        var (year, total, deconstructedGroups, deconstructedProducts) =
            new EnergyConsumptionResponse(2025, 123m, groups, products);

        year.Should().Be(2025);
        total.Should().Be(123m);
        deconstructedGroups.Should().BeSameAs(groups);
        deconstructedProducts.Should().BeSameAs(products);
    }
}