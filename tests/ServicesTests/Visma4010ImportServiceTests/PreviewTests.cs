using FluentAssertions;
using SeconSbtiBackend.Importing.Visma4010;
using tests.ImportingTests.Visma4010;
using static tests.ServicesTests.Visma4010ImportServiceTests.Visma4010ImportServiceTestHelpers;

namespace tests.ServicesTests.Visma4010ImportServiceTests;


/// <summary>
/// PreviewAsync on readable files: summary, warnings, blockers and previous imports.
/// </summary>
public class PreviewTests
{
    [Fact]
    public async Task Preview_should_return_ok_when_file_is_valid()
    {
        await using var context = TestDatabase.Create();
        var service = CreateService(context);
        await using var file = Visma4010FileBuilder.Standard().ToStream();

        var result = await service.PreviewAsync(file, "visma4010.xlsx", CancellationToken.None);

        result.Status.Should().Be(Visma4010ImportStatus.Ok);
        result.Response.Should().NotBeNull();
        result.Message.Should().BeNull();
    }

    [Fact]
    public async Task Preview_should_summarize_what_will_be_imported_and_which_suppliers_are_new()
    {
        await using var context = TestDatabase.Create();
        var service = CreateService(context);
        await using var file = Visma4010FileBuilder.Standard().ToStream();

        var result = await service.PreviewAsync(file, "visma4010.xlsx", CancellationToken.None);

        var response = result.Response!;
        response.CanImport.Should().BeTrue();
        response.Blockers.Should().BeEmpty();
        response.ToImport.Count.Should().Be(2);
        response.ToImport.NetSek.Should().Be(4159.60m);
        response.AlreadyImported.Count.Should().Be(0);
        response.NewSuppliers.Should().Equal("IMI Hydronic Engineering AB", "Virken AB");
        response.Accounts.Should().ContainSingle().Which.Number.Should().Be("4010");
        response.PreviousImport.Should().BeNull();
        response.ImportBatchId.Should().BeNull();
    }

    /************************Warnings************************/
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
    public async Task Preview_should_not_warn_when_report_is_final_and_all_rows_have_a_supplier()
    {
        await using var context = TestDatabase.Create();
        var service = CreateService(context);
        await using var file = Visma4010FileBuilder.Standard().ToStream();

        var result = await service.PreviewAsync(file, "visma4010.xlsx", CancellationToken.None);

        result.Response!.Warnings.Should().BeEmpty();
        result.Response.RowsWithoutSupplier.Should().BeEmpty();
    }

    [Fact]
    public async Task Preview_should_warn_and_list_rows_without_supplier()
    {
        await using var context = TestDatabase.Create();
        var service = CreateService(context);

        var builder = Account()
            .Invoice("L1", Jan1, "Levfkt 1 A AB", debit: 100m)
            .Invoice("L2", Jan1, "Manuell korrigering", debit: 50m)
            .Totals();

        await using var file = builder.ToStream();

        var result = await service.PreviewAsync(file, "visma4010.xlsx", CancellationToken.None);

        var response = result.Response!;
        response.CanImport.Should().BeTrue();
        response.Warnings.Should().Contain(Visma4010Warnings.RowsWithoutSupplier);
        response.ToImport.Count.Should().Be(1);
        response.WithoutSupplier.Count.Should().Be(1);
        response.WithoutSupplier.NetSek.Should().Be(50m);

        var row = response.RowsWithoutSupplier.Should().ContainSingle().Subject;
        row.RowNumber.Should().Be(Visma4010FileBuilder.FirstTransactionRow + 1);
        row.Account.Should().Be("4010");
        row.Voucher.Should().Be("L2");
        row.Date.Should().Be(new DateOnly(2026, 1, 1));
        row.Text.Should().Be("Manuell korrigering");
        row.NetSek.Should().Be(50m);
    }

    /************************Blockers************************/
    [Fact]
    public async Task Preview_should_be_blocked_when_reconciliation_fails()
    {
        await using var context = TestDatabase.Create();
        var service = CreateService(context);

        var builder = Account()
            .Invoice("L5", Jan1, "Levfkt 260034 IMI Hydronic Engineering AB", debit: 1000m)
            .Turnover(debit: 2000m)
            .Closing();

        await using var file = builder.ToStream();

        var result = await service.PreviewAsync(file, "visma4010.xlsx", CancellationToken.None);

        result.Status.Should().Be(Visma4010ImportStatus.Ok);
        result.Response!.CanImport.Should().BeFalse();
        result.Response.Blockers.Should().Contain(Visma4010Blockers.ReconciliationFailed);
        result.Response.Issues.Should().ContainSingle()
            .Which.Code.Should().Be(ParsedIssueCodes.TurnoverMismatch);
    }

    [Fact]
    public async Task Preview_should_be_blocked_when_invoice_number_is_repeated_in_the_file()
    {
        await using var context = TestDatabase.Create();
        var service = CreateService(context);

        var builder = Account()
            .Invoice("L1", Jan1, "Levfkt 5 A AB", debit: 100m)
            .Invoice("L2", Jan1, "Levfkt 5 B AB", debit: 200m)
            .Totals();

        await using var file = builder.ToStream();

        var result = await service.PreviewAsync(file, "visma4010.xlsx", CancellationToken.None);

        result.Status.Should().Be(Visma4010ImportStatus.Ok);
        result.Response!.CanImport.Should().BeFalse();
        result.Response.Blockers.Should().Equal(Visma4010Blockers.DuplicateInvoiceNumber);
    }

    [Fact]
    public async Task Preview_should_report_previous_import_when_file_was_imported_before()
    {
        await using var context = TestDatabase.Create();
        var user = await SeedUserAsync(context);
        var service = CreateService(context);
        var bytes = Visma4010FileBuilder.Standard().ToBytes();

        await service.ImportAsync(new MemoryStream(bytes), "visma4010.xlsx", user.Id, CancellationToken.None);
        var result = await service.PreviewAsync(new MemoryStream(bytes), "visma4010.xlsx", CancellationToken.None);

        var response = result.Response!;
        response.CanImport.Should().BeFalse();
        response.Blockers.Should().Equal(Visma4010Blockers.FileAlreadyImported);
        response.PreviousImport.Should().NotBeNull();
        response.PreviousImport!.ImportedBy.Should().Be("Test User");
        response.AlreadyImported.Count.Should().Be(2);
        response.ToImport.Count.Should().Be(0);
    }
}