using FluentAssertions;
using SeconSbtiBackend.Importing.Visma;
using Xunit;

namespace tests.ImportingTests.Visma;

public class VismaReportHeaderTests
{
    private static SheetRow Row(int number, params object?[] cells) => new(number, cells);

    private static VismaReportHeader Parse(params string?[] lines) =>
        VismaReportHeader.Parse(lines.Select((line, index) => Row(index + 1, line)).ToList());

    [Fact]
    public void Company_is_the_first_text_line_and_title_the_second()
    {
        var header = Parse("Secon AB", "Huvudbok");
        header.Company.Should().Be("Secon AB");
        header.Title.Should().Be("Huvudbok");
    }

    [Fact]
    public void Empty_rows_are_skipped_when_looking_for_company_and_title()
    {
        var header = Parse(null, "Secon AB", "", "Huvudbok");
        header.Company.Should().Be("Secon AB");
        header.Title.Should().Be("Huvudbok");
    }

    [Fact]
    public void Empty_input_gives_an_empty_header()
    {
        var header = VismaReportHeader.Parse([]);
        header.Company.Should().BeNull();
        header.Title.Should().BeNull();
        header.PeriodFrom.Should().BeNull();
        header.PeriodTo.Should().BeNull();
        header.IsPreliminary.Should().BeFalse();
    }

    [Fact]
    public void Period_with_two_digit_years_is_read_as_20xx()
    {
        var header = Parse("Secon AB", "Huvudbok", "Period: 26-01-01 - 26-09-30");
        header.PeriodFrom.Should().Be(new DateOnly(2026, 1, 1));
        header.PeriodTo.Should().Be(new DateOnly(2026, 9, 30));
    }

    [Fact]
    public void Period_with_four_digit_years_is_accepted()
    {
        var header = Parse("Period: 2026-01-01 - 2026-09-30");
        header.PeriodFrom.Should().Be(new DateOnly(2026, 1, 1));
        header.PeriodTo.Should().Be(new DateOnly(2026, 9, 30));
    }

    [Fact]
    public void Fiscal_year_is_kept_apart_from_the_period()
    {
        var header = Parse("Räkenskapsår: 26-01-01 - 26-12-31", "Period: 26-01-01 - 26-09-30");
        header.FiscalYearFrom.Should().Be(new DateOnly(2026, 1, 1));
        header.FiscalYearTo.Should().Be(new DateOnly(2026, 12, 31));
        header.PeriodTo.Should().Be(new DateOnly(2026, 9, 30));
    }

    [Fact]
    public void Invalid_period_dates_give_null()
    {
        var header = Parse("Period: 26-13-45 - 26-99-99");

        header.PeriodFrom.Should().BeNull();
        header.PeriodTo.Should().BeNull();
    }

    [Fact]
    public void Preliminary_marker_is_detected_and_not_used_as_company()
    {
        var header = Parse("Preliminär", "Secon AB");
        header.IsPreliminary.Should().BeTrue();
        header.Company.Should().Be("Secon AB");
    }

    [Fact]
    public void Result_unit_is_read()
    {
        Parse("Secon AB", "Resultatenhet:  Stockholm ").ResultUnit.Should().Be("Stockholm");
    }

    [Fact]
    public void Printed_time_combines_the_date_with_the_time_in_the_row_below()
    {
        var rows = new List<SheetRow>
        {
            Row(1, "Secon AB"),
            Row(2, null, null, null, "Utskrivet:", new DateTime(2026, 10, 1)),
            Row(3, null, null, null, null, "14:30")
        };

        VismaReportHeader.Parse(rows).PrintedAt.Should().Be(new DateTime(2026, 10, 1, 14, 30, 0));
    }

    [Fact]
    public void Printed_time_defaults_to_midnight_when_the_time_is_missing()
    {
        var rows = new List<SheetRow>
        {
            Row(1, "Secon AB"),
            Row(2, null, null, null, "Utskrivet:", "26-10-01")
        };

        VismaReportHeader.Parse(rows).PrintedAt.Should().Be(new DateTime(2026, 10, 1));
    }

    [Fact]
    public void Printed_is_null_when_the_date_cannot_be_read()
    {
        var rows = new List<SheetRow>
        {
            Row(1, "Secon AB"),
            Row(2, null, null, null, "Utskrivet:", "inte ett datum")
        };

        VismaReportHeader.Parse(rows).PrintedAt.Should().BeNull();
    }

    [Fact]
    public void Last_voucher_is_read_from_the_cell_after_the_label()
    {
        var rows = new List<SheetRow>
        {
            Row(1, "Secon AB"),
            Row(2, null, null, null, "Senaste vernr:", "L123")
        };

        VismaReportHeader.Parse(rows).LastVoucher.Should().Be("L123");
    }
}