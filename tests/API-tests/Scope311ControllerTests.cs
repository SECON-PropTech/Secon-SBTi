using FluentAssertions;
using SeconSbtiBackend.Controllers;
using SeconSbtiBackend.Data;
using SeconSbtiBackend.Models;

namespace api_tests;

public class Scope311ControllerTests
{
    /// <summary>
    /// Tests the Get method of the Scope311Controller class to make sure only products sold in the requested year are included.
    /// </summary>
    [Fact]
    public async Task Get_only_includes_products_sold_in_the_requested_year()
    {
        using var dbContext = TestDatabase.Create();
        var soldIn2025 = AddProduct(dbContext, "Fläkt 2025");
        var soldIn2024 = AddProduct(dbContext, "Fläkt 2024");
        AddMaterialLine(dbContext, soldIn2025, quantity: 1, date: new DateOnly(2025, 6, 1));
        AddMaterialLine(dbContext, soldIn2024, quantity: 1, date: new DateOnly(2024, 12, 31));
        await dbContext.SaveChangesAsync();

        var report = await new Scope311Controller(dbContext).Get(2025);

        report.Products.Should().ContainSingle().Which.Name.Should().Be("Fläkt 2025");
    }

    /// <summary>
    /// Tests the Get method of the Scope311Controller class to make sure the quantity is summed from all material lines in the year.
    /// </summary>
    [Fact]
    public async Task Get_sums_quantity_from_all_material_lines_in_the_year()
    {
        using var dbContext = TestDatabase.Create();
        var product = AddProduct(dbContext, "Fläkt A");
        AddMaterialLine(dbContext, product, quantity: 2, date: new DateOnly(2025, 1, 1));
        AddMaterialLine(dbContext, product, quantity: 3, date: new DateOnly(2025, 12, 31));
        AddMaterialLine(dbContext, product, quantity: 100, date: new DateOnly(2026, 1, 1));   // next year, not counted
        await dbContext.SaveChangesAsync();

        var report = await new Scope311Controller(dbContext).Get(2025);

        report.Products.Single().Quantity.Should().Be(5);
        report.TotalActivityKwh.Should().Be(2500);   // 5 × 50 × 10
    }

    /// <summary>
    /// Tests the Get method of the Scope311Controller class to make sure an empty report is returned when nothing was sold that year.
    /// </summary>
    [Fact]
    public async Task Get_returns_empty_report_when_nothing_was_sold()
    {
        using var dbContext = TestDatabase.Create();

        var report = await new Scope311Controller(dbContext).Get(2025);

        report.Year.Should().Be(2025);
        report.TotalActivityKwh.Should().Be(0);
        report.Products.Should().BeEmpty();
        report.Groups.Should().BeEmpty();
    }

    /// <summary>
    /// Test helper: adds a product that uses 50 kWh/year for 10 years.
    /// </summary>
    private static SoldProduct AddProduct(ApplicationDbContext dbContext, string name)
    {
        var product = new SoldProduct
        {
            Id = Guid.NewGuid(),
            Name = name,
            KwhPerYear = 50,
            LifetimeYears = 10,
            MeasurementQuality = "estimated"
        };
        dbContext.SoldProducts.Add(product);
        return product;
    }

    /// <summary>
    /// Test helper: adds one sale (material line from Mobigo) of a product on a given date.
    /// </summary>
    private static void AddMaterialLine(ApplicationDbContext dbContext, SoldProduct product, decimal quantity, DateOnly date)
    {
        dbContext.MobigoMaterialLines.Add(new MobigoMaterialLine
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Quantity = quantity,
            Date = date,
            MeasurementQuality = "measured"
        });
    }
}