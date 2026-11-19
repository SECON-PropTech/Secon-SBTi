using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SeconSbtiBackend.Data;
using SeconSbtiBackend.Importing.Visma4010;
using tests.ImportingTests.Visma4010;
using SeconSbtiBackend.Models;

namespace tests.ServicesTests;

public class Visma4010ImportServiceTests
{

    //Helpers to create the inMemory Database
    private static Visma4010ImportService CreateService()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().Options;
        var context = new ApplicationDbContext(options);
        return new Visma4010ImportService(context);
    }

    private static Visma4010ImportService CreateService(ApplicationDbContext context)
    {
        return new Visma4010ImportService(context);
    }

/****************HeLPER ??*******************/
    private sealed class ThrowingReadStream : MemoryStream
    {
        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            throw new IOException("Simulated stream failure.");
        }
    }

    /************************Previews************************/
    [Fact]
    public async Task Preview_should_return_ok_when_file_is_valid()
    {
        await using var context = TestDatabase.Create();
        var service = new Visma4010ImportService(context);
        await using var file = Visma4010FileBuilder.Standard().ToStream();

        var result = await service.PreviewAsync(file, "visma4010.xlsx", CancellationToken.None);
        result.Status.Should().Be(Visma4010ImportStatus.Ok);
        result.Response.Should().NotBeNull();
        result.Message.Should().BeNull();
    }

    [Fact]
    public async Task Preview_should_return_invalid_file_when_extension_is_not_supported()
    {
        var service = CreateService();
        await using var content = new MemoryStream([1, 2, 3]);

        var result = await service.PreviewAsync(content, "visma.pdf", CancellationToken.None);
        result.Status.Should().Be(Visma4010ImportStatus.InvalidFile);
        result.Message.Should().Be("Endast .xlsx- och .xls-filer stöds.");
        result.Response.Should().BeNull();
    }

    [Fact]
    public async Task Preview_should_return_invalid_file_when_file_is_empty()
    {
        var service = CreateService();
        await using var content = new MemoryStream();

        var result = await service.PreviewAsync( content, "visma.xlsx", CancellationToken.None);
        result.Status.Should().Be(Visma4010ImportStatus.InvalidFile);
        result.Message.Should().Be("Filen är tom.");
        result.Response.Should().BeNull();
    }

    [Fact]
    public async Task Preview_should_return_invalid_file_when_file_is_too_large()
    {
        var service = CreateService();
        var bytes = new byte[Visma4010ImportService.MaxFileSize + 1];
        await using var content = new MemoryStream(bytes);

        var result = await service.PreviewAsync(content, "visma.xlsx", CancellationToken.None);
        result.Status.Should().Be(Visma4010ImportStatus.InvalidFile);
        result.Message.Should().Be("Filen är större än 10 MB.");
        result.Response.Should().BeNull();
    }

    [Theory]
    [InlineData("visma.txt")]
    [InlineData("visma.csv")]
    [InlineData("visma.pdf")]
    [InlineData("visma")]
    public async Task Preview_should_reject_unsupported_file_extensions(string fileName)
    {
        var service = CreateService();
        await using var content = new MemoryStream([1, 2, 3]);

        var result = await service.PreviewAsync(content, fileName, CancellationToken.None);
        result.Status.Should().Be(Visma4010ImportStatus.InvalidFile);
        result.Message.Should().Be("Endast .xlsx- och .xls-filer stöds.");
    }

    [Theory]
    [InlineData("visma.XLSX")]
    [InlineData("visma.XLS")]
    public async Task Preview_should_accept_supported_extensions_regardless_of_case(
        string fileName)
    {
        // The file extension is accepted, but the content is not a valid Excel file.
        // Therefore, the expected error comes from Excel reading, not file extension validation.
        var service = CreateService();
        await using var content = new MemoryStream([1, 2, 3]);

        var result = await service.PreviewAsync(content, fileName, CancellationToken.None);
        result.Status.Should().Be(Visma4010ImportStatus.InvalidFile);
        result.Message.Should().NotBe("Endast .xlsx- och .xls-filer stöds.");
    }

    [Theory]
    [InlineData("folder/visma.pdf")]
    [InlineData("folder\\visma.pdf")]
    public async Task Preview_should_validate_extension_of_file_name(
        string fileName)
    {
        var service = CreateService();
        await using var content = new MemoryStream([1, 2, 3]);
        var result = await service.PreviewAsync(content, fileName, CancellationToken.None);
        result.Status.Should().Be(Visma4010ImportStatus.InvalidFile);
        result.Message.Should().Be("Endast .xlsx- och .xls-filer stöds.");
    }

    [Fact]
    public async Task Preview_should_propagate_cancellation()
    {
        var service = CreateService();
        await using var content = new MemoryStream([1, 2, 3]);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var act = () => service.PreviewAsync(content, "visma.xlsx", cancellation.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Preview_should_propagate_stream_read_errors()
    {
        var service = CreateService();
        await using var content = new ThrowingReadStream();
        var act = () => service.PreviewAsync(content, "visma.xlsx", CancellationToken.None);
        await act.Should().ThrowAsync<IOException>();
    }

    [Fact]
    public async Task Preview_should_return_warning_when_report_is_preliminary()
    {
        await using var context = TestDatabase.Create();
        var service = CreateService(context);
        await using var file = Visma4010FileBuilder.Standard().Preliminary().ToStream();

        var result = await service.PreviewAsync(file, "visma4010.xlsx", CancellationToken.None);
        result.Status.Should().Be(Visma4010ImportStatus.Ok);
        result.Response!.IsPreliminary.Should().BeTrue();
        result.Response.Warnings.Should().Contain(Visma4010Warnings.PreliminaryReport);
    }

    [Fact]
    public async Task Preview_should_be_blocked_when_reconciliation_fails()
    {
        await using var context = TestDatabase.Create();
        var service = CreateService(context);

        var builder = new Visma4010FileBuilder()
            .Account("4010", "Inköp material")
            .Invoice("L5", new DateTime(2026, 1, 1),
                "Levfkt 260034 IMI Hydronic Engineering AB", debit: 1000m)
            .Turnover(debit: 2000m)
            .Closing();

        await using var file = builder.ToStream();

        var result = await service.PreviewAsync(file, "visma4010.xlsx", CancellationToken.None);
        result.Status.Should().Be(Visma4010ImportStatus.Ok);
        result.Response!.CanImport.Should().BeFalse();
        result.Response.Blockers.Should().Contain(Visma4010Blockers.ReconciliationFailed);
    }

    /*******************************Imports*****************************/
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

[Fact]
    public async Task Import_should_create_batch_suppliers_and_purchased_goods_when_file_is_valid()
    {
        await using var context = TestDatabase.Create();

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "User",
            CreatedAt = DateTime.UtcNow
        };

        context.AppUsers.Add(user);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        await using var file = Visma4010FileBuilder.Standard().ToStream();

        var result = await service.ImportAsync(
            file,
            "visma4010.xlsx",
            user.Id,
            CancellationToken.None);

        result.Status.Should().Be(Visma4010ImportStatus.Ok);
        result.Response.Should().NotBeNull();
        result.Response!.ImportBatchId.Should().NotBeEmpty();

        var batch = await context.ImportBatches
            .Include(batch => batch.ImportBatchFiles)
            .SingleAsync();

        batch.Source.Should().Be("visma_4010");
        batch.ImportedBy.Should().Be(user.Id);
        batch.ImportBatchFiles.Should().ContainSingle();

        var suppliers = await context.Suppliers.ToListAsync();
        suppliers.Should().HaveCount(2);

        var purchasedGoods = await context.PurchasedGoods.ToListAsync();
        purchasedGoods.Should().HaveCount(2);

        purchasedGoods.Should().OnlyContain(
            good => good.ImportBatchId == batch.Id);

        purchasedGoods.Should().OnlyContain(
            good => good.CreatedBy == user.Id);
    }

    [Fact]
    public async Task Import_should_be_blocked_when_file_has_already_been_imported()
    {
        await using var context = TestDatabase.Create();

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "User",
            CreatedAt = DateTime.UtcNow
        };

        context.AppUsers.Add(user);
        await context.SaveChangesAsync();
        
        var service = CreateService(context);
        var bytes = Visma4010FileBuilder.Standard().ToBytes();
        await using var firstFile = new MemoryStream(bytes);

        var firstResult = await service.ImportAsync(
            firstFile, "visma4010.xlsx", user.Id, CancellationToken.None);

        firstResult.Status.Should().Be(Visma4010ImportStatus.Ok);

        await using var secondFile = new MemoryStream(bytes);

        var secondResult = await service.ImportAsync(
            secondFile, "visma4010.xlsx", user.Id, CancellationToken.None);

        secondResult.Status.Should().Be(Visma4010ImportStatus.Blocked);
        secondResult.Response!.CanImport.Should().BeFalse();
        secondResult.Response.Blockers.Should()
            .Contain(Visma4010Blockers.FileAlreadyImported);
    }

    /*[Fact]
public async Task Import_should_reuse_existing_supplier_after_normalizing_name()
{
    await using var context = TestDatabase.Create();

    var user = new AppUser
    {
        Id = Guid.NewGuid(),
        FirstName = "Test",
        LastName = "User",
        CreatedAt = DateTime.UtcNow
    };

    var existingSupplier = new Supplier
    {
        Id = Guid.NewGuid(),
        Name = "IMI   Hydronic Engineering AB"
    };

    context.AppUsers.Add(user);
    context.Suppliers.Add(existingSupplier);

    await context.SaveChangesAsync();

    var service = CreateService(context);

    var result = await service.ImportAsync(
        Visma4010FileBuilder.Standard().ToStream(),
        "test.xlsx",
        user.Id,
        CancellationToken.None);

    result.Status.Should().Be(Visma4010ImportStatus.Ok);

    var suppliers = await context.Suppliers
    .Select(supplier => new { supplier.Id, supplier.Name }).ToListAsync();

    foreach (var supplier in suppliers)

    {
        Console.WriteLine($"Supplier: '{supplier.Name}' ({supplier.Id})");
    }

    suppliers.Should().HaveCount(1);
}
*/

