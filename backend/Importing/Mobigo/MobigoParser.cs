using SeconSbtiBackend.Importing.Visma;

namespace SeconSbtiBackend.Importing.Mobigo;

/// <summary>
/// Reads the list of work order rows ("uppdragsrader") exported from Mobigo as Excel.
/// The sheet starts with a header row and ends with a SUMMA: row.
/// </summary>
public static class MobigoParser
{
    private const decimal Tolerance = 0.01m;
    private const string TotalLabel = "SUMMA:";

    public static ParsedMobigoFile Parse(Stream stream)
    {
        // VismaSheet is not Visma specific, it reads any Excel sheet.
        var rows = VismaSheet.ReadRows(stream);
        var headerIndex = rows.FindIndex(row => row.Has("Uppdrag") && row.Has("Summa"));

        if (headerIndex < 0)
        {
            throw new VismaFileFormatException(
                "Filen saknar kolumnrubrikerna 'Uppdrag' och 'Summa'. Kontrollera att filen är en lista med uppdragsrader exporterad från Mobigo.");
        }

        var columns = LocateColumns(rows[headerIndex]);
        var file = new ParsedMobigoFile();

        foreach (var row in rows.Skip(headerIndex + 1))
        {
            if (IsBlank(row))
            {
                continue;
            }

            if (row.Text(columns.WorkOrder) == TotalLabel)
            {
                // The total is printed in the cell after the label, not in the Summa column.
                file.Total = row.AmountOrNull(columns.WorkOrder + 1);
                file.TotalRowNumber = row.Number;
                break;
            }

            file.Lines.Add(ReadLine(row, columns));
        }

        if (file.Lines.Count == 0)
        {
            throw new VismaFileFormatException("Filen innehåller inga uppdragsrader.");
        }

        ValidateAmounts(file);
        ValidateTotal(file, rows.Count);

        return file;
    }

    private static ColumnMap LocateColumns(SheetRow headerRow)
    {
        var columnsByLabel = new Dictionary<string, int>();

        for (var column = 0; column < headerRow.Cells.Length; column++)
        {
            if (headerRow.Text(column) is { } label)
            {
                columnsByLabel[label] = column;
            }
        }

        int Find(string label) => columnsByLabel.TryGetValue(label, out var column)
            ? column
            : throw new VismaFileFormatException($"Kolumnen '{label}' saknas i filen.");

        var unitPrice = Find("Á-pris");
        var amount = Find("Summa");

        return new ColumnMap(
            WorkOrder: Find("Uppdrag"),
            Status: Find("Task"),
            Customer: Find("Kund"),
            ArticleNumber: Find("Nr"),
            Name: Find("Namn"),
            DeliveryDate: Find("Leveransdatum"),
            Quantity: Find("Antal"),
            Unit: Find("Enhet"),
            Invoiced: Find("Fakturerad"),
            UnitPrice: unitPrice,
            // The discount column has no label and sits between Á-pris and Summa.
            Discount: unitPrice + 1 < amount ? unitPrice + 1 : null,
            Amount: amount);
    }

    private static ParsedMobigoLine ReadLine(SheetRow row, ColumnMap columns) =>
        new()
        {
            RowNumber = row.Number,
            WorkOrder = row.Text(columns.WorkOrder) ?? "",
            Status = row.Text(columns.Status),
            Customer = row.Text(columns.Customer),
            // Mostly a number cell (700002115), sometimes text (SAS61.03). Text() gives both as text.
            ArticleNumber = row.Text(columns.ArticleNumber),
            Name = row.Text(columns.Name),
            DeliveryDate = row.Date(columns.DeliveryDate),
            Quantity = row.AmountOrNull(columns.Quantity),
            Unit = row.Text(columns.Unit),
            Invoiced = row.Text(columns.Invoiced) == "Ja",
            UnitPrice = row.AmountOrNull(columns.UnitPrice),
            // Not read as an amount, which would round it to two decimals.
            Discount = columns.Discount is { } discount && row.Value(discount) is double fraction ? (decimal)fraction : null,
            Amount = row.AmountOrNull(columns.Amount)
        };

    /// <summary>
    /// Antal × Á-pris less the discount should be Summa on every priced row.
    /// A difference means that the columns have been read wrong or that the file has been edited.
    /// </summary>
    private static void ValidateAmounts(ParsedMobigoFile file)
    {
        foreach (var line in file.Lines)
        {
            if (line is not { Quantity: { } quantity, UnitPrice: { } unitPrice, Amount: { } amount })
            {
                continue;
            }

            var expected = Math.Round(quantity * unitPrice * (1 + (line.Discount ?? 0)), 2, MidpointRounding.AwayFromZero);

            if (Math.Abs(expected - amount) > Tolerance)
            {
                file.Issues.Add(new ParsedMobigoIssue(line.RowNumber, ParsedMobigoIssueCodes.AmountMismatch));
            }
        }
    }

    private static void ValidateTotal(ParsedMobigoFile file, int lastRowNumber)
    {
        if (file.Total is not { } total)
        {
            file.Issues.Add(new ParsedMobigoIssue(file.TotalRowNumber ?? lastRowNumber, ParsedMobigoIssueCodes.TotalMissing));
            return;
        }

        var sum = file.Lines.Sum(line => line.Amount ?? 0);

        if (Math.Abs(sum - total) > Tolerance)
        {
            file.Issues.Add(new ParsedMobigoIssue(file.TotalRowNumber!.Value, ParsedMobigoIssueCodes.TotalMismatch));
        }
    }

    private static bool IsBlank(SheetRow row) =>
        row.Cells.All(cell => cell is null || cell is string text && string.IsNullOrWhiteSpace(text));

    private record ColumnMap(
        int WorkOrder,
        int Status,
        int Customer,
        int ArticleNumber,
        int Name,
        int DeliveryDate,
        int Quantity,
        int Unit,
        int Invoiced,
        int UnitPrice,
        int? Discount,
        int Amount);
}
