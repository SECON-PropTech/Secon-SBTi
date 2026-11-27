using System.Text.RegularExpressions;
using SeconSbtiBackend.Importing.Visma;

namespace SeconSbtiBackend.Importing.Visma4010;

public static class Visma4010Parser
{
    private const decimal Tolerance = 0.01m;

    private static readonly Regex AccountNumberPattern = new(@"^\d+$");
    private static readonly Regex VoucherPattern = new(@"^[A-Z]+\d+$");
    private static readonly Regex InvoiceTextPattern = new(@"^(Levfkt|Levkreditfkt)\s+(\d+)\s+(.+?)\s*$");
    private static readonly Regex TransInfoPattern = new(@"^LevFktnr:\s*(\d+),\s*Namn:\s*(.+?)\s*$");
    private static readonly Regex Whitespace = new(@"\s+");

    public static ParsedVisma4010File Parse(Stream stream)
    {
        var rows = VismaSheet.ReadRows(stream);
        var headerIndex = rows.FindIndex(row => row.Has("Vernr") && row.Has("Datum"));

        if (headerIndex < 0)
        {
            throw new VismaFileFormatException(
                "Filen saknar kolumnrubrikerna 'Vernr' och 'Datum'. Kontrollera att filen är en huvudbok exporterad från Visma.");
        }

        var header = VismaReportHeader.Parse(rows.GetRange(0, headerIndex));
        var file = new ParsedVisma4010File
        {
            Company = header.Company,
            IsPreliminary = header.IsPreliminary,
            PeriodFrom = header.PeriodFrom,
            PeriodTo = header.PeriodTo
        };

        var columns = LocateColumns(rows.Take(headerIndex + 1));
        ReadAccounts(rows.Skip(headerIndex + 1), columns, file);

        if (file.Accounts.Count == 0)
        {
            throw new VismaFileFormatException("Filen innehåller inget konto.");
        }

        ValidateAccountAndPeriod(file);

        var transactions = file.Accounts.SelectMany(account => account.Transactions).ToList();
        UseMostFrequentSpelling(transactions);

        foreach (var account in file.Accounts)
        {
            ValidateSums(account, file.Issues);
        }

        ValidateUniqueInvoiceNumbers(transactions, file.Issues);

        return file;
    }

    private static void ValidateAccountAndPeriod(ParsedVisma4010File file)
    {
        if (file.Accounts.Any(account => account.Number != "4010"))
        {
            throw new VismaFileFormatException(
                "Filen får endast innehålla konto 4010. Exportera en huvudbok för konto 4010 från Visma.");
        }

        if (file.PeriodFrom is not DateOnly periodFrom ||
            file.PeriodTo is not DateOnly periodTo)
        {
            throw new VismaFileFormatException(
                "Rapportperioden saknas eller är ogiltig. Exportera filen med en angiven period.");
        }

        if (periodFrom > periodTo)
        {
            throw new VismaFileFormatException(
                "Rapportperiodens startdatum måste vara före eller samma dag som slutdatumet.");
        }

        var outsidePeriod = file.Accounts
            .SelectMany(account => account.Transactions)
            .FirstOrDefault(transaction =>
                transaction.Date < periodFrom || transaction.Date > periodTo);

        if (outsidePeriod is not null)
        {
            throw new VismaFileFormatException(
                $"Rad {outsidePeriod.RowNumber} har ett datum utanför rapportperioden. Kontrollera Visma-exporten.");
        }
    }

    private static ColumnMap LocateColumns(IEnumerable<SheetRow> headerRows)
    {
        var columnsByLabel = new Dictionary<string, int>();

        foreach (var row in headerRows)
        {
            for (var column = 0; column < row.Cells.Length; column++)
            {
                if (row.Text(column) is { } label)
                {
                    columnsByLabel[label] = column;
                }
            }
        }

        int Find(string label) => columnsByLabel.TryGetValue(label, out var column)
            ? column
            : throw new VismaFileFormatException($"Kolumnen '{label}' saknas i filen.");

        return new ColumnMap(
            Account: Find("Konto"),
            Name: Find("Namn"),
            Voucher: Find("Vernr"),
            Date: Find("Datum"),
            Text: Find("Text"),
            Debit: Find("Debet"),
            Credit: Find("Kredit"),
            Balance: Find("Saldo"));
    }

