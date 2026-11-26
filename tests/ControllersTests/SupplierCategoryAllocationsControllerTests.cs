using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SeconSbtiBackend.Controllers;
using SeconSbtiBackend.DTOs;
using SeconSbtiBackend.Interfaces;
using Xunit;

namespace tests.CalculationsTests;

public class SupplierCategoryAllocationsControllerTests
{
    private readonly ISupplierCategoryAllocationService _service;
    private readonly SupplierCategoryAllocationsController _controller;

    public SupplierCategoryAllocationsControllerTests()
    {
        _service = Substitute.For<ISupplierCategoryAllocationService>();
        _controller = new SupplierCategoryAllocationsController(_service);
    }

    [Fact]
    public async Task GetSuppliers_ReturnsOk_WhenSuccessful()
    {
        var expected = new List<SupplierResponse>
        {
            new(Guid.NewGuid(), "Supplier A")
        };

        _service.GetSuppliersAsync(null, Arg.Any<CancellationToken>()).Returns(expected);
        var result = await _controller.GetSuppliers(null, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task GetSuppliers_ReturnsBadRequest_WhenCategoryIdIsEmpty()
    {
        var result = await _controller.GetSuppliers(Guid.Empty, CancellationToken.None);

        result.Result.Should().BeAssignableTo<ObjectResult>()
            .Which.StatusCode.Should().Be(400);

        await _service.DidNotReceive().GetSuppliersAsync(
            Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSuppliers_PassesCategoryIdToService()
    {
        var categoryId = Guid.NewGuid();

        _service.GetSuppliersAsync(categoryId, Arg.Any<CancellationToken>())
            .Returns(new List<SupplierResponse>());

        await _controller.GetSuppliers(categoryId, CancellationToken.None);
        await _service.Received(1).GetSuppliersAsync(categoryId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCategories_ReturnsOk()
    {
        var expected = new List<PurchaseCategoryResponse>
        {
            new(Guid.NewGuid(), "Category A")
        };

        _service.GetCategoriesAsync(Arg.Any<CancellationToken>()).Returns(expected);
        var result = await _controller.GetCategories(CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task GetAllocations_ReturnsOk_WhenSuccessful()
    {
        var supplierId = Guid.NewGuid();

        var allocations = new List<SupplierCategoryAllocationResponse>
        {
            new(Guid.NewGuid(), "Category A", 1m)
        };

        _service.GetAsync(supplierId, Arg.Any<CancellationToken>())
            .Returns(SupplierCategoryAllocationResult.Success(allocations));

        var result = await _controller.GetAllocations(supplierId, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeSameAs(allocations);
    }

    [Fact]
    public async Task GetAllocations_ReturnsNotFound_WhenSupplierDoesNotExist()
    {
        var supplierId = Guid.NewGuid();

        _service.GetAsync(supplierId, Arg.Any<CancellationToken>())
            .Returns(SupplierCategoryAllocationResult.NotFound());

        var result = await _controller.GetAllocations(supplierId, CancellationToken.None);
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateAllocations_ReturnsOk_WhenSuccessful()
    {
        var supplierId = Guid.NewGuid();

        var request = new UpdateSupplierCategoryAllocationsRequest
        {
            Allocations =
            [
                new SupplierCategoryAllocationRequest
                {
                    PurchaseCategoryId = Guid.NewGuid(),
                    Share = 1m
                }
            ]
        };

        var expected = new List<SupplierCategoryAllocationResponse>
        {
            new(Guid.NewGuid(), "Category A", 1m)
        };

        _service.UpdateAsync(supplierId, request, Arg.Any<CancellationToken>())
            .Returns(SupplierCategoryAllocationResult.Success(expected));

        var result = await _controller.UpdateAllocations(
            supplierId, request, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task UpdateAllocations_ReturnsBadRequest_WhenValidationFails()
    {
        var supplierId = Guid.NewGuid();
        var request = new UpdateSupplierCategoryAllocationsRequest();

        _service.UpdateAsync(supplierId, request, Arg.Any<CancellationToken>())
            .Returns(SupplierCategoryAllocationResult.ValidationError(
                "allocations", "Fördelningen måste vara 100 procent."));

        var result = await _controller.UpdateAllocations(supplierId, request, CancellationToken.None);

        result.Result.Should().BeAssignableTo<ObjectResult>()
            .Which.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task UpdateAllocations_ReturnsNotFound_WhenSupplierDoesNotExist()
    {
        var supplierId = Guid.NewGuid();
        var request = new UpdateSupplierCategoryAllocationsRequest();

        _service.UpdateAsync(supplierId, request, Arg.Any<CancellationToken>())
            .Returns(SupplierCategoryAllocationResult.NotFound());

        var result = await _controller.UpdateAllocations(supplierId, request, CancellationToken.None);
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateAllocations_PassesRequestToService()
    {
        var supplierId = Guid.NewGuid();
        var request = new UpdateSupplierCategoryAllocationsRequest();

        _service.UpdateAsync(supplierId, request, Arg.Any<CancellationToken>())
            .Returns(SupplierCategoryAllocationResult.Success(
                new List<SupplierCategoryAllocationResponse>()));

        await _controller.UpdateAllocations(supplierId, request, CancellationToken.None);
        await _service.Received(1).UpdateAsync(supplierId, request, Arg.Any<CancellationToken>());
    }
}
