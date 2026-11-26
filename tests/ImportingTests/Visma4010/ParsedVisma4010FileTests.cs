using FluentAssertions;
using SeconSbtiBackend.Importing.Visma4010;
using Xunit;

namespace tests.Importing.Visma4010;

public class ParsedVisma4010FileTests
{
    [Fact]
    public void ParsedVisma4010File_initializes_empty_collections()
    {
        var file = new ParsedVisma4010File();
        file.Accounts.Should().BeEmpty();
        file.Issues.Should().BeEmpty();
    }

    [Fact]
    public void ParsedVisma4010File_stores_file_properties()
    {
        var file = new ParsedVisma4010File
        {
            Company = "Secon AB",
            IsPreliminary = true,
            PeriodFrom = new DateOnly(2026, 1, 1),
            PeriodTo = new DateOnly(2026, 9, 30)
        };

        file.Company.Should().Be("Secon AB");
        file.IsPreliminary.Should().BeTrue();
        file.PeriodFrom.Should().Be(new DateOnly(2026, 1, 1));
        file.PeriodTo.Should().Be(new DateOnly(2026, 9, 30));
    }

    [Fact]
    public void ParsedAccount_initializes_empty_transactions()
    {
        var account = new ParsedAccount
        {
            RowNumber = 5,
            Number = "4010",
            Name = "Inköp material"
        };

        account.Transactions.Should().BeEmpty();
        account.OpeningBalance.Should().Be(0);
        account.Turnover.Should().BeNull();
        account.Closing.Should().BeNull();
    }

    [Fact]
    public void ParsedAccount_stores_account_properties()
    {
        var account = new ParsedAccount
        {
            RowNumber = 5,
            Number = "4010",
            Name = "Inköp material",
            OpeningBalance = 1500m
        };

        account.RowNumber.Should().Be(5);
        account.Number.Should().Be("4010");
        account.Name.Should().Be("Inköp material");
        account.OpeningBalance.Should().Be(1500m);
    }

    [Fact]
    public void ParsedAccount_stores_summary_rows()
    {
        var turnover = new ParsedSummaryRow(10, 5000m, 1000m, 5500m);
        var closing = new ParsedSummaryRow(11, 5000m, 1000m, 5500m);
        var account = new ParsedAccount
        {
            RowNumber = 5,
            Number = "4010",
            Name = "Inköp material",
            Turnover = turnover,
            Closing = closing
        };

        account.Turnover.Should().Be(turnover);
        account.Closing.Should().Be(closing);
    }

    [Fact]
    public void ParsedSummaryRow_stores_values()
    {
        var summary = new ParsedSummaryRow(10, 5000m, 1000m, 5500m);

        summary.RowNumber.Should().Be(10);
        summary.Debit.Should().Be(5000m);
        summary.Credit.Should().Be(1000m);
        summary.Balance.Should().Be(5500m);
    }

    [Fact]
    public void ParsedTransaction_initializes_optional_properties_as_null()
    {
        var transaction = CreateTransaction();

        transaction.Supplier.Should().BeNull();
        transaction.InvoiceNumber.Should().BeNull();
    }

    [Fact]
    public void ParsedTransaction_stores_transaction_properties()
    {
        var transaction = CreateTransaction();

        transaction.RowNumber.Should().Be(7);
        transaction.Account.Should().Be("4010");
        transaction.Voucher.Should().Be("L5");
        transaction.Date.Should().Be(new DateOnly(2026, 1, 1));
        transaction.Text.Should().Be("Inköp material");
        transaction.Debit.Should().Be(3159.60m);
        transaction.Credit.Should().Be(0m);
        transaction.Balance.Should().Be(3159.60m);
    }

    [Fact]
    public void ParsedTransaction_calculates_net_from_debit_and_credit()
    {
        var transaction = CreateTransaction();
        transaction.Net.Should().Be(3159.60m);
    }

    [Fact]
    public void ParsedTransaction_calculates_negative_net_when_credit_is_higher()
    {
        var transaction = CreateTransaction(debit: 100m, credit: 300m);
        transaction.Net.Should().Be(-200m);
    }

    [Fact]
    public void ParsedTransaction_allows_setting_supplier_and_invoice_number()
    {
        var transaction = CreateTransaction();

        transaction.Supplier = "IMI Hydronic Engineering AB";
        transaction.InvoiceNumber = "260034";
        transaction.Supplier.Should().Be("IMI Hydronic Engineering AB");
        transaction.InvoiceNumber.Should().Be("260034");
    }

    [Fact]
    public void ParsedIssue_stores_row_number_and_code()
    {
        var issue = new ParsedIssue(7, ParsedIssueCodes.BalanceMismatch);
        issue.RowNumber.Should().Be(7);
        issue.Code.Should().Be("BalanceMismatch");
    }

    [Fact]
    public void ParsedIssueCodes_contains_expected_values()
    {
        ParsedIssueCodes.BalanceMismatch.Should().Be("BalanceMismatch");
        ParsedIssueCodes.TurnoverMissing.Should().Be("TurnoverMissing");
        ParsedIssueCodes.TurnoverMismatch.Should().Be("TurnoverMismatch");
        ParsedIssueCodes.ClosingBalanceMissing.Should().Be("ClosingBalanceMissing");
        ParsedIssueCodes.ClosingBalanceMismatch.Should().Be("ClosingBalanceMismatch");
        ParsedIssueCodes.DuplicateInvoiceNumber.Should().Be("DuplicateInvoiceNumber");
    }

    private static ParsedTransaction CreateTransaction(
        decimal debit = 3159.60m,
        decimal credit = 0m) =>
        new()
        {
            RowNumber = 7,
            Account = "4010",
            Voucher = "L5",
            Date = new DateOnly(2026, 1, 1),
            Text = "Inköp material",
            Debit = debit,
            Credit = credit,
            Balance = debit - credit
        };
}