[Fact]
public async Task Import_should_skip_invoice_that_already_exists()
{
    await using var context = TestDatabase.Create();

    var user = new AppUser
    {
        Id = Guid.NewGuid(),
        FirstName = "Test",
        LastName = "User",
        CreatedAt = DateTime.UtcNow
    };

    var supplier = new Supplier
    {
        Id = Guid.NewGuid(),
        Name = "Existing supplier"
    };

    context.AppUsers.Add(user);
    context.Suppliers.Add(supplier);

    context.PurchasedGoods.Add(new PurchasedGood
    {
        Id = Guid.NewGuid(),
        SupplierId = supplier.Id,
        TransactionDate = new DateOnly(2026, 1, 1),
        Value = 3159.60m,
        Unit = "sek",
        MeasurementQuality = "measured",
        SourceRecordId = "260034",
        CreatedBy = user.Id,
        CreatedAt = DateTime.UtcNow
    });

    await context.SaveChangesAsync();

    var service = CreateService(context);
    await using var file = Visma4010FileBuilder.Standard().ToStream();

    var result = await service.ImportAsync(
        file, "visma4010.xlsx", user.Id, CancellationToken.None);

    result.Status.Should().Be(Visma4010ImportStatus.Ok);

    var goods = await context.PurchasedGoods.ToListAsync();
    goods.Should().HaveCount(2);
    goods.Count(good => good.SourceRecordId == "260034").Should().Be(1);
    goods.Count(good => good.SourceRecordId == "260035").Should().Be(1);
}


