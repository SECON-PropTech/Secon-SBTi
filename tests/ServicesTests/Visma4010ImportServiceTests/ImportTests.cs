using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SeconSbtiBackend.Importing.Visma4010;
using SeconSbtiBackend.Models;
using tests.ImportingTests.Visma4010;
using static tests.ServicesTests.Visma4010ImportServiceTests.Visma4010ImportServiceTestHelpers;

namespace tests.ServicesTests.Visma4010ImportServiceTests;

/// <summary>
/// ImportAsync on readable files: what is saved, what is blocked and what is skipped.
/// </summary>
public class ImportTests
{
    [Fact]
    public async Task Import_should_return_invalid_user_when_user_does_not_exist()
    {
        await using var context = TestDatabase.Create();
        var service = CreateService(context);
        await using var content = new MemoryStream([1, 2, 3]);

        var result = await service.ImportAsync(content, "visma.xlsx", Guid.NewGuid(), CancellationToken.None);

        result.Status.Should().Be(Visma4010ImportStatus.InvalidUser);
        result.Message.Should().Be("Användarkontot finns inte längre. Kontakta en administratör.");
        result.Response.Should().BeNull();
    }

    /************************Saved data************************/
    [Fact]
    public async Task Import_should_create_batch_suppliers_and_purchased_goods_when_file_is_valid()
    {
        await using var context = TestDatabase.Create();
        var user = await SeedUserAsync(context);
        var service = CreateService(context);
        await using var file = Visma4010FileBuilder.Standard().ToStream();

        var result = await service.ImportAsync(file, "visma4010.xlsx", user.Id, CancellationToken.None);

        result.Status.Should().Be(Visma4010ImportStatus.Ok);
        result.Response.Should().NotBeNull();
        result.Response!.ImportBatchId.Should().NotBeEmpty();

        var batch = await context.ImportBatches
            .Include(batch => batch.ImportBatchFiles)
            .SingleAsync();

        batch.Id.Should().Be(result.Response.ImportBatchId!.Value);
        batch.Source.Should().Be("visma_4010");
        batch.ImportedBy.Should().Be(user.Id);
        batch.ImportBatchFiles.Should().ContainSingle();

        (await context.Suppliers.ToListAsync()).Should().HaveCount(2);

        var goods = await context.PurchasedGoods.ToListAsync();
        goods.Should().HaveCount(2);
        goods.Should().OnlyContain(good => good.ImportBatchId == batch.Id);
        goods.Should().OnlyContain(good => good.CreatedBy == user.Id);
        goods.Should().OnlyContain(good => good.Unit == "sek" && good.MeasurementQuality == "measured");
        goods.Should().OnlyContain(good => good.AccountNo == "4010");
        goods.Select(good => good.SourceRecordId).Should().BeEquivalentTo(["260034", "260035"]);
        goods.Sum(good => good.Value).Should().Be(4159.60m);
    }

    [Fact]
    public async Task Import_should_reuse_existing_suppliers_after_normalizing_names()
    {
        await using var context = TestDatabase.Create();
        var user = CreateUser();

        context.AppUsers.Add(user);
        // Two existing suppliers that normalize to the same name: the first one wins, no error.
        context.Suppliers.AddRange(
            new Supplier { Id = Guid.NewGuid(), Name = "IMI   Hydronic Engineering AB" },
            new Supplier { Id = Guid.NewGuid(), Name = "IMI Hydronic Engineering AB" });
        await context.SaveChangesAsync();

        var service = CreateService(context);
        await using var file = Visma4010FileBuilder.Standard().ToStream();

        var result = await service.ImportAsync(file, "test.xlsx", user.Id, CancellationToken.None);

        result.Status.Should().Be(Visma4010ImportStatus.Ok);
        result.Response!.NewSuppliers.Should().Equal("Virken AB");

        var suppliers = await context.Suppliers.ToListAsync();
        // 2 existing + Virken AB. No new IMI supplier is created.
        suppliers.Should().HaveCount(3);
        suppliers.Count(supplier => supplier.Name.Contains("IMI")).Should().Be(2);
    }

