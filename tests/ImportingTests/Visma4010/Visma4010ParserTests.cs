using FluentAssertions;
using SeconSbtiBackend.Importing.Visma;
using SeconSbtiBackend.Importing.Visma4010;
using Xunit;

namespace tests.ImportingTests.Visma4010;

public class Visma4010ParserTests
{
    private static readonly DateTime Jan1 = new(2026, 1, 1);

    private static ParsedVisma4010File Parse(Visma4010FileBuilder builder)
    {
        using var stream = builder.ToStream();
        return Visma4010Parser.Parse(stream);
    }

    //Test helper
    private static Visma4010FileBuilder Account(decimal opening = 0) =>
        new Visma4010FileBuilder().Account("4010", "Inköp material", opening);


    /**********************Header**********************/
    [Fact]
    public void Parse_reads_company_and_period_from_the_header()
    {
        var file = Parse(Visma4010FileBuilder.Standard());
        file.Company.Should().Be("Secon AB");
        file.PeriodFrom.Should().Be(new DateOnly(2026, 1, 1));
        file.PeriodTo.Should().Be(new DateOnly(2026, 9, 30));
        file.IsPreliminary.Should().BeFalse();
    }

    [Fact]
    public void Parse_detects_a_preliminary_report()
    {
        Parse(Visma4010FileBuilder.Standard().Preliminary()).IsPreliminary.Should().BeTrue();
    }

    /**********************Accounts and transactions**********************/
    [Fact]
    public void Parse_reads_account_number_name_and_opening_balance()
    {
        var file = Parse(Account(opening: 1500m).Invoice("L1", Jan1, "Levfkt 1 A AB", debit: 100m).Totals());
        var account = file.Accounts.Should().ContainSingle().Subject;
        account.Number.Should().Be("4010");
        account.Name.Should().Be("Inköp material");
        account.OpeningBalance.Should().Be(1500m);
        account.RowNumber.Should().Be(Visma4010FileBuilder.FirstAccountRow);
    }

    [Fact]
    public void Parse_reads_all_fields_of_an_invoice_row()
    {
        var file = Parse(Visma4010FileBuilder.Standard());
        var transactions = file.Accounts.Single().Transactions;
        transactions.Should().HaveCount(2);

        var first = transactions[0];
        first.Account.Should().Be("4010");
        first.Voucher.Should().Be("L5");
        first.Date.Should().Be(new DateOnly(2026, 1, 1));
        first.Text.Should().Be("Levfkt 260034 IMI Hydronic Engineering AB");
        first.Debit.Should().Be(3159.60m);
        first.Credit.Should().Be(0);
        first.Balance.Should().Be(3159.60m);
        first.Net.Should().Be(3159.60m);
        first.InvoiceNumber.Should().Be("260034");
        first.Supplier.Should().Be("IMI Hydronic Engineering AB");
        first.RowNumber.Should().Be(Visma4010FileBuilder.FirstTransactionRow);
    }

    [Fact]
    public void Parse_reads_turnover_and_closing_rows()
    {
        var account = Parse(Visma4010FileBuilder.Standard()).Accounts.Single();
        account.Turnover.Should().NotBeNull();
        account.Turnover!.Debit.Should().Be(4159.60m);
        account.Turnover.Credit.Should().Be(0);
        account.Closing.Should().NotBeNull();
        account.Closing!.Balance.Should().Be(4159.60m);
    }

    [Fact]
    public void Parse_of_a_consistent_file_has_no_issues()
    {
        Parse(Visma4010FileBuilder.Standard()).Issues.Should().BeEmpty();
    }

    [Fact]
    public void Parse_keeps_transactions_with_the_account_they_belong_to()
    {
        var file = Parse(new Visma4010FileBuilder()
            .Account("4010", "Inköp material", opening: 1000m)
            .Invoice("L1", Jan1, "Levfkt 1 A AB", debit: 200m)
            .Totals()
            .Account("4020", "Underentreprenad")
            .Invoice("L2", Jan1, "Levfkt 2 B AB", debit: 300m)
            .Totals());

        file.Accounts.Select(account => account.Number).Should().Equal("4010", "4020");
        file.Accounts[0].OpeningBalance.Should().Be(1000m);
        file.Accounts[0].Transactions.Should().ContainSingle().Which.Account.Should().Be("4010");
        file.Accounts[1].OpeningBalance.Should().Be(0);
        file.Accounts[1].Transactions.Should().ContainSingle().Which.Account.Should().Be("4020");
        file.Issues.Should().BeEmpty();
    }

