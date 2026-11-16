using System.Globalization;
using System.Text.RegularExpressions;

namespace SeconSbtiBackend.Importing.Visma;

public class VismaReportHeader
{
    public const string PreliminaryText = "Preliminär";

    private const string PrintedLabel = "Utskrivet:";
    private const string LastVoucherLabel = "Senaste vernr:";

    private static readonly Regex RangePattern = new(@"^(Räkenskapsår|Period):\s*(\d{2}(?:\d{2})?-\d{2}-\d{2})\s*-\s*(\d{2}(?:\d{2})?-\d{2}-\d{2})$");
    private static readonly Regex ResultUnitPattern = new(@"^Resultatenhet:\s*(.+)$");

    public string? Company { get; set; }
    public string? Title { get; set; }
    public bool IsPreliminary { get; set; }
    public DateOnly? FiscalYearFrom { get; set; }
    public DateOnly? FiscalYearTo { get; set; }
    public DateOnly? PeriodFrom { get; set; }
    public DateOnly? PeriodTo { get; set; }
    public string? ResultUnit { get; set; }
    public DateTime? PrintedAt { get; set; }
    public string? LastVoucher { get; set; }

    public static VismaReportHeader Parse(IReadOnlyList<SheetRow> rows)
    {
        var header = new VismaReportHeader();

        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            ReadLine(header, row);

            for (var column = 0; column < row.Cells.Length; column++)
            {
                var label = row.Text(column);

                if (label == PrintedLabel)
                {
                    // The print time sits in the cell below the date.
                    var below = index + 1 < rows.Count ? rows[index + 1] : null;
                    header.PrintedAt = ReadPrintedAt(row, below, column + 1);
                }
                else if (label == LastVoucherLabel)
                {
                    header.LastVoucher = row.Text(column + 1);
                }
            }
        }

        return header;
    }

    private static void ReadLine(VismaReportHeader header, SheetRow row)
    {
        var text = row.Text(0);
        if (text is null)
        {
            return;
        }

        if (text == PreliminaryText)
        {
            header.IsPreliminary = true;
            return;
        }

        var range = RangePattern.Match(text);
        if (range.Success)
        {
            var from = ParseDate(range.Groups[2].Value);
            var to = ParseDate(range.Groups[3].Value);

            if (range.Groups[1].Value == "Period")
            {
                header.PeriodFrom = from;
                header.PeriodTo = to;
            }
            else
            {
                header.FiscalYearFrom = from;
                header.FiscalYearTo = to;
            }

            return;
        }

        var resultUnit = ResultUnitPattern.Match(text);
        if (resultUnit.Success)
        {
            header.ResultUnit = resultUnit.Groups[1].Value.Trim();
            return;
        }

        // The report starts with the company name, followed by the report title.
        if (header.Company is null)
        {
            header.Company = text;
        }
        else
        {
            header.Title ??= text;
        }
    }

    private static DateTime? ReadPrintedAt(SheetRow row, SheetRow? below, int column)
    {
        var date = row.Value(column) switch
        {
            DateTime value => DateOnly.FromDateTime(value),
            string value => ParseDate(value.Trim()),
            _ => null
        };

        if (date is null)
        {
            return null;
        }

        var time = below?.Value(column) switch
        {
            DateTime value => TimeOnly.FromDateTime(value),
            string value when TimeOnly.TryParseExact(value.Trim(), ["H:mm", "HH:mm"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) => parsed,
            _ => TimeOnly.MinValue
        };

        return date.Value.ToDateTime(time);
    }

    // Visma prints dates as yy-MM-dd; yyyy-MM-dd is accepted too.
    private static DateOnly? ParseDate(string value)
    {
        var fullDate = value.Length == 8 ? $"20{value}" : value;

        return DateOnly.TryParseExact(fullDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;
    }
}