    private static void ReadAccounts(IEnumerable<SheetRow> rows, ColumnMap columns, ParsedVisma4010File file)
    {
        ParsedAccount? account = null;
        ParsedTransaction? previousTransaction = null;

        foreach (var row in rows)
        {
            var accountNumber = row.Text(columns.Account);
            if (accountNumber is not null && AccountNumberPattern.IsMatch(accountNumber))
            {
                account = new ParsedAccount
                {
                    RowNumber = row.Number,
                    Number = accountNumber,
                    Name = row.Text(columns.Name) ?? ""
                };
                file.Accounts.Add(account);
                previousTransaction = null;
                continue;
            }

            if (account is null)
            {
                continue;
            }

            var label = row.Text(columns.Date);
            var transaction = ReadTransaction(row, columns, account.Number);

            if (transaction is not null)
            {
                account.Transactions.Add(transaction);
            }
            else if (label == "Ingående saldo:")
            {
                account.OpeningBalance = row.Amount(columns.Balance);
            }
            else if (label == "Omslutning:")
            {
                account.Turnover = ReadSummaryRow(row, columns);
            }
            else if (label == "Utgående saldo:")
            {
                account.Closing = ReadSummaryRow(row, columns);
            }
            else if (previousTransaction is not null && label is not null)
            {
                ApplyTransInfo(previousTransaction, label);
            }

            previousTransaction = transaction;
        }
    }
    private static ParsedTransaction? ReadTransaction(
        SheetRow row,
        ColumnMap columns,
        string accountNumber)
    {
        var voucher = row.Text(columns.Voucher);

        if (voucher is null || !VoucherPattern.IsMatch(voucher))
        {
            return null;
        }

        var date = row.Date(columns.Date)
            ?? throw new VismaFileFormatException(
                $"Rad {row.Number} saknar ett giltigt datum.");

        var debit = row.AmountOrNull(columns.Debit);
        var credit = row.AmountOrNull(columns.Credit);

        if (debit is null && credit is null)
        {
            throw new VismaFileFormatException(
                $"Rad {row.Number} saknar belopp i både Debet och Kredit.");
        }

        var balance = row.AmountOrNull(columns.Balance)
            ?? throw new VismaFileFormatException(
                $"Rad {row.Number} saknar saldo.");

        var text = row.Text(columns.Text) ?? "";

        var transaction = new ParsedTransaction
        {
            RowNumber = row.Number,
            Account = accountNumber,
            Voucher = voucher,
            Date = date,
            Text = text,
            Debit = debit ?? 0m,
            Credit = credit ?? 0m,
            Balance = balance
        };

        // Preferred over the Transinfo row, where supplier names are cut off at 36 characters.
        var match = InvoiceTextPattern.Match(text);

        if (match.Success)
        {
            transaction.InvoiceNumber = match.Groups[2].Value;
            transaction.Supplier = CollapseWhitespace(match.Groups[3].Value);
        }

        return transaction;
    }
    private static void ApplyTransInfo(ParsedTransaction transaction, string transInfo)
    {
        var match = TransInfoPattern.Match(transInfo);
        if (transaction.Supplier is null && match.Success)
        {
            transaction.InvoiceNumber = match.Groups[1].Value;
            transaction.Supplier = CollapseWhitespace(match.Groups[2].Value);
        }
    }

    private static ParsedSummaryRow ReadSummaryRow(SheetRow row, ColumnMap columns) =>
        new(row.Number, row.Amount(columns.Debit), row.Amount(columns.Credit), row.Amount(columns.Balance));

    private static void UseMostFrequentSpelling(IEnumerable<ParsedTransaction> transactions)
    {
        var suppliers = transactions
            .Where(transaction => transaction.Supplier is not null)
            .GroupBy(transaction => transaction.Supplier!, StringComparer.OrdinalIgnoreCase);

        foreach (var supplier in suppliers)
        {
            var spelling = supplier
                .GroupBy(transaction => transaction.Supplier)
                .OrderByDescending(group => group.Count())
                .First().Key;

            foreach (var transaction in supplier)
            {
                transaction.Supplier = spelling;
            }
        }
    }

    private static void ValidateSums(ParsedAccount account, List<ParsedIssue> issues)
    {
        var balance = account.OpeningBalance;

        foreach (var transaction in account.Transactions)
        {
            balance += transaction.Net;

            if (Math.Abs(balance - transaction.Balance) > Tolerance)
            {
                issues.Add(new ParsedIssue(transaction.RowNumber, ParsedIssueCodes.BalanceMismatch));
                // Continue from the file's balance so one wrong row gives one issue, not one per following row.
                balance = transaction.Balance;
            }
        }

        var debit = account.Transactions.Sum(transaction => transaction.Debit);
        var credit = account.Transactions.Sum(transaction => transaction.Credit);

        if (account.Turnover is null)
        {
            issues.Add(new ParsedIssue(account.RowNumber, ParsedIssueCodes.TurnoverMissing));
        }
        else if (Math.Abs(debit - account.Turnover.Debit) > Tolerance || Math.Abs(credit - account.Turnover.Credit) > Tolerance)
        {
            issues.Add(new ParsedIssue(account.Turnover.RowNumber, ParsedIssueCodes.TurnoverMismatch));
        }

        if (account.Closing is null)
        {
            issues.Add(new ParsedIssue(account.RowNumber, ParsedIssueCodes.ClosingBalanceMissing));
        }
        else if (Math.Abs(account.OpeningBalance + debit - credit - account.Closing.Balance) > Tolerance)
        {
            issues.Add(new ParsedIssue(account.Closing.RowNumber, ParsedIssueCodes.ClosingBalanceMismatch));
        }
    }

    private static void ValidateUniqueInvoiceNumbers(IEnumerable<ParsedTransaction> transactions, List<ParsedIssue> issues)
    {
        var seen = new HashSet<string>();

        foreach (var transaction in transactions)
        {
            if (transaction.InvoiceNumber is not null && !seen.Add(transaction.InvoiceNumber))
            {
                issues.Add(new ParsedIssue(transaction.RowNumber, ParsedIssueCodes.DuplicateInvoiceNumber));
            }
        }
    }

    private static string CollapseWhitespace(string value) => Whitespace.Replace(value, " ");

    private record ColumnMap(int Account, int Name, int Voucher, int Date, int Text, int Debit, int Credit, int Balance);
}
