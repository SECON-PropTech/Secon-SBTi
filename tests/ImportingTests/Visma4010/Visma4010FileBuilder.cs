using ClosedXML.Excel;

namespace tests.ImportingTests.Visma4010;

/// <summary>
/// Test helper: builds a Visma 4010 huvudbok as an .xlsx file in memory.
/// Running balance, turnover and closing balance are calculated automatically, so a default
/// file is consistent. Pass explicit values to create reconciliation problems on purpose.
///
/// Default layout (1-based row numbers in the sheet):
///   1-3  header lines (company, title, period)
///   4    column headers
///   5    first account row
///   6    "Ingående saldo:"
///   7... transactions
/// </summary>
internal sealed class Visma4010FileBuilder
{
    // Row numbers with the default header, handy when asserting on issues.
    public const int FirstAccountRow = 5;
    public const int FirstTransactionRow = 7;

    private static readonly string[] DefaultColumnHeaders =
        ["Konto", "Namn", "Vernr", "Datum", "Text", "Debet", "Kredit", "Saldo"];

    private string[] _headerLines = ["Secon AB", "Huvudbok", "Period: 26-01-01 - 26-09-30"];
    private string[] _columnHeaders = DefaultColumnHeaders;
    private readonly List<object?[]> _body = [];
    private decimal _opening;
    private decimal _balance;
    private decimal _debit;
    private decimal _credit;

    /// <summary>
    /// One account (4010) with two invoices and correct totals. Net 4159.60 SEK.
    /// </summary>
    public static Visma4010FileBuilder Standard() =>
        new Visma4010FileBuilder()
            .Account("4010", "Inköp material")
            .Invoice("L5", new DateTime(2026, 1, 1), "Levfkt 260034 IMI Hydronic Engineering AB", debit: 3159.60m)
            .Invoice("L6", new DateTime(2026, 2, 3), "Levfkt 260035 Virken AB", debit: 1000m)
            .Totals();

    public Visma4010FileBuilder WithHeader(params string[] lines)
    {
        _headerLines = lines;
        return this;
    }

    public Visma4010FileBuilder Preliminary()
    {
        _headerLines = [.. _headerLines.Take(2), "Preliminär", .. _headerLines.Skip(2)];
        return this;
    }

    public Visma4010FileBuilder WithColumnHeaders(params string[] headers)
    {
        _columnHeaders = headers;
        return this;
    }

    public Visma4010FileBuilder Account(string number, string name, decimal opening = 0)
    {
        _opening = opening;
        _balance = opening;
        _debit = 0;
        _credit = 0;

        _body.Add(Cells(account: number, name: name));
        _body.Add(Cells(date: "Ingående saldo:", balance: opening));

        return this;
    }

    /// <summary>
    /// A transaction row. If <paramref name="balance"/> is given it is written instead of the calculated
    /// running balance, and later rows continue from it (like the parser does).
    /// </summary>
    public Visma4010FileBuilder Invoice(
        string voucher,
        DateTime date,
        string text,
        decimal debit = 0,
        decimal credit = 0,
        decimal? balance = null)
    {
        _balance += debit - credit;
        _debit += debit;
        _credit += credit;

        var written = balance ?? _balance;
        _balance = written;

        _body.Add(Cells(
            voucher: voucher,
            date: date,
            text: text,
            debit: debit == 0 ? null : debit,
            credit: credit == 0 ? null : credit,
            balance: written));

        return this;
    }

    /// <summary>
    /// The "Transinfo" row Visma prints under a transaction.
    /// </summary>
    public Visma4010FileBuilder TransInfo(string text)
    {
        _body.Add(Cells(date: text));
        return this;
    }

    public Visma4010FileBuilder Turnover(decimal? debit = null, decimal? credit = null)
    {
        _body.Add(Cells(date: "Omslutning:", debit: debit ?? _debit, credit: credit ?? _credit, balance: _balance));
        return this;
    }

    public Visma4010FileBuilder Closing(decimal? balance = null)
    {
        _body.Add(Cells(date: "Utgående saldo:", balance: balance ?? _opening + _debit - _credit));
        return this;
    }

    public Visma4010FileBuilder Totals() => Turnover().Closing();

    /// <summary>
    /// Adds a row exactly as given (8 cells: Konto, Namn, Vernr, Datum, Text, Debet, Kredit, Saldo).
    /// </summary>
    public Visma4010FileBuilder RawRow(params object?[] cells)
    {
        _body.Add(cells);
        return this;
    }

    public byte[] ToBytes()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Huvudbok");
        var rowNumber = 1;

        foreach (var line in _headerLines)
        {
            Write(sheet.Cell(rowNumber++, 1), line);
        }

        for (var column = 0; column < _columnHeaders.Length; column++)
        {
            Write(sheet.Cell(rowNumber, column + 1), _columnHeaders[column]);
        }

        rowNumber++;

        foreach (var cells in _body)
        {
            for (var column = 0; column < cells.Length; column++)
            {
                Write(sheet.Cell(rowNumber, column + 1), cells[column]);
            }

            rowNumber++;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return stream.ToArray();
    }

    /// <summary>
    /// Note: two calls give files with different bytes (the xlsx contains timestamps).
    /// Use <see cref="ToBytes"/> once when a test needs the exact same file twice.
    /// </summary>
    public MemoryStream ToStream() => new(ToBytes());

    private static object?[] Cells(
        object? account = null,
        object? name = null,
        object? voucher = null,
        object? date = null,
        object? text = null,
        object? debit = null,
        object? credit = null,
        object? balance = null) => [account, name, voucher, date, text, debit, credit, balance];

    private static void Write(IXLCell cell, object? value)
    {
        switch (value)
        {
            case null:
                break;
            case string text:
                cell.Value = text;
                break;
            case DateTime date:
                cell.Value = date;
                cell.Style.DateFormat.Format = "yyyy-mm-dd";
                break;
            case decimal number:
                cell.Value = number;
                break;
            default:
                throw new ArgumentException($"Unsupported cell type {value.GetType().Name}.");
        }
    }
}