[Fact]
public async Task Import_should_be_blocked_when_all_invoices_already_exist()
{
    await using var context = TestDatabase.Create();

    var user = new AppUser
    {
        Id = Guid.NewGuid(),
        FirstName = "Test",
        LastName = "User",
        CreatedAt = DateTime.UtcNow
    };

    var supplier = new Supplier
    {
        Id = Guid.NewGuid(),
        Name = "Existing supplier"
    };

    context.AppUsers.Add(user);
    context.Suppliers.Add(supplier);

    context.PurchasedGoods.AddRange(
        new PurchasedGood
        {
            Id = Guid.NewGuid(),
            SupplierId = supplier.Id,
            TransactionDate = new DateOnly(2026, 1, 1),
            Value = 3159.60m,
            Unit = "sek",
            MeasurementQuality = "measured",
            SourceRecordId = "260034",
            CreatedBy = user.Id,
            CreatedAt = DateTime.UtcNow
        },
        new PurchasedGood
        {
            Id = Guid.NewGuid(),
            SupplierId = supplier.Id,
            TransactionDate = new DateOnly(2026, 2, 3),
            Value = 1000m,
            Unit = "sek",
            MeasurementQuality = "measured",
            SourceRecordId = "260035",
            CreatedBy = user.Id,
            CreatedAt = DateTime.UtcNow
        });

    await context.SaveChangesAsync();

    var service = CreateService(context);
    await using var file = Visma4010FileBuilder.Standard().ToStream();

    var result = await service.ImportAsync(file, "visma4010.xlsx", user.Id, CancellationToken.None);
    result.Status.Should().Be(Visma4010ImportStatus.Blocked);
    result.Response!.CanImport.Should().BeFalse();
    result.Response.Blockers.Should().Contain(Visma4010Blockers.NothingToImport);
}

}