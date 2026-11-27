using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SeconSbtiBackend.Data;
using SeconSbtiBackend.DTOs;
using SeconSbtiBackend.Models;
using SeconSbtiBackend.Services;

namespace tests.ServicesTests;

/// <summary>
/// Unit tests with the InMemory provider.
/// Not covered here: the paths of UpdateAsync that pass validation (success, unknown category, supplier not found).
/// They use BeginTransactionAsync and "SELECT ... FOR UPDATE", which the InMemory provider does not support.
/// Those paths are covered in tests.IntegrationTests.PostgresTests.SupplierCategoryAllocationServiceTests.
/// </summary>
public class SupplierCategoryAllocationServiceTests
{
    private static DbContextOptions<ApplicationDbContext> NewOptions() =>
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

    private static ApplicationDbContext Db() => new(NewOptions());
    private static SupplierCategoryAllocationService CreateSut(ApplicationDbContext db) => new(db);

    /****************Seeding helpers*******************/
    private static async Task<Guid> SeedSupplierAsync(ApplicationDbContext db, string name)
    {
        var supplier = new Supplier { Id = Guid.NewGuid(), Name = name };
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        return supplier.Id;
    }

    private static async Task<Guid> SeedCategoryAsync(ApplicationDbContext db, string name)
    {
        var category = new PurchaseCategory { Id = Guid.NewGuid(), Name = name };
        db.PurchaseCategories.Add(category);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        return category.Id;
    }

