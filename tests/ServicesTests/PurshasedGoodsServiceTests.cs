using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SeconSbtiBackend.Data;
using SeconSbtiBackend.DTOs;
using SeconSbtiBackend.Models;
using SeconSbtiBackend.Services;

namespace tests.ServicesTests;

/// <summary>
/// Unit tests for PurchasedGoodsService with the InMemory provider.
/// The controller-level behaviour (HTTP status codes, authentication) is covered in PurchasedGoodsControllerTests.
/// </summary>
public class PurchasedGoodsServiceTests
{
    private static DbContextOptions<ApplicationDbContext> NewOptions() =>
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

    private static ApplicationDbContext Db() => new(NewOptions());
    private static PurchasedGoodsService CreateSut(ApplicationDbContext db) => new(db);

    /****************Seeding helpers*******************/
    private static async Task<Guid> SeedUserAsync(ApplicationDbContext db)
    {
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            FirstName = "Anna",
            LastName = "Testsson",
            Email = $"{Guid.NewGuid():N}@secon.se"
        };
        db.AppUsers.Add(user);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        return user.Id;
    }

    private static async Task<Guid> SeedSupplierAsync(ApplicationDbContext db, string name = "Virken AB")
    {
        var supplier = new Supplier { Id = Guid.NewGuid(), Name = $"{name} {Guid.NewGuid():N}" };
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

    private static async Task<Guid> SeedGoodAsync(
        ApplicationDbContext db,
        Guid supplierId,
        Guid createdBy,
        DateOnly? date = null,
        string unit = "sek",
        decimal value = 100m,
        string measurementQuality = "measured",
        string? goodsName = "Original vara",
        string? comment = "Original kommentar",
        string? specification = "Original specifikation",
        string? accountNo = "4010")
    {
        var good = new PurchasedGood
        {
            Id = Guid.NewGuid(),
            SupplierId = supplierId,
            TransactionDate = date ?? new DateOnly(2025, 6, 1),
            Value = value,
            Unit = unit,
            MeasurementQuality = measurementQuality,
            GoodsName = goodsName,
            Comment = comment,
            Specification = specification,
            AccountNo = accountNo,
            CreatedBy = createdBy,
            CreatedAt = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc)
        };
        db.PurchasedGoods.Add(good);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        return good.Id;
    }

    private static CreatePurchasedGoodRequest CreateRequest(
        Guid supplierId,
        string unit = "sek",
        decimal value = 100m,
        string measurementQuality = "estimated",
        DateOnly? date = null) => new()
    {
        SupplierId = supplierId,
        TransactionDate = date ?? new DateOnly(2025, 6, 1),
        Value = value,
        Unit = unit,
        MeasurementQuality = measurementQuality,
        GoodsName = "Plywood",
        Comment = "Ny kommentar",
        Specification = "18 mm",
        AccountNo = "4010"
    };

    private static UpdatePurchasedGoodRequest UpdateRequest(
        Guid supplierId,
        string unit = "sek",
        decimal value = 250m,
        string measurementQuality = "estimated",
        string? goodsName = null,
        string? comment = null,
        string? specification = null,
        string? accountNo = null) => new()
    {
        SupplierId = supplierId,
        TransactionDate = new DateOnly(2025, 7, 1),
        Value = value,
        Unit = unit,
        MeasurementQuality = measurementQuality,
        GoodsName = goodsName,
        Comment = comment,
        Specification = specification,
        AccountNo = accountNo
    };

    private static async Task<PurchasedGood> ReadGoodAsync(ApplicationDbContext db, Guid id)
    {
        db.ChangeTracker.Clear();

        return await db.PurchasedGoods.AsNoTracking().SingleAsync(good => good.Id == id);
    }

    private static async Task<int> CountGoodsAsync(ApplicationDbContext db)
    {
        db.ChangeTracker.Clear();

        return await db.PurchasedGoods.CountAsync();
    }

    /****************GetAllAsync: validation*******************/

    [Theory]
    [InlineData(null, 2, "year")]
    [InlineData(0, null, "year")]
    [InlineData(-1, null, "year")]
    [InlineData(10000, null, "year")]
    [InlineData(2025, 0, "month")]
    [InlineData(2025, 13, "month")]
    public async Task GetAllAsync_InvalidPeriod_ReturnsValidationErrorOnTheRightField(int? year, int? month, string field)
    {
        await using var db = Db();

        var result = await CreateSut(db).GetAllAsync(year, month, CancellationToken.None);

        result.Status.Should().Be(PurchasedGoodsResultStatus.ValidationError);
        result.Field.Should().Be(field);
        result.Message.Should().NotBeNullOrWhiteSpace();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_MonthWithoutYear_ReturnsSwedishMessage()
    {
        await using var db = Db();

        var result = await CreateSut(db).GetAllAsync(null, 3, CancellationToken.None);

        result.Message.Should().Be("Ange ett år när du filtrerar på månad.");
    }

    /****************GetAllAsync: filtering and ordering*******************/

    [Fact]
    public async Task GetAllAsync_NoGoods_ReturnsEmptyList()
    {
        await using var db = Db();

        var result = await CreateSut(db).GetAllAsync(null, null, CancellationToken.None);

        result.Status.Should().Be(PurchasedGoodsResultStatus.Success);
        result.Value.Should().BeEmpty();
    }

    private static async Task SeedGoodsAcrossPeriodsAsync(ApplicationDbContext db)
    {
        var user = await SeedUserAsync(db);
        var supplier = await SeedSupplierAsync(db);

        foreach (var date in new[]
        {
            new DateOnly(2024, 1, 31),
            new DateOnly(2024, 2, 1),
            new DateOnly(2024, 2, 29),
            new DateOnly(2024, 3, 1),
            new DateOnly(2024, 12, 31),
            new DateOnly(2025, 2, 15)
        })
        {
            await SeedGoodAsync(db, supplier, user, date);
        }
    }

    [Fact]
    public async Task GetAllAsync_WithoutFilter_ReturnsAllGoodsNewestFirst()
    {
        await using var db = Db();
        await SeedGoodsAcrossPeriodsAsync(db);

        var result = await CreateSut(db).GetAllAsync(null, null, CancellationToken.None);

        result.Value!.Select(good => good.TransactionDate).Should().BeInDescendingOrder();
        result.Value.Should().HaveCount(6);
    }

    [Fact]
    public async Task GetAllAsync_YearFilter_ReturnsOnlyThatYearIncludingFirstAndLastDay()
    {
        await using var db = Db();
        await SeedGoodsAcrossPeriodsAsync(db);

        var result = await CreateSut(db).GetAllAsync(2024, null, CancellationToken.None);

        result.Value!.Select(good => good.TransactionDate).Should().Equal(
            new DateOnly(2024, 12, 31),
            new DateOnly(2024, 3, 1),
            new DateOnly(2024, 2, 29),
            new DateOnly(2024, 2, 1),
            new DateOnly(2024, 1, 31));
    }

    [Fact]
    public async Task GetAllAsync_MonthFilterInLeapYear_IncludesLeapDayAndExcludesNeighbouringMonths()
    {
        await using var db = Db();
        await SeedGoodsAcrossPeriodsAsync(db);

        var result = await CreateSut(db).GetAllAsync(2024, 2, CancellationToken.None);

        result.Value!.Select(good => good.TransactionDate).Should().Equal(
            new DateOnly(2024, 2, 29),
            new DateOnly(2024, 2, 1));
    }

    [Fact]
    public async Task GetAllAsync_MonthFilterSameMonthOtherYear_IsNotMixedUp()
    {
        await using var db = Db();
        await SeedGoodsAcrossPeriodsAsync(db);

        var result = await CreateSut(db).GetAllAsync(2025, 2, CancellationToken.None);

        result.Value.Should().ContainSingle().Which.TransactionDate.Should().Be(new DateOnly(2025, 2, 15));
    }

    [Fact]
    public async Task GetAllAsync_PeriodWithoutGoods_ReturnsEmptyList()
    {
        await using var db = Db();
        await SeedGoodsAcrossPeriodsAsync(db);

        var result = await CreateSut(db).GetAllAsync(2023, null, CancellationToken.None);

        result.Status.Should().Be(PurchasedGoodsResultStatus.Success);
        result.Value.Should().BeEmpty();
    }

    /****************GetAllAsync: response content*******************/

    [Fact]
    public async Task GetAllAsync_MapsAllFieldsOfTheGood()
    {
        await using var db = Db();
        var user = await SeedUserAsync(db);
        var supplier = await SeedSupplierAsync(db);
        var id = await SeedGoodAsync(db, supplier, user, new DateOnly(2025, 6, 1), "st", 12m, "estimated",
            "Plywood", "En kommentar", "18 mm", "4010");

        var result = await CreateSut(db).GetAllAsync(null, null, CancellationToken.None);

        var good = result.Value.Should().ContainSingle().Subject;
        good.Id.Should().Be(id);
        good.SupplierId.Should().Be(supplier);
        good.TransactionDate.Should().Be(new DateOnly(2025, 6, 1));
        good.Value.Should().Be(12m);
        good.Unit.Should().Be("st");
        good.MeasurementQuality.Should().Be("estimated");
        good.GoodsName.Should().Be("Plywood");
        good.Comment.Should().Be("En kommentar");
        good.Specification.Should().Be("18 mm");
        good.AccountNo.Should().Be("4010");
        good.CreatedBy.Should().Be(user);
    }

    [Fact]
    public async Task GetAllAsync_IncludesSupplierCategoriesSortedByNameWithShare()
    {
        await using var db = Db();
        var user = await SeedUserAsync(db);
        var supplier = await SeedSupplierAsync(db);
        var wood = await SeedCategoryAsync(db, "Trä");
        var electronics = await SeedCategoryAsync(db, "Elektronik");
        await SeedAllocationAsync(db, supplier, wood, 0.7m);
        await SeedAllocationAsync(db, supplier, electronics, 0.3m);
        await SeedGoodAsync(db, supplier, user);

        var result = await CreateSut(db).GetAllAsync(null, null, CancellationToken.None);

        var categories = result.Value.Should().ContainSingle().Subject.Categories;
        categories.Select(category => category.Name).Should().Equal("Elektronik", "Trä");
        categories.Select(category => category.Share).Should().Equal(0.3m, 0.7m);
        categories.Select(category => category.PurchaseCategoryId).Should().Equal(electronics, wood);
    }

    [Fact]
    public async Task GetAllAsync_SupplierWithoutAllocations_GivesEmptyCategories()
    {
        await using var db = Db();
        var user = await SeedUserAsync(db);
        var supplier = await SeedSupplierAsync(db);
        await SeedGoodAsync(db, supplier, user);

        var result = await CreateSut(db).GetAllAsync(null, null, CancellationToken.None);

        result.Value.Should().ContainSingle().Which.Categories.Should().BeEmpty();
    }

    /****************CreateAsync*******************/
    [Fact]
    public async Task CreateAsync_ValidRequest_SavesGoodAndReturnsIt()
    {
        await using var db = Db();
        var user = await SeedUserAsync(db);
        var supplier = await SeedSupplierAsync(db);

        var before = DateTime.UtcNow;
        var result = await CreateSut(db).CreateAsync(CreateRequest(supplier, value: 150m), user, CancellationToken.None);

        result.Status.Should().Be(PurchasedGoodsResultStatus.Success);
        var response = result.Value!;
        response.Id.Should().NotBeEmpty();
        response.SupplierId.Should().Be(supplier);
        response.Value.Should().Be(150m);
        response.Unit.Should().Be("sek");
        response.MeasurementQuality.Should().Be("estimated");
        response.GoodsName.Should().Be("Plywood");
        response.Comment.Should().Be("Ny kommentar");
        response.Specification.Should().Be("18 mm");
        response.AccountNo.Should().Be("4010");
        response.CreatedBy.Should().Be(user);
        response.CreatedAt.Should().BeOnOrAfter(before);

        var saved = await ReadGoodAsync(db, response.Id);
        saved.ImportBatchId.Should().BeNull("manually created goods have no import batch");
        saved.SourceRecordId.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_IncludesSupplierCategoriesInResponse()
    {
        await using var db = Db();
        var user = await SeedUserAsync(db);
        var supplier = await SeedSupplierAsync(db);
        var wood = await SeedCategoryAsync(db, "Trä");
        await SeedAllocationAsync(db, supplier, wood, 1m);

        var result = await CreateSut(db).CreateAsync(CreateRequest(supplier), user, CancellationToken.None);

        result.Value!.Categories.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new PurchasedGoodCategoryResponse
            {
                PurchaseCategoryId = wood,
                Name = "Trä",
                Share = 1m
            });
    }

    [Theory]
    [InlineData("sek", 0)]
    [InlineData("sek", -500)]
    [InlineData("st", 0)]
    [InlineData("st", 25)]
    public async Task CreateAsync_AcceptsZeroAndCreditsInSekAndNonNegativeQuantityInPieces(string unit, int value)
    {
        await using var db = Db();
        var user = await SeedUserAsync(db);
        var supplier = await SeedSupplierAsync(db);

        var result = await CreateSut(db).CreateAsync(
            CreateRequest(supplier, unit, value), user, CancellationToken.None);

        result.Status.Should().Be(PurchasedGoodsResultStatus.Success);
        result.Value!.Value.Should().Be(value);
        result.Value.Unit.Should().Be(unit);
    }

    [Fact]
    public async Task CreateAsync_UnknownUser_ReturnsInvalidUserAndSavesNothing()
    {
        await using var db = Db();
        var supplier = await SeedSupplierAsync(db);

        var result = await CreateSut(db).CreateAsync(CreateRequest(supplier), Guid.NewGuid(), CancellationToken.None);

        result.Status.Should().Be(PurchasedGoodsResultStatus.InvalidUser);
        result.Message.Should().Be("Användarkontot finns inte längre. Kontakta en administratör.");
        (await CountGoodsAsync(db)).Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_UnknownSupplier_ReturnsValidationErrorOnSupplierIdAndSavesNothing()
    {
        await using var db = Db();
        var user = await SeedUserAsync(db);

        var result = await CreateSut(db).CreateAsync(CreateRequest(Guid.NewGuid()), user, CancellationToken.None);

        result.Status.Should().Be(PurchasedGoodsResultStatus.ValidationError);
        result.Field.Should().Be("supplierId");
        (await CountGoodsAsync(db)).Should().Be(0);
    }

    [Theory]
    [InlineData("kg")]
    [InlineData("SEK")]
    [InlineData("")]
    public async Task CreateAsync_UnsupportedUnit_ReturnsValidationErrorOnUnitAndSavesNothing(string unit)
    {
        await using var db = Db();
        var user = await SeedUserAsync(db);
        var supplier = await SeedSupplierAsync(db);

        var result = await CreateSut(db).CreateAsync(CreateRequest(supplier, unit), user, CancellationToken.None);

        result.Status.Should().Be(PurchasedGoodsResultStatus.ValidationError);
        result.Field.Should().Be("unit");
        result.Message.Should().Be("Välj SEK eller styck.");
        (await CountGoodsAsync(db)).Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_NegativeQuantityInPieces_ReturnsValidationErrorOnValue()
    {
        await using var db = Db();
        var user = await SeedUserAsync(db);
        var supplier = await SeedSupplierAsync(db);

        var result = await CreateSut(db).CreateAsync(CreateRequest(supplier, "st", -1m), user, CancellationToken.None);

        result.Status.Should().Be(PurchasedGoodsResultStatus.ValidationError);
        result.Field.Should().Be("value");
        result.Message.Should().Be("Antal får inte vara negativt.");
        (await CountGoodsAsync(db)).Should().Be(0);
    }

    [Theory]
    [InlineData("guessed")]
    [InlineData("")]
    [InlineData("Measured")]
    public async Task CreateAsync_InvalidMeasurementQuality_ReturnsValidationErrorAndSavesNothing(string quality)
    {
        await using var db = Db();
        var user = await SeedUserAsync(db);
        var supplier = await SeedSupplierAsync(db);

        var result = await CreateSut(db).CreateAsync(
            CreateRequest(supplier, measurementQuality: quality), user, CancellationToken.None);

        result.Status.Should().Be(PurchasedGoodsResultStatus.ValidationError);
        result.Field.Should().Be("measurementQuality");
        result.Message.Should().Be("Välj uppmätt eller uppskattat.");
        (await CountGoodsAsync(db)).Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_UnknownUserIsCheckedBeforeSupplierAndUnit()
    {
        await using var db = Db();

        var result = await CreateSut(db).CreateAsync(
            CreateRequest(Guid.NewGuid(), "kg"), Guid.NewGuid(), CancellationToken.None);

        result.Status.Should().Be(PurchasedGoodsResultStatus.InvalidUser);
    }

    /****************UpdateAsync*******************/
    [Fact]
    public async Task UpdateAsync_UnknownGood_ReturnsNotFoundWithMessage()
    {
        await using var db = Db();
        var supplier = await SeedSupplierAsync(db);

        var result = await CreateSut(db).UpdateAsync(Guid.NewGuid(), UpdateRequest(supplier), CancellationToken.None);

        result.Status.Should().Be(PurchasedGoodsResultStatus.NotFound);
        result.Message.Should().Be("Den köpta varan kunde inte hittas.");
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ValidRequest_SavesNewValuesAndReturnsThem()
    {
        await using var db = Db();
        var user = await SeedUserAsync(db);
        var supplier = await SeedSupplierAsync(db);
        var newSupplier = await SeedSupplierAsync(db, "Annan AB");
        var id = await SeedGoodAsync(db, supplier, user);

        var result = await CreateSut(db).UpdateAsync(
            id,
            UpdateRequest(newSupplier, "st", 12m, "measured", "Ny vara", "Ny kommentar", "Ny spec", "4020"),
            CancellationToken.None);

        result.Status.Should().Be(PurchasedGoodsResultStatus.Success);
        result.Value!.Id.Should().Be(id);
        result.Value.SupplierId.Should().Be(newSupplier);

        var saved = await ReadGoodAsync(db, id);
        saved.SupplierId.Should().Be(newSupplier);
        saved.TransactionDate.Should().Be(new DateOnly(2025, 7, 1));
        saved.Value.Should().Be(12m);
        saved.Unit.Should().Be("st");
        saved.MeasurementQuality.Should().Be("measured");
        saved.GoodsName.Should().Be("Ny vara");
        saved.Comment.Should().Be("Ny kommentar");
        saved.Specification.Should().Be("Ny spec");
        saved.AccountNo.Should().Be("4020");
    }

    [Fact]
    public async Task UpdateAsync_DoesNotChangeCreatedByCreatedAtOrImportLink()
    {
        await using var db = Db();
        var user = await SeedUserAsync(db);
        var supplier = await SeedSupplierAsync(db);
        var id = await SeedGoodAsync(db, supplier, user);

        await CreateSut(db).UpdateAsync(id, UpdateRequest(supplier), CancellationToken.None);

        var saved = await ReadGoodAsync(db, id);
        saved.CreatedBy.Should().Be(user);
        saved.CreatedAt.Should().Be(new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc));
        saved.ImportBatchId.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_OmittedOptionalFields_KeepTheirOldValues()
    {
        await using var db = Db();
        var user = await SeedUserAsync(db);
        var supplier = await SeedSupplierAsync(db);
        var id = await SeedGoodAsync(db, supplier, user);

        var result = await CreateSut(db).UpdateAsync(id, UpdateRequest(supplier), CancellationToken.None);

        result.Value!.GoodsName.Should().Be("Original vara");
        result.Value.Comment.Should().Be("Original kommentar");
        result.Value.Specification.Should().Be("Original specifikation");
        result.Value.AccountNo.Should().Be("4010");
    }

    [Fact]
    public async Task UpdateAsync_EmptyStringInOptionalField_ReplacesTheOldValue()
    {
        await using var db = Db();
        var user = await SeedUserAsync(db);
        var supplier = await SeedSupplierAsync(db);
        var id = await SeedGoodAsync(db, supplier, user);

        await CreateSut(db).UpdateAsync(id, UpdateRequest(supplier, comment: ""), CancellationToken.None);
        (await ReadGoodAsync(db, id)).Comment.Should().Be("");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task UpdateAsync_AcceptsZeroAndCreditsInSek(int value)
    {
        await using var db = Db();
        var user = await SeedUserAsync(db);
        var supplier = await SeedSupplierAsync(db);
        var id = await SeedGoodAsync(db, supplier, user);

        var result = await CreateSut(db).UpdateAsync(id, UpdateRequest(supplier, value: value), CancellationToken.None);
        result.Status.Should().Be(PurchasedGoodsResultStatus.Success);
        (await ReadGoodAsync(db, id)).Value.Should().Be(value);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsSupplierCategoriesOfTheNewSupplier()
    {
        await using var db = Db();
        var user = await SeedUserAsync(db);
        var supplier = await SeedSupplierAsync(db);
        var newSupplier = await SeedSupplierAsync(db, "Annan AB");
        var wood = await SeedCategoryAsync(db, "Trä");
        var electronics = await SeedCategoryAsync(db, "Elektronik");
        await SeedAllocationAsync(db, supplier, wood, 1m);
        await SeedAllocationAsync(db, newSupplier, electronics, 1m);
        var id = await SeedGoodAsync(db, supplier, user);

        var result = await CreateSut(db).UpdateAsync(id, UpdateRequest(newSupplier), CancellationToken.None);
        result.Value!.Categories.Should().ContainSingle().Which.Name.Should().Be("Elektronik");
    }

    [Theory]
    [InlineData("kg")]
    [InlineData("")]
    public async Task UpdateAsync_UnsupportedUnitInRequest_ReturnsValidationErrorAndChangesNothing(string unit)
    {
        await using var db = Db();
        var user = await SeedUserAsync(db);
        var supplier = await SeedSupplierAsync(db);
        var id = await SeedGoodAsync(db, supplier, user);

        var result = await CreateSut(db).UpdateAsync(id, UpdateRequest(supplier, unit), CancellationToken.None);

        result.Status.Should().Be(PurchasedGoodsResultStatus.ValidationError);
        result.Field.Should().Be("unit");
        (await ReadGoodAsync(db, id)).Value.Should().Be(100m);
    }

    [Fact]
    public async Task UpdateAsync_StoredGoodWithUnsupportedUnit_ReturnsValidationErrorOnUnit()
    {
        await using var db = Db();
        var user = await SeedUserAsync(db);
        var supplier = await SeedSupplierAsync(db);
        var id = await SeedGoodAsync(db, supplier, user, unit: "kg");

        var result = await CreateSut(db).UpdateAsync(id, UpdateRequest(supplier, "sek"), CancellationToken.None);

        result.Status.Should().Be(PurchasedGoodsResultStatus.ValidationError);
        result.Field.Should().Be("unit");
        (await ReadGoodAsync(db, id)).Unit.Should().Be("kg");
    }

    [Fact]
    public async Task UpdateAsync_CanChangeUnitBetweenSekAndPieces()
    {
        await using var db = Db();
        var user = await SeedUserAsync(db);
        var supplier = await SeedSupplierAsync(db);
        var id = await SeedGoodAsync(db, supplier, user, unit: "sek");

        var result = await CreateSut(db).UpdateAsync(id, UpdateRequest(supplier, "st", 5m), CancellationToken.None);
        result.Status.Should().Be(PurchasedGoodsResultStatus.Success);
        (await ReadGoodAsync(db, id)).Unit.Should().Be("st");
    }

    [Fact]
    public async Task UpdateAsync_NegativeQuantityInPieces_ReturnsValidationErrorOnValueAndChangesNothing()
    {
        await using var db = Db();
        var user = await SeedUserAsync(db);
        var supplier = await SeedSupplierAsync(db);
        var id = await SeedGoodAsync(db, supplier, user);

        var result = await CreateSut(db).UpdateAsync(id, UpdateRequest(supplier, "st", -1m), CancellationToken.None);

        result.Status.Should().Be(PurchasedGoodsResultStatus.ValidationError);
        result.Field.Should().Be("value");
        var saved = await ReadGoodAsync(db, id);
        saved.Value.Should().Be(100m);
        saved.Unit.Should().Be("sek");
    }

    [Fact]
    public async Task UpdateAsync_InvalidMeasurementQuality_ReturnsValidationErrorAndChangesNothing()
    {
        await using var db = Db();
        var user = await SeedUserAsync(db);
        var supplier = await SeedSupplierAsync(db);
        var id = await SeedGoodAsync(db, supplier, user);

        var result = await CreateSut(db).UpdateAsync(id, UpdateRequest(supplier, measurementQuality: "guessed"), CancellationToken.None);

        result.Status.Should().Be(PurchasedGoodsResultStatus.ValidationError);
        result.Field.Should().Be("measurementQuality");
        (await ReadGoodAsync(db, id)).MeasurementQuality.Should().Be("measured");
    }

    [Fact]
    public async Task UpdateAsync_UnknownSupplier_ReturnsValidationErrorOnSupplierIdAndChangesNothing()
    {
        await using var db = Db();
        var user = await SeedUserAsync(db);
        var supplier = await SeedSupplierAsync(db);
        var id = await SeedGoodAsync(db, supplier, user);

        var result = await CreateSut(db).UpdateAsync(id, UpdateRequest(Guid.NewGuid()), CancellationToken.None);

        result.Status.Should().Be(PurchasedGoodsResultStatus.ValidationError);
        result.Field.Should().Be("supplierId");
        var saved = await ReadGoodAsync(db, id);
        saved.SupplierId.Should().Be(supplier);
        saved.Value.Should().Be(100m);
    }

    [Fact]
    public async Task UpdateAsync_NotFoundIsCheckedBeforeValidation()
    {
        await using var db = Db();
        var result = await CreateSut(db).UpdateAsync(
            Guid.NewGuid(), UpdateRequest(Guid.NewGuid(), "kg", measurementQuality: "guessed"), CancellationToken.None);

        result.Status.Should().Be(PurchasedGoodsResultStatus.NotFound);
    }
}