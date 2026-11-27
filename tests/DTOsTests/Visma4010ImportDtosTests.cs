using FluentAssertions;
using SeconSbtiBackend.Importing.Visma4010;
using Xunit;

namespace tests.DTOsTests;

public class Visma4010ImportDtosTests
{
    [Fact]
    public void Visma4010ImportResponse_stores_all_properties()
    {
        var importedAt = new DateTime(2026, 11, 19, 10, 0, 0);
        var batchId = Guid.NewGuid();

        var account = new Visma4010AccountResponse("4010", "Inköp material");
        var issue = new Visma4010IssueResponse(7, "BalanceMismatch");
        var previousImport = new Visma4010PreviousImportResponse(importedAt, "admin");
        var toImport = new Visma4010RowsSummary(2, 4159.60m);
        var alreadyImported = new Visma4010RowsSummary(1, 1000m);
        var withoutSupplier = new Visma4010RowsSummary(1, 500m);
        var rowWithoutSupplier = new Visma4010RowResponse(
            8, "4010", "L6", new DateOnly(2026, 2, 3), "Inköp material", 500m);

        var response = new Visma4010ImportResponse
        {
            FileName = "visma.xlsx",
            Company = "Secon AB",
            PeriodFrom = new DateOnly(2026, 1, 1),
            PeriodTo = new DateOnly(2026, 9, 30),
            IsPreliminary = true,
            Accounts = [account],
            CanImport = true,
            Blockers = [],
            Warnings = ["PreliminaryReport"],
            Issues = [issue],
            PreviousImport = previousImport,
            ToImport = toImport,
            AlreadyImported = alreadyImported,
            WithoutSupplier = withoutSupplier,
            NewSuppliers = ["IMI Hydronic Engineering AB"],
            RowsWithoutSupplier = [rowWithoutSupplier],
            ImportBatchId = batchId
        };

        response.FileName.Should().Be("visma.xlsx");
        response.Company.Should().Be("Secon AB");
        response.PeriodFrom.Should().Be(new DateOnly(2026, 1, 1));
        response.PeriodTo.Should().Be(new DateOnly(2026, 9, 30));
        response.IsPreliminary.Should().BeTrue();
        response.Accounts.Should().ContainSingle().Which.Should().Be(account);
        response.CanImport.Should().BeTrue();
        response.Blockers.Should().BeEmpty();
        response.Warnings.Should().ContainSingle("PreliminaryReport");
        response.Issues.Should().ContainSingle().Which.Should().Be(issue);
        response.PreviousImport.Should().Be(previousImport);
        response.ToImport.Should().Be(toImport);
        response.AlreadyImported.Should().Be(alreadyImported);
        response.WithoutSupplier.Should().Be(withoutSupplier);
        response.NewSuppliers.Should().ContainSingle("IMI Hydronic Engineering AB");
        response.RowsWithoutSupplier.Should().ContainSingle().Which.Should().Be(rowWithoutSupplier);
        response.ImportBatchId.Should().Be(batchId);
    }

    [Fact]
    public void Visma4010ImportResponse_allows_null_optional_properties()
    {
        var response = CreateResponse();

        response.Company.Should().BeNull();
        response.PeriodFrom.Should().BeNull();
        response.PeriodTo.Should().BeNull();
        response.PreviousImport.Should().BeNull();
        response.ImportBatchId.Should().BeNull();
    }

    [Fact]
    public void Visma4010AccountResponse_stores_values()
    {
        var account = new Visma4010AccountResponse("4010", "Inköp material");
        account.Number.Should().Be("4010");
        account.Name.Should().Be("Inköp material");
    }

    [Fact]
    public void Visma4010IssueResponse_stores_values()
    {
        var issue = new Visma4010IssueResponse(7, "BalanceMismatch");
        issue.RowNumber.Should().Be(7);
        issue.Code.Should().Be("BalanceMismatch");
    }

    [Fact]
    public void Visma4010PreviousImportResponse_stores_values()
    {
        var importedAt = new DateTime(2026, 11, 19, 10, 0, 0);
        var previousImport = new Visma4010PreviousImportResponse(
            importedAt,
            "admin");

        previousImport.ImportedAt.Should().Be(importedAt);
        previousImport.ImportedBy.Should().Be("admin");
    }

    [Fact]
    public void Visma4010RowsSummary_stores_values()
    {
        var summary = new Visma4010RowsSummary(5, 4159.60m);
        summary.Count.Should().Be(5);
        summary.NetSek.Should().Be(4159.60m);
    }

    [Fact]
    public void Visma4010RowResponse_stores_values()
    {
        var row = new Visma4010RowResponse(
            7,
            "4010",
            "L5",
            new DateOnly(2026, 1, 1),
            "Inköp material",
            3159.60m);

        row.RowNumber.Should().Be(7);
        row.Account.Should().Be("4010");
        row.Voucher.Should().Be("L5");
        row.Date.Should().Be(new DateOnly(2026, 1, 1));
        row.Text.Should().Be("Inköp material");
        row.NetSek.Should().Be(3159.60m);
    }

    [Fact]
    public void Visma4010Blockers_contains_expected_values()
    {
        Visma4010Blockers.FileAlreadyImported.Should().Be("FileAlreadyImported");
        Visma4010Blockers.ReconciliationFailed.Should().Be("ReconciliationFailed");
        Visma4010Blockers.DuplicateInvoiceNumber.Should().Be("DuplicateInvoiceNumber");
        Visma4010Blockers.NothingToImport.Should().Be("NothingToImport");
    }

    [Fact]
    public void Visma4010Warnings_contains_expected_values()
    {
        Visma4010Warnings.PreliminaryReport.Should().Be("PreliminaryReport");
        Visma4010Warnings.RowsWithoutSupplier.Should().Be("RowsWithoutSupplier");
    }

    private static Visma4010ImportResponse CreateResponse() =>
        new()
        {
            FileName = "visma.xlsx",
            Accounts = [],
            CanImport = false,
            Blockers = [],
            Warnings = [],
            Issues = [],
            ToImport = new Visma4010RowsSummary(0, 0m),
            AlreadyImported = new Visma4010RowsSummary(0, 0m),
            WithoutSupplier = new Visma4010RowsSummary(0, 0m),
            NewSuppliers = [],
            RowsWithoutSupplier = []
        };
}