    [Fact]
    public async Task Import_should_skip_invoice_that_already_exists()
    {
        await using var context = TestDatabase.Create();
        var user = CreateUser();
        var supplier = new Supplier { Id = Guid.NewGuid(), Name = "Existing supplier" };

        context.AppUsers.Add(user);
        context.Suppliers.Add(supplier);
        context.PurchasedGoods.Add(ExistingGood(supplier, user, new DateOnly(2026, 1, 1), 3159.60m, "260034"));
        await context.SaveChangesAsync();

        var service = CreateService(context);
        await using var file = Visma4010FileBuilder.Standard().ToStream();

        var result = await service.ImportAsync(file, "visma4010.xlsx", user.Id, CancellationToken.None);

        result.Status.Should().Be(Visma4010ImportStatus.Ok);
        result.Response!.AlreadyImported.Count.Should().Be(1);
        result.Response.ToImport.Count.Should().Be(1);

        var goods = await context.PurchasedGoods.ToListAsync();
        goods.Should().HaveCount(2);
        goods.Count(good => good.SourceRecordId == "260034").Should().Be(1);
        goods.Count(good => good.SourceRecordId == "260035").Should().Be(1);
    }

    /************************Blocked imports************************/
    [Fact]
    public async Task Import_should_be_blocked_and_save_nothing_when_reconciliation_fails()
    {
        await using var context = TestDatabase.Create();
        var user = await SeedUserAsync(context);
        var service = CreateService(context);

        var builder = Account()
            .Invoice("L1", Jan1, "Levfkt 1 A AB", debit: 100m)
            .Turnover(debit: 1m)
            .Closing();

        await using var file = builder.ToStream();

        var result = await service.ImportAsync(file, "visma4010.xlsx", user.Id, CancellationToken.None);

        result.Status.Should().Be(Visma4010ImportStatus.Blocked);
        result.Response!.Blockers.Should().Contain(Visma4010Blockers.ReconciliationFailed);
        (await context.ImportBatches.CountAsync()).Should().Be(0);
        (await context.PurchasedGoods.CountAsync()).Should().Be(0);
        (await context.Suppliers.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Import_should_be_blocked_when_file_has_already_been_imported()
    {
        await using var context = TestDatabase.Create();
        var user = await SeedUserAsync(context);
        var service = CreateService(context);
        var bytes = Visma4010FileBuilder.Standard().ToBytes();
        await using var firstFile = new MemoryStream(bytes);

        var firstResult = await service.ImportAsync(firstFile, "visma4010.xlsx", user.Id, CancellationToken.None);
        firstResult.Status.Should().Be(Visma4010ImportStatus.Ok);

        await using var secondFile = new MemoryStream(bytes);

        var secondResult = await service.ImportAsync(secondFile, "visma4010.xlsx", user.Id, CancellationToken.None);

        secondResult.Status.Should().Be(Visma4010ImportStatus.Blocked);
        secondResult.Response!.CanImport.Should().BeFalse();
        secondResult.Response.Blockers.Should().Contain(Visma4010Blockers.FileAlreadyImported);
        (await context.ImportBatches.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Import_should_be_blocked_when_all_invoices_already_exist()
    {
        await using var context = TestDatabase.Create();
        var user = CreateUser();
        var supplier = new Supplier { Id = Guid.NewGuid(), Name = "Existing supplier" };

        context.AppUsers.Add(user);
        context.Suppliers.Add(supplier);
        context.PurchasedGoods.AddRange(
            ExistingGood(supplier, user, new DateOnly(2026, 1, 1), 3159.60m, "260034"),
            ExistingGood(supplier, user, new DateOnly(2026, 2, 3), 1000m, "260035"));
        await context.SaveChangesAsync();

        var service = CreateService(context);
        await using var file = Visma4010FileBuilder.Standard().ToStream();

        var result = await service.ImportAsync(file, "visma4010.xlsx", user.Id, CancellationToken.None);

        result.Status.Should().Be(Visma4010ImportStatus.Blocked);
        result.Response!.CanImport.Should().BeFalse();
        result.Response.Blockers.Should().Contain(Visma4010Blockers.NothingToImport);
    }

    /// <summary>
    /// Test helper: a purchased good that was imported earlier, identified by its invoice number.
    /// </summary>
    private static PurchasedGood ExistingGood(Supplier supplier, AppUser user, DateOnly date, decimal value, string invoiceNumber) => new()
    {
        Id = Guid.NewGuid(),
        SupplierId = supplier.Id,
        TransactionDate = date,
        Value = value,
        Unit = "sek",
        MeasurementQuality = "measured",
        SourceRecordId = invoiceNumber,
        CreatedBy = user.Id,
        CreatedAt = DateTime.UtcNow
    };
}