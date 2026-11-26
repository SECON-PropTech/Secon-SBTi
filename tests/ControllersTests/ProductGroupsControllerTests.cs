using FluentAssertions;
using SeconSbtiBackend.Controllers;
using SeconSbtiBackend.Models;
using SeconSbtiBackend.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Http;

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

    /*************************Create****************************/
    [Fact]
    public async Task Create_creates_product_group_and_returns_201()
    {
        using var dbContext = TestDatabase.Create();
        var controller = new ProductGroupsController(dbContext);

        var result = await controller.Create(new ProductGroupRequest
        {
            Name = "Fläktar",
            Description = "Ventilation"
        });

        var response = result.Should().BeOfType<ObjectResult>().Subject;
        response.StatusCode.Should().Be(StatusCodes.Status201Created);

        var savedGroup = dbContext.ProductGroups.Single();
        savedGroup.Name.Should().Be("Fläktar");
        savedGroup.Description.Should().Be("Ventilation");
    }

    [Fact]
    public async Task Create_trims_whitespace_from_name()
    {
        using var dbContext = TestDatabase.Create();
        var controller = new ProductGroupsController(dbContext);

        await controller.Create(new ProductGroupRequest
        {
            Name = "  Fläktar  ",
            Description = "Ventilation"
        });

        dbContext.ProductGroups.Single().Name.Should().Be("Fläktar");
    }

    [Fact]
    public async Task Create_returns_400_when_name_already_exists()
    {
        using var dbContext = TestDatabase.Create();
        dbContext.ProductGroups.Add(new ProductGroup
        {
            Id = Guid.NewGuid(),
            Name = "Fläktar"
        });
        await dbContext.SaveChangesAsync();

        var controller = new ProductGroupsController(dbContext);
        var result = await controller.Create(new ProductGroupRequest
        {
            Name = "Fläktar",
            Description = "Ventilation"
        });

        var response = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        response.Value.Should().BeOfType<ValidationProblemDetails>();
        dbContext.ProductGroups.Should().HaveCount(1);
    }

    [Fact]
    public async Task Create_returns_400_when_name_exists_with_different_case()
    {
        using var dbContext = TestDatabase.Create();
        dbContext.ProductGroups.Add(new ProductGroup
        {
            Id = Guid.NewGuid(),
            Name = "Fläktar"
        });
        await dbContext.SaveChangesAsync();

        var controller = new ProductGroupsController(dbContext);
        var result = await controller.Create(new ProductGroupRequest
        {
            Name = "FLÄKTAR"
        });

        var response = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        response.Value.Should().BeOfType<ValidationProblemDetails>();
    }

    /************************Update*********************************/

    [Fact]
    public async Task Update_updates_product_group_and_returns_204()
    {
        using var dbContext = TestDatabase.Create();
        var id = Guid.NewGuid();

        dbContext.ProductGroups.Add(new ProductGroup
        {
            Id = id,
            Name = "Fläktar",
            Description = "Gammal beskrivning"
        });
        await dbContext.SaveChangesAsync();

        var controller = new ProductGroupsController(dbContext);
        var result = await controller.Update(id, new ProductGroupRequest
        {
            Name = "Pumpar",
            Description = "Ny beskrivning"
        });

        result.Should().BeOfType<NoContentResult>();
        var updatedGroup = await dbContext.ProductGroups.FindAsync(id);
        updatedGroup!.Name.Should().Be("Pumpar");
        updatedGroup.Description.Should().Be("Ny beskrivning");
    }

    [Fact]
    public async Task Update_returns_404_when_product_group_does_not_exist()
    {
        using var dbContext = TestDatabase.Create();
        var controller = new ProductGroupsController(dbContext);

        var result = await controller.Update(
            Guid.NewGuid(),
            new ProductGroupRequest { Name = "Fläktar" });

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Update_returns_400_when_name_is_already_taken()
    {
        using var dbContext = TestDatabase.Create();
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();

        dbContext.ProductGroups.AddRange(
            new ProductGroup { Id = firstId, Name = "Fläktar" },
            new ProductGroup { Id = secondId, Name = "Pumpar" });

        await dbContext.SaveChangesAsync();

        var controller = new ProductGroupsController(dbContext);
        var result = await controller.Update(secondId, new ProductGroupRequest
        {
            Name = "Fläktar"
        });

        var response = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        response.Value.Should().BeOfType<ValidationProblemDetails>();

        (await dbContext.ProductGroups.FindAsync(secondId))!.Name.Should().Be("Pumpar");
    }

    [Fact]
    public async Task Update_returns_400_when_name_matches_another_group_with_different_case()
    {
        using var dbContext = TestDatabase.Create();
        var pumpsId = Guid.NewGuid();
    
        dbContext.ProductGroups.AddRange(
            new ProductGroup { Id = Guid.NewGuid(), Name = "Fläktar" },
            new ProductGroup { Id = pumpsId, Name = "Pumpar" });
        await dbContext.SaveChangesAsync();
    
        var result = await new ProductGroupsController(dbContext).Update(pumpsId, new ProductGroupRequest { Name = "fläktar" });
        var response = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        response.Value.Should().BeOfType<ValidationProblemDetails>();
        (await dbContext.ProductGroups.FindAsync(pumpsId))!.Name.Should().Be("Pumpar");
    }

    [Fact]
    public async Task Update_allows_same_group_to_keep_its_name()
    {
        using var dbContext = TestDatabase.Create();
        var id = Guid.NewGuid();
        dbContext.ProductGroups.Add(new ProductGroup
        {
            Id = id,
            Name = "Fläktar"
        });
        await dbContext.SaveChangesAsync();

        var controller = new ProductGroupsController(dbContext);
        var result = await controller.Update(id, new ProductGroupRequest
        {
            Name = "Fläktar",
            Description = "Ny beskrivning"
        });
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Update_allows_clearing_description()
    {
        using var dbContext = TestDatabase.Create();
        var id = Guid.NewGuid();
        dbContext.ProductGroups.Add(new ProductGroup
        {
            Id = id,
            Name = "Fläktar",
            Description = "Ventilation"
        });
        await dbContext.SaveChangesAsync();

        var controller = new ProductGroupsController(dbContext);
        await controller.Update(id, new ProductGroupRequest
        {
            Name = "Fläktar",
            Description = null
        });
        (await dbContext.ProductGroups.FindAsync(id))!.Description.Should().BeNull();
    }

    /*******************************Delete***********************************/
    [Fact]
    public async Task Delete_returns_409_when_products_still_belong_to_the_group()
    {
        using var dbContext = TestDatabase.Create();
        var groupId = Guid.NewGuid();
    
        dbContext.ProductGroups.Add(new ProductGroup { Id = groupId, Name = "Fläktar" });
        dbContext.SoldProducts.Add(new SoldProduct
        {
            Id = Guid.NewGuid(),
            Name = "Fläkt A",
            GroupId = groupId,
            MeasurementQuality = "estimated"
        });
        await dbContext.SaveChangesAsync();
    
        var result = await new ProductGroupsController(dbContext).Delete(groupId);
        var response = result.Should().BeOfType<ObjectResult>().Subject;
        response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        response.Value.Should().BeOfType<ProblemDetails>()
            .Which.Detail.Should().Contain("används av en eller flera produkter");
        dbContext.ProductGroups.Should().ContainSingle();
    }
}