    [Fact]
    public void Parse_rounds_float_noise_in_amounts()
    {
        var file = Parse(Account().Invoice("L1", Jan1, "Levfkt 1 A AB", debit: 3159.600000001m).Totals());
        file.Accounts.Single().Transactions.Single().Debit.Should().Be(3159.60m);
        file.Issues.Should().BeEmpty();
    }

    [Fact]
    public void Parse_reads_a_credit_note_as_a_negative_net()
    {
        var file = Parse(Account().Invoice("L7", Jan1, "Levkreditfkt 9 Virken AB", credit: 500m).Totals());
        var transaction = file.Accounts.Single().Transactions.Single();
        transaction.Credit.Should().Be(500m);
        transaction.Debit.Should().Be(0);
        transaction.Net.Should().Be(-500m);
        transaction.InvoiceNumber.Should().Be("9");
        transaction.Supplier.Should().Be("Virken AB");
        file.Issues.Should().BeEmpty();
    }

    /**********************Supplier and invoice number**********************/
    [Fact]
    public void Parse_uses_transinfo_when_the_text_has_no_invoice_pattern()
    {
        var file = Parse(Account()
            .Invoice("L1", Jan1, "Faktura utan nummer", debit: 100m)
            .TransInfo("LevFktnr: 777, Namn: Acme AB")
            .Totals());

        var transaction = file.Accounts.Single().Transactions.Single();
        transaction.InvoiceNumber.Should().Be("777");
        transaction.Supplier.Should().Be("Acme AB");
    }

    [Fact]
    public void Parse_prefers_the_text_over_transinfo_because_transinfo_cuts_long_names()
    {
        var file = Parse(Account()
            .Invoice("L1", Jan1, "Levfkt 1 Långt Leverantörsnamn AB", debit: 100m)
            .TransInfo("LevFktnr: 1, Namn: Långt Leverantörsn")
            .Totals());

        file.Accounts.Single().Transactions.Single().Supplier.Should().Be("Långt Leverantörsnamn AB");
    }

    [Fact]
    public void Parse_leaves_supplier_and_invoice_number_empty_for_manual_postings()
    {
        var file = Parse(Account().Invoice("L1", Jan1, "Manuell korrigering", debit: 100m).Totals());
        var transaction = file.Accounts.Single().Transactions.Single();
        transaction.Supplier.Should().BeNull();
        transaction.InvoiceNumber.Should().BeNull();
    }

    [Fact]
    public void Parse_collapses_repeated_whitespace_in_supplier_names()
    {
        var file = Parse(Account().Invoice("L1", Jan1, "Levfkt 5 Virken   AB", debit: 100m).Totals());
        file.Accounts.Single().Transactions.Single().Supplier.Should().Be("Virken AB");
    }

    [Fact]
    public void Parse_uses_the_most_common_spelling_of_a_supplier_everywhere()
    {
        var file = Parse(Account()
            .Invoice("L1", Jan1, "Levfkt 1 VIRKEN AB", debit: 100m)
            .Invoice("L2", Jan1, "Levfkt 2 Virken AB", debit: 100m)
            .Invoice("L3", Jan1, "Levfkt 3 Virken AB", debit: 100m)
            .Totals());

        file.Accounts.Single().Transactions
            .Select(transaction => transaction.Supplier)
            .Should().OnlyContain(supplier => supplier == "Virken AB");
    }

