using FluentAssertions;
using SeconSbtiBackend.Importing.Visma;
using Xunit;
using tests.ImportingTests.Visma4010;
using System.Runtime.CompilerServices;

namespace tests.ImportingTests.Visma;
public class VismaSheetTests
{
    private static SheetRow Row(params object?[] cells) => new(7, cells);

    /**********************Has**********************/
    [Fact]
    public void Has_matches_text_ignoring_surrounding_whitespace()
    {
        Row("  Vernr ", 5.0).Has("Vernr").Should().BeTrue();
    }

    [Fact]
    public void Has_is_false_for_other_text_and_for_non_text_cells()
    {
        Row("Datum").Has("Vernr").Should().BeFalse();
        Row(12).Has("12").Should().BeFalse();
    }

    /**********************Text**********************/
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Text_is_null_for_empty_cells(string? value)
    {
        Row(value).Text(0).Should().BeNull();
    }

    [Fact]
    public void Text_trims_the_value()
    {
        Row("  L5  ").Text(0).Should().Be("L5");
    }

    [Fact]
    public void Text_formats_numbers_with_invariant_culture()
    {
        Row(4010.0).Text(0).Should().Be("4010");
        Row(1.5).Text(0).Should().Be("1.5");
    }

    [Fact]
    public void Text_outside_the_row_is_null()
    {
        Row("a").Text(5).Should().BeNull();
    }

    /**********************Date**********************/
    [Fact]
    public void Date_reads_date_cells()
    {
        Row(new DateTime(2026, 1, 31, 13, 45, 0)).Date(0).Should().Be(new DateOnly(2026, 1, 31));
    }

    [Fact]
    public void Date_is_null_for_text_and_empty_cells()
    {
        Row("2026-01-31").Date(0).Should().BeNull();
        Row((object?)null).Date(0).Should().BeNull();
    }

    /**********************Amount**********************/
    [Theory]
    [InlineData(3159.6, 3159.60)]
    [InlineData(-7973972.050000001, -7973972.05)] // float noise from Visma
    [InlineData(1.005, 1.01)]                     // away from zero
    [InlineData(-1.005, -1.01)]
    public void AmountOrNull_rounds_to_two_decimals(double value, double expected)
    {
        Row(value).AmountOrNull(0).Should().Be((decimal)expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AmountOrNull_is_null_for_empty_cells(string? value)
    {
        Row(value).AmountOrNull(0).Should().BeNull();
    }

    [Fact]
    public void Amount_is_zero_for_empty_cells()
    {
        Row((object?)null).Amount(0).Should().Be(0);
    }

    [Fact]
    public void Amount_throws_with_row_number_when_the_cell_is_not_a_number()
    {
        var act = () => Row("abc").Amount(0);
        act.Should().Throw<VismaFileFormatException>()
            .WithMessage("Rad 7 har ett belopp som inte är ett tal.");
    }

    /**********************Value**********************/
    [Fact]
    public void Value_outside_the_row_is_null()
    {
        Row("a", "b").Value(2).Should().BeNull();
    }

    /**********************ReadRows**********************/
    [Fact]
    public void ReadRows_returns_every_row_with_one_based_numbers()
    {
        using var stream = Visma4010FileBuilder.Standard().ToStream();

        var rows = VismaSheet.ReadRows(stream);

        // 3 header lines + column headers + account + opening balance + 2 invoices + turnover + closing
        rows.Should().HaveCount(10);
        rows.Select(row => row.Number).Should().Equal(Enumerable.Range(1, 10));
        rows[0].Text(0).Should().Be("Secon AB");
        rows[3].Has("Vernr").Should().BeTrue();
    }

    [Fact]
    public void ReadRows_returns_dates_and_numbers_as_typed_values()
    {
        using var stream = Visma4010FileBuilder.Standard().ToStream();
        var invoiceRow = VismaSheet.ReadRows(stream)[Visma4010FileBuilder.FirstTransactionRow - 1];

        invoiceRow.Text(2).Should().Be("L5");
        invoiceRow.Date(3).Should().Be(new DateOnly(2026, 1, 1));
        invoiceRow.Amount(5).Should().Be(3159.60m);
    }

    [Fact]
    public void ReadRows_throws_VismaFileFormatException_for_invalid_excel_file()
    {
        using var stream = new MemoryStream("not an excel file"u8.ToArray());
        var act = () => VismaSheet.ReadRows(stream);
        act.Should().Throw<VismaFileFormatException>()
            .WithMessage("Filen kunde inte läsas som en Excel-fil.");
    }

    [Fact]
    public void ReadRows_returns_empty_list_for_empty_excel_file()
    {
        using var stream = new MemoryStream();
        var act = () => VismaSheet.ReadRows(stream);
        act.Should().Throw<VismaFileFormatException>();
    }

    /*****************Sheet Row************************/
    [Fact(Skip = "Handle negative column indexes in VismaSheet and remove Skip")]
    public void Value_returns_null_for_negative_column()
    {
        Row("a", "b").Value(-1).Should().BeNull();
    }

    [Fact]
    public void Text_returns_null_when_value_is_null()
    {
        Row((object?)null).Text(0).Should().BeNull();
    }

    [Fact]
    public void AmountOrNull_throws_for_invalid_text()
    {
        var act = () => Row("abc").AmountOrNull(0);
        act.Should().Throw<VismaFileFormatException>()
            .WithMessage("Rad 7 har ett belopp som inte är ett tal.");
    }

    [Fact]
    public void AmountOrNull_throws_for_overflow()
    {
        var act = () => Row(double.MaxValue).AmountOrNull(0);
        act.Should().Throw<VismaFileFormatException>();
    }
}