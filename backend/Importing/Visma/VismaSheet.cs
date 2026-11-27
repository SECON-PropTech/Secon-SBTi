using System.Globalization;
using ExcelDataReader;
using ExcelDataReader.Exceptions;
using System.Text;

namespace SeconSbtiBackend.Importing.Visma;

public static class VismaSheet
{
    public static List<SheetRow> ReadRows(Stream stream)
    {
        try
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            using var reader = ExcelReaderFactory.CreateReader(stream);
            var rows = new List<SheetRow>();

            while (reader.Read())
            {
                var cells = new object?[reader.FieldCount];
                for (var column = 0; column < cells.Length; column++)
                {
                    cells[column] = reader.GetValue(column);
                }

                rows.Add(new SheetRow(rows.Count + 1, cells));
            }

            return rows;
        }
        catch (Exception ex) when (ex is ExcelReaderException or InvalidDataException)
        {
            throw new VismaFileFormatException("Filen kunde inte läsas som en Excel-fil.");
        }
    }
}

public record SheetRow(int Number, object?[] Cells)
{
    public bool Has(string label) => Cells.Any(cell => cell is string text && text.Trim() == label);

    public string? Text(int column)
    {
        var text = Convert.ToString(Value(column), CultureInfo.InvariantCulture)?.Trim();
        return string.IsNullOrEmpty(text) ? null : text;
    }

    public DateOnly? Date(int column) =>
        Value(column) is DateTime date ? DateOnly.FromDateTime(date) : null;

    public decimal Amount(int column) => AmountOrNull(column) ?? 0;

    public decimal? AmountOrNull(int column)
    {
        var value = Value(column);
        if (value is null || value is string text && string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        try
        {
            // Amounts are floats with noise, e.g. -7973972.050000001.
            return Math.Round(Convert.ToDecimal(value, CultureInfo.InvariantCulture), 2, MidpointRounding.AwayFromZero);
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        {
            throw new VismaFileFormatException($"Rad {Number} har ett belopp som inte är ett tal.");
        }
    }

    //TODO: Handle negative column indexes (e.g. column = -1) by returning null or throwing an exception.
    public object? Value(int column) => column < Cells.Length ? Cells[column] : null;
}