    /**********************Reconciliation issues**********************/
    [Fact]
    public void Parse_reports_one_balance_mismatch_for_one_wrong_row()
    {
        var file = Parse(Account()
            .Invoice("L1", Jan1, "Levfkt 1 A AB", debit: 100m, balance: 999m)
            .Invoice("L2", Jan1, "Levfkt 2 A AB", debit: 100m)
            .Invoice("L3", Jan1, "Levfkt 3 A AB", debit: 100m)
            .Totals());

        file.Issues.Should().ContainSingle().Which
            .Should().Be(new ParsedIssue(Visma4010FileBuilder.FirstTransactionRow, ParsedIssueCodes.BalanceMismatch));
    }

    [Fact]
    public void Parse_reports_missing_turnover_and_closing_rows()
    {
        var file = Parse(Account().Invoice("L1", Jan1, "Levfkt 1 A AB", debit: 100m));
        file.Issues.Should().Equal(
            new ParsedIssue(Visma4010FileBuilder.FirstAccountRow, ParsedIssueCodes.TurnoverMissing),
            new ParsedIssue(Visma4010FileBuilder.FirstAccountRow, ParsedIssueCodes.ClosingBalanceMissing));
    }

    [Fact]
    public void Parse_reports_turnover_that_does_not_match_the_transactions()
    {
        var file = Parse(Account()
            .Invoice("L1", Jan1, "Levfkt 1 A AB", debit: 100m)
            .Turnover(debit: 1m)
            .Closing());

        file.Issues.Should().ContainSingle().Which
            .Should().Be(new ParsedIssue(Visma4010FileBuilder.FirstTransactionRow + 1, ParsedIssueCodes.TurnoverMismatch));
    }

    [Fact]
    public void Parse_reports_closing_balance_that_does_not_match_opening_plus_turnover()
    {
        var file = Parse(Account(opening: 50m)
            .Invoice("L1", Jan1, "Levfkt 1 A AB", debit: 100m)
            .Turnover()
            .Closing(balance: 999m));

        file.Issues.Should().ContainSingle().Which
            .Should().Be(new ParsedIssue(Visma4010FileBuilder.FirstTransactionRow + 2, ParsedIssueCodes.ClosingBalanceMismatch));
    }

    [Fact]
    public void Parse_reports_a_repeated_invoice_number_on_the_second_row()
    {
        var file = Parse(Account()
            .Invoice("L1", Jan1, "Levfkt 5 A AB", debit: 100m)
            .Invoice("L2", Jan1, "Levfkt 5 B AB", debit: 200m)
            .Totals());

        file.Issues.Should().ContainSingle().Which
            .Should().Be(new ParsedIssue(Visma4010FileBuilder.FirstTransactionRow + 1, ParsedIssueCodes.DuplicateInvoiceNumber));
    }

    /**********************Invalid files**********************/
    [Fact]
    public void Parse_throws_when_vernr_and_datum_headers_are_missing()
    {
        var builder = Visma4010FileBuilder.Standard()
            .WithColumnHeaders("Konto", "Namn", "Verifikation", "Datum", "Text", "Debet", "Kredit", "Saldo");

        var act = () => Parse(builder);
        act.Should().Throw<VismaFileFormatException>().WithMessage("*Vernr*Datum*");
    }

    [Fact]
    public void Parse_throws_and_names_the_missing_column()
    {
        var builder = Visma4010FileBuilder.Standard()
            .WithColumnHeaders("Konto", "Namn", "Vernr", "Datum", "Text", "Debet", "Kredit", "Annat");

        var act = () => Parse(builder);
        act.Should().Throw<VismaFileFormatException>().WithMessage("Kolumnen 'Saldo' saknas i filen.");
    }

    [Fact]
    public void Parse_throws_when_the_file_has_no_account()
    {
        var act = () => Parse(new Visma4010FileBuilder());
        act.Should().Throw<VismaFileFormatException>().WithMessage("Filen innehåller inget konto.");
    }

    [Fact]
    public void Parse_throws_when_an_amount_is_not_a_number()
    {
        var builder = Account().RawRow(null, null, "L1", Jan1, "Levfkt 1 A AB", "abc", null, 100m);
        var act = () => Parse(builder);
        act.Should().Throw<VismaFileFormatException>().WithMessage("*inte är ett tal*");
    }
}