    private static async Task SeedAllocationAsync(
        ApplicationDbContext db, Guid supplierId, Guid categoryId, decimal share)
    {
        db.SupplierCategoryAllocations.Add(new SupplierCategoryAllocation
        {
            SupplierId = supplierId,
            PurchaseCategoryId = categoryId,
            Share = share
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
    }

    private static UpdateSupplierCategoryAllocationsRequest Request(
        params (Guid CategoryId, decimal? Share)[] allocations) => new()
    {
        Allocations = allocations
            .Select(allocation => new SupplierCategoryAllocationRequest
            {
                PurchaseCategoryId = allocation.CategoryId,
                Share = allocation.Share
            })
            .ToList()
    };

    private static async Task<List<SupplierCategoryAllocation>> ReadAllocationsAsync(
        ApplicationDbContext db, Guid supplierId)
    {
        db.ChangeTracker.Clear();

        return await db.SupplierCategoryAllocations
            .AsNoTracking()
            .Where(allocation => allocation.SupplierId == supplierId)
            .ToListAsync();
    }

    /****************GetSuppliersAsync*******************/
    [Fact]
    public async Task GetSuppliersAsync_NoSuppliers_ReturnsEmptyList()
    {
        await using var db = Db();
        var result = await CreateSut(db).GetSuppliersAsync(null, CancellationToken.None);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSuppliersAsync_WithoutCategory_ReturnsAllSuppliersSortedByName()
    {
        await using var db = Db();
        await SeedSupplierAsync(db, "Charlie AB");
        await SeedSupplierAsync(db, "Alpha AB");
        await SeedSupplierAsync(db, "Bravo AB");

        var result = await CreateSut(db).GetSuppliersAsync(null, CancellationToken.None);
        result.Select(supplier => supplier.Name).Should().Equal("Alpha AB", "Bravo AB", "Charlie AB");
    }

    [Fact]
    public async Task GetSuppliersAsync_WithoutCategory_IncludesSuppliersWithoutAllocations()
    {
        await using var db = Db();
        var withAllocation = await SeedSupplierAsync(db, "Med fördelning");
        var withoutAllocation = await SeedSupplierAsync(db, "Utan fördelning");
        var category = await SeedCategoryAsync(db, "Trä");
        await SeedAllocationAsync(db, withAllocation, category, 1m);

        var result = await CreateSut(db).GetSuppliersAsync(null, CancellationToken.None);
        result.Select(supplier => supplier.Id).Should().BeEquivalentTo([withAllocation, withoutAllocation]);
    }

    [Fact]
    public async Task GetSuppliersAsync_WithCategory_ReturnsOnlySuppliersInThatCategory()
    {
        await using var db = Db();
        var wood = await SeedCategoryAsync(db, "Trä");
        var electronics = await SeedCategoryAsync(db, "Elektronik");
        var woodSupplier = await SeedSupplierAsync(db, "Virken AB");
        var electronicsSupplier = await SeedSupplierAsync(db, "DataBiz AB");
        await SeedSupplierAsync(db, "Utan kategori AB");
        await SeedAllocationAsync(db, woodSupplier, wood, 1m);
        await SeedAllocationAsync(db, electronicsSupplier, electronics, 1m);

        var result = await CreateSut(db).GetSuppliersAsync(wood, CancellationToken.None);
        var supplier = result.Should().ContainSingle().Subject;
        supplier.Id.Should().Be(woodSupplier);
        supplier.Name.Should().Be("Virken AB");
    }

    [Fact]
    public async Task GetSuppliersAsync_SupplierSplitOverTwoCategories_IsReturnedForBoth()
    {
        await using var db = Db();
        var wood = await SeedCategoryAsync(db, "Trä");
        var electronics = await SeedCategoryAsync(db, "Elektronik");
        var supplier = await SeedSupplierAsync(db, "Delad AB");
        await SeedAllocationAsync(db, supplier, wood, 0.6m);
        await SeedAllocationAsync(db, supplier, electronics, 0.4m);
        var sut = CreateSut(db);

        var inWood = await sut.GetSuppliersAsync(wood, CancellationToken.None);
        var inElectronics = await sut.GetSuppliersAsync(electronics, CancellationToken.None);

        inWood.Should().ContainSingle().Which.Id.Should().Be(supplier);
        inElectronics.Should().ContainSingle().Which.Id.Should().Be(supplier);
    }

    [Fact]
    public async Task GetSuppliersAsync_WithCategory_ReturnsSuppliersSortedByName()
    {
        await using var db = Db();
        var category = await SeedCategoryAsync(db, "Trä");
        var zeta = await SeedSupplierAsync(db, "Zeta AB");
        var alpha = await SeedSupplierAsync(db, "Alpha AB");
        await SeedAllocationAsync(db, zeta, category, 1m);
        await SeedAllocationAsync(db, alpha, category, 1m);

        var result = await CreateSut(db).GetSuppliersAsync(category, CancellationToken.None);
        result.Select(supplier => supplier.Name).Should().Equal("Alpha AB", "Zeta AB");
    }

    [Fact]
    public async Task GetSuppliersAsync_CategoryWithoutSuppliers_ReturnsEmptyList()
    {
        await using var db = Db();
        var category = await SeedCategoryAsync(db, "Tom kategori");
        await SeedSupplierAsync(db, "Någon AB");

        var result = await CreateSut(db).GetSuppliersAsync(category, CancellationToken.None);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSuppliersAsync_UnknownCategory_ReturnsEmptyList()
    {
        await using var db = Db();
        var category = await SeedCategoryAsync(db, "Trä");
        var supplier = await SeedSupplierAsync(db, "Virken AB");
        await SeedAllocationAsync(db, supplier, category, 1m);

        var result = await CreateSut(db).GetSuppliersAsync(Guid.NewGuid(), CancellationToken.None);
        result.Should().BeEmpty();
    }

    /****************GetCategoriesAsync*******************/
    [Fact]
    public async Task GetCategoriesAsync_NoCategories_ReturnsEmptyList()
    {
        await using var db = Db();
        var result = await CreateSut(db).GetCategoriesAsync(CancellationToken.None);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCategoriesAsync_ReturnsCategoriesSortedByName()
    {
        await using var db = Db();
        await SeedCategoryAsync(db, "Trä");
        await SeedCategoryAsync(db, "Elektronik");
        await SeedCategoryAsync(db, "Mat och dryck");

        var result = await CreateSut(db).GetCategoriesAsync(CancellationToken.None);
        result.Select(category => category.Name).Should().Equal("Elektronik", "Mat och dryck", "Trä");
    }

    [Fact]
    public async Task GetCategoriesAsync_ReturnsIdAndName()
    {
        await using var db = Db();
        var id = await SeedCategoryAsync(db, "Trä");
        var result = await CreateSut(db).GetCategoriesAsync(CancellationToken.None);
        result.Should().ContainSingle().Which.Should().Be(new PurchaseCategoryResponse(id, "Trä"));
    }

    /*************************GetAsync****************************/
    [Fact]
    public async Task GetAsync_UnknownSupplier_ReturnsNotFoundWithMessage()
    {
        await using var db = Db();
        await SeedSupplierAsync(db, "Virken AB");

        var result = await CreateSut(db).GetAsync(Guid.NewGuid(), CancellationToken.None);
        result.Status.Should().Be(SupplierCategoryAllocationStatus.NotFound);
        result.Message.Should().Be("Leverantören finns inte.");
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_SupplierWithoutAllocations_ReturnsSuccessWithEmptyList()
    {
        await using var db = Db();
        var supplier = await SeedSupplierAsync(db, "Virken AB");

        var result = await CreateSut(db).GetAsync(supplier, CancellationToken.None);
        result.Status.Should().Be(SupplierCategoryAllocationStatus.Success);
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAsync_ReturnsCategoryIdNameAndShare()
    {
        await using var db = Db();
        var supplier = await SeedSupplierAsync(db, "Virken AB");
        var category = await SeedCategoryAsync(db, "Trä");
        await SeedAllocationAsync(db, supplier, category, 0.75m);

        var result = await CreateSut(db).GetAsync(supplier, CancellationToken.None);
        result.Status.Should().Be(SupplierCategoryAllocationStatus.Success);
        result.Value.Should().ContainSingle()
            .Which.Should().Be(new SupplierCategoryAllocationResponse(category, "Trä", 0.75m));
    }

    [Fact]
    public async Task GetAsync_ReturnsAllocationsSortedByCategoryName()
    {
        await using var db = Db();
        var supplier = await SeedSupplierAsync(db, "Delad AB");
        var wood = await SeedCategoryAsync(db, "Trä");
        var electronics = await SeedCategoryAsync(db, "Elektronik");
        var food = await SeedCategoryAsync(db, "Mat och dryck");
        await SeedAllocationAsync(db, supplier, wood, 0.5m);
        await SeedAllocationAsync(db, supplier, electronics, 0.3m);
        await SeedAllocationAsync(db, supplier, food, 0.2m);

        var result = await CreateSut(db).GetAsync(supplier, CancellationToken.None);
        result.Value!.Select(allocation => allocation.Name).Should().Equal("Elektronik", "Mat och dryck", "Trä");
        result.Value!.Select(allocation => allocation.Share).Should().Equal(0.3m, 0.2m, 0.5m);
    }

    [Fact]
    public async Task GetAsync_ReturnsOnlyTheRequestedSuppliersAllocations()
    {
        await using var db = Db();
        var supplier = await SeedSupplierAsync(db, "Virken AB");
        var other = await SeedSupplierAsync(db, "Annan AB");
        var wood = await SeedCategoryAsync(db, "Trä");
        var electronics = await SeedCategoryAsync(db, "Elektronik");
        await SeedAllocationAsync(db, supplier, wood, 1m);
        await SeedAllocationAsync(db, other, electronics, 1m);

        var result = await CreateSut(db).GetAsync(supplier, CancellationToken.None);
        result.Value.Should().ContainSingle().Which.PurchaseCategoryId.Should().Be(wood);
    }

    /****************UpdateAsync: validation (returns before the transaction)*******************/
    [Fact]
    public async Task UpdateAsync_NullAllocations_ReturnsValidationErrorOnAllocations()
    {
        await using var db = Db();

        var result = await CreateSut(db).UpdateAsync(
            Guid.NewGuid(),
            new UpdateSupplierCategoryAllocationsRequest { Allocations = null },
            CancellationToken.None);

        result.Status.Should().Be(SupplierCategoryAllocationStatus.ValidationError);
        result.Errors.Should().ContainKey("allocations");
        result.Errors!["allocations"].Should().Equal("Ange minst en kategori och fördela totalt 100 procent.");
    }

    [Fact]
    public async Task UpdateAsync_EmptyAllocations_ReturnsValidationErrorOnAllocations()
    {
        await using var db = Db();
        var result = await CreateSut(db).UpdateAsync(Guid.NewGuid(), Request(), CancellationToken.None);
        result.Status.Should().Be(SupplierCategoryAllocationStatus.ValidationError);
        result.Errors.Should().ContainKey("allocations");
    }

    [Fact]
    public async Task UpdateAsync_NullAllocationItem_ReturnsValidationErrorOnThatIndex()
    {
        await using var db = Db();
        var request = new UpdateSupplierCategoryAllocationsRequest
        {
            Allocations = new List<SupplierCategoryAllocationRequest> { null! }
        };

        var result = await CreateSut(db).UpdateAsync(Guid.NewGuid(), request, CancellationToken.None);
        result.Status.Should().Be(SupplierCategoryAllocationStatus.ValidationError);
        result.Errors.Should().ContainKey("allocations[0]");
        result.Errors!["allocations[0]"].Should().Equal("Ange en kategori och en andel.");
    }

    [Fact]
    public async Task UpdateAsync_EmptyCategoryId_ReturnsValidationErrorOnThatField()
    {
        await using var db = Db();

        var result = await CreateSut(db).UpdateAsync(
            Guid.NewGuid(), Request((Guid.Empty, 1m)), CancellationToken.None);

        result.Status.Should().Be(SupplierCategoryAllocationStatus.ValidationError);
        result.Errors.Should().ContainKey("allocations[0].purchaseCategoryId");
        result.Errors!["allocations[0].purchaseCategoryId"].Should().Equal("Välj en kategori.");
    }

    [Fact]
    public async Task UpdateAsync_SameCategoryTwice_ReturnsValidationErrorOnTheSecondOne()
    {
        await using var db = Db();
        var category = Guid.NewGuid();

        var result = await CreateSut(db).UpdateAsync(
            Guid.NewGuid(), Request((category, 0.5m), (category, 0.5m)), CancellationToken.None);

        result.Status.Should().Be(SupplierCategoryAllocationStatus.ValidationError);
        result.Errors.Should().ContainKey("allocations[1].purchaseCategoryId");
        result.Errors!["allocations[1].purchaseCategoryId"].Should().Equal("Samma kategori får bara anges en gång.");
    }

    [Fact]
    public async Task UpdateAsync_MissingShare_ReturnsValidationErrorOnThatField()
    {
        await using var db = Db();

        var result = await CreateSut(db).UpdateAsync(
            Guid.NewGuid(), Request((Guid.NewGuid(), null)), CancellationToken.None);

        result.Status.Should().Be(SupplierCategoryAllocationStatus.ValidationError);
        result.Errors.Should().ContainKey("allocations[0].share");
        result.Errors!["allocations[0].share"].Should().Equal("Ange en andel.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.1)]
    [InlineData(1.01)]
    [InlineData(50)]
    public async Task UpdateAsync_ShareOutsideZeroExclusiveToOneInclusive_ReturnsValidationError(double share)
    {
        await using var db = Db();

        var result = await CreateSut(db).UpdateAsync(
            Guid.NewGuid(), Request((Guid.NewGuid(), (decimal)share)), CancellationToken.None);

        result.Status.Should().Be(SupplierCategoryAllocationStatus.ValidationError);
        result.Errors.Should().ContainKey("allocations[0].share");
        result.Errors!["allocations[0].share"].Single().Should().Contain("större än 0 och högst 1");
    }

    [Fact]
    public async Task UpdateAsync_ErrorIsReportedOnTheIndexOfTheInvalidItem()
    {
        await using var db = Db();

        var result = await CreateSut(db).UpdateAsync(
            Guid.NewGuid(),
            Request((Guid.NewGuid(), 0.5m), (Guid.NewGuid(), 0.3m), (Guid.NewGuid(), null)),
            CancellationToken.None);

        result.Errors.Should().ContainKey("allocations[2].share");
        result.Errors.Should().NotContainKey("allocations[0].share");
        result.Errors.Should().NotContainKey("allocations[1].share");
    }

    [Theory]
    [InlineData(0.6, 0.3)]   // 90 %
    [InlineData(0.5, 0.6)]   // 110 %
    [InlineData(0.1, 0.1)]   // 20 %
    public async Task UpdateAsync_SharesNotSummingToOne_ReturnsValidationErrorOnAllocations(double first, double second)
    {
        await using var db = Db();

        var result = await CreateSut(db).UpdateAsync(
            Guid.NewGuid(),
            Request((Guid.NewGuid(), (decimal)first), (Guid.NewGuid(), (decimal)second)),
            CancellationToken.None);

        result.Status.Should().Be(SupplierCategoryAllocationStatus.ValidationError);
        result.Errors.Should().ContainKey("allocations");
        result.Errors!["allocations"].Should().Equal("Fördelningen måste summera till 100 procent innan den kan sparas.");
    }

    [Fact]
    public async Task UpdateAsync_ThreeSharesSlightlyBelowOne_ReturnsValidationError()
    {
        await using var db = Db();

        var result = await CreateSut(db).UpdateAsync(
            Guid.NewGuid(),
            Request((Guid.NewGuid(), 0.3333m), (Guid.NewGuid(), 0.3333m), (Guid.NewGuid(), 0.3333m)),
            CancellationToken.None);

        result.Status.Should().Be(SupplierCategoryAllocationStatus.ValidationError);
        result.Errors.Should().ContainKey("allocations");
    }

    [Fact]
    public async Task UpdateAsync_InvalidRequest_DoesNotChangeExistingAllocations()
    {
        await using var db = Db();
        var supplier = await SeedSupplierAsync(db, "Virken AB");
        var wood = await SeedCategoryAsync(db, "Trä");
        var electronics = await SeedCategoryAsync(db, "Elektronik");
        await SeedAllocationAsync(db, supplier, wood, 0.6m);
        await SeedAllocationAsync(db, supplier, electronics, 0.4m);

        var result = await CreateSut(db).UpdateAsync(
            supplier, Request((wood, 0.5m), (electronics, 0.3m)), CancellationToken.None);

        result.Status.Should().Be(SupplierCategoryAllocationStatus.ValidationError);

        var saved = await ReadAllocationsAsync(db, supplier);
        saved.Should().HaveCount(2);
        saved.Single(allocation => allocation.PurchaseCategoryId == wood).Share.Should().Be(0.6m);
        saved.Single(allocation => allocation.PurchaseCategoryId == electronics).Share.Should().Be(0.4m);
    }

    [Fact]
    public async Task UpdateAsync_ValidationRunsBeforeSupplierLookup_SoUnknownSupplierWithInvalidRequestGivesValidationError()
    {
        await using var db = Db();
        var result = await CreateSut(db).UpdateAsync(Guid.NewGuid(), Request(), CancellationToken.None);
        result.Status.Should().Be(SupplierCategoryAllocationStatus.ValidationError);
    }
}