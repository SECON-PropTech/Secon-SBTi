using FluentAssertions;
using SeconSbtiBackend.Controllers;
using SeconSbtiBackend.Models;
using tests.DatabaseTests;

namespace tests.ControllersTests;

public class ProductGroupsControllerTests
{
    /// <summary>
    /// Tests the GetAll method of the ProductGroupsController class to make sure all groups are returned sorted by name.
    /// </summary>
    [Fact]
    public async Task GetAll_returns_all_groups_sorted_by_name()
    {
        using var dbContext = TestDatabase.Create();
        dbContext.ProductGroups.AddRange(
            new ProductGroup { Id = Guid.NewGuid(), Name = "Pumpar" },
            new ProductGroup { Id = Guid.NewGuid(), Name = "Fläktar", Description = "Ventilation" });
        await dbContext.SaveChangesAsync();

        var productGroups = await new ProductGroupsController(dbContext).GetAll();

        productGroups.Select(productGroup => productGroup.Name).Should().Equal("Fläktar", "Pumpar");
        productGroups[0].Description.Should().Be("Ventilation");
    }
}