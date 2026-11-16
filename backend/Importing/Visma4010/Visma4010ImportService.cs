using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SeconSbtiBackend.Data;
using SeconSbtiBackend.Importing.Visma;
using SeconSbtiBackend.Models;

namespace SeconSbtiBackend.Importing.Visma4010;

public sealed class Visma4010ImportService(ApplicationDbContext context) : IVisma4010ImportService
{
    public const long MaxFileSize = 10 * 1024 * 1024;

    private const string Source = "visma_4010";

    private static readonly string[] AllowedExtensions = [".xlsx", ".xls"];
    private static readonly Regex Whitespace = new(@"\s+");

    public async Task<Visma4010ImportResult> PreviewAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken)
    {
        try
        {
            var file = await ReadFileAsync(content, fileName, cancellationToken);
            var analysis = await AnalyzeAsync(file, cancellationToken);

            return new Visma4010ImportResult(Visma4010ImportStatus.Ok, analysis.Response);
        }
        catch (VismaFileFormatException ex)
        {
            return new Visma4010ImportResult(Visma4010ImportStatus.InvalidFile, Message: ex.Message);
        }
    }

    public async Task<Visma4010ImportResult> ImportAsync(
        Stream content,
        string fileName,
        Guid importedBy,
        CancellationToken cancellationToken)
    {
        if (!await context.AppUsers.AnyAsync(user => user.Id == importedBy, cancellationToken))
        {
            return new Visma4010ImportResult(
                Visma4010ImportStatus.InvalidUser,
                Message: "Användarkontot finns inte längre. Kontakta en administratör.");
        }

        UploadedFile file;
        Analysis analysis;

        try
        {
            file = await ReadFileAsync(content, fileName, cancellationToken);
            analysis = await AnalyzeAsync(file, cancellationToken);
        }
        catch (VismaFileFormatException ex)
        {
            return new Visma4010ImportResult(Visma4010ImportStatus.InvalidFile, Message: ex.Message);
        }

        if (!analysis.Response.CanImport)
        {
            return new Visma4010ImportResult(Visma4010ImportStatus.Blocked, analysis.Response);
        }

        var batchId = AddBatch(file, analysis, importedBy);

        try
        {
            // One save, so the batch, the file, the suppliers and the purchased goods are saved together or not at all.
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Another import can have saved the same file or the same invoices between our check and our save.
            // The unique indexes stop it, and a new check then tells the user why.
            context.ChangeTracker.Clear();
            var retry = await AnalyzeAsync(file, cancellationToken);

            if (retry.Response.CanImport)
            {
                throw;
            }

            return new Visma4010ImportResult(Visma4010ImportStatus.Blocked, retry.Response);
        }

        return new Visma4010ImportResult(
            Visma4010ImportStatus.Ok,
            analysis.Response with { ImportBatchId = batchId });
    }

    private static async Task<UploadedFile> ReadFileAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken)
    {
        var name = Path.GetFileName(fileName);

        if (!AllowedExtensions.Contains(Path.GetExtension(name).ToLowerInvariant()))
        {
            throw new VismaFileFormatException("Endast .xlsx- och .xls-filer stöds.");
        }

        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);

        if (buffer.Length == 0)
        {
            throw new VismaFileFormatException("Filen är tom.");
        }

        if (buffer.Length > MaxFileSize)
        {
            throw new VismaFileFormatException("Filen är större än 10 MB.");
        }

        var bytes = buffer.ToArray();

        return new UploadedFile(name, bytes, Convert.ToHexStringLower(SHA256.HashData(bytes)));
    }

    private async Task<Analysis> AnalyzeAsync(UploadedFile file, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(file.Content);
        var parsed = Visma4010Parser.Parse(stream);

        var transactions = parsed.Accounts.SelectMany(account => account.Transactions).ToList();
        var invoices = transactions.Where(transaction => transaction.Supplier is not null).ToList();
        var rowsWithoutSupplier = transactions.Where(transaction => transaction.Supplier is null).ToList();

        var previousImport = await context.ImportBatchFiles
            .AsNoTracking()
            .Where(batchFile => batchFile.FileHash == file.Hash)
            .Select(batchFile => new Visma4010PreviousImportResponse(
                batchFile.ImportBatch.CreatedAt,
                batchFile.ImportBatch.ImportedByNavigation.FirstName + " " + batchFile.ImportBatch.ImportedByNavigation.LastName))
            .FirstOrDefaultAsync(cancellationToken);

        var invoiceNumbers = invoices.Select(invoice => invoice.InvoiceNumber!).Distinct().ToList();
        var savedInvoiceNumbers = (await context.PurchasedGoods
                .AsNoTracking()
                .Where(good => good.SourceRecordId != null && invoiceNumbers.Contains(good.SourceRecordId))
                .Select(good => good.SourceRecordId!)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var rowsToImport = invoices.Where(invoice => !savedInvoiceNumbers.Contains(invoice.InvoiceNumber!)).ToList();
        var rowsAlreadyImported = invoices.Where(invoice => savedInvoiceNumbers.Contains(invoice.InvoiceNumber!)).ToList();

        var supplierIdsByName = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        var suppliers = await context.Suppliers
            .AsNoTracking()
            .OrderBy(supplier => supplier.Name)
            .Select(supplier => new { supplier.Id, supplier.Name })
            .ToListAsync(cancellationToken);

        foreach (var supplier in suppliers)
        {
            supplierIdsByName.TryAdd(NormalizeName(supplier.Name), supplier.Id);
        }

        var newSuppliers = rowsToImport
            .Select(row => row.Supplier!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(name => !supplierIdsByName.ContainsKey(name))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var blockers = new List<string>();

        if (previousImport is not null)
        {
            blockers.Add(Visma4010Blockers.FileAlreadyImported);
        }

        if (parsed.Issues.Any(issue => issue.Code != ParsedIssueCodes.DuplicateInvoiceNumber))
        {
            blockers.Add(Visma4010Blockers.ReconciliationFailed);
        }

        if (parsed.Issues.Any(issue => issue.Code == ParsedIssueCodes.DuplicateInvoiceNumber))
        {
            blockers.Add(Visma4010Blockers.DuplicateInvoiceNumber);
        }

        // Not added for a file that has been imported before, FileAlreadyImported already says why.
        if (rowsToImport.Count == 0 && previousImport is null)
        {
            blockers.Add(Visma4010Blockers.NothingToImport);
        }

        var warnings = new List<string>();

        if (parsed.IsPreliminary)
        {
            warnings.Add(Visma4010Warnings.PreliminaryReport);
        }

        if (rowsWithoutSupplier.Count > 0)
        {
            warnings.Add(Visma4010Warnings.RowsWithoutSupplier);
        }

        var response = new Visma4010ImportResponse
        {
            FileName = file.Name,
            Company = parsed.Company,
            PeriodFrom = parsed.PeriodFrom,
            PeriodTo = parsed.PeriodTo,
            IsPreliminary = parsed.IsPreliminary,
            Accounts = parsed.Accounts
                .Select(account => new Visma4010AccountResponse(account.Number, account.Name))
                .ToList(),
            CanImport = blockers.Count == 0,
            Blockers = blockers,
            Warnings = warnings,
            Issues = parsed.Issues
                .Select(issue => new Visma4010IssueResponse(issue.RowNumber, issue.Code))
                .ToList(),
            PreviousImport = previousImport,
            ToImport = Summarize(rowsToImport),
            AlreadyImported = Summarize(rowsAlreadyImported),
            WithoutSupplier = Summarize(rowsWithoutSupplier),
            NewSuppliers = newSuppliers,
            RowsWithoutSupplier = rowsWithoutSupplier
                .Select(row => new Visma4010RowResponse(row.RowNumber, row.Account, row.Voucher, row.Date, row.Text, row.Net))
                .ToList()
        };

        return new Analysis(response, rowsToImport, supplierIdsByName);
    }

    private Guid AddBatch(UploadedFile file, Analysis analysis, Guid importedBy)
    {
        var now = DateTime.UtcNow;

        var batch = new ImportBatch
        {
            Id = Guid.NewGuid(),
            Source = Source,
            ImportedBy = importedBy,
            CreatedAt = now
        };

        batch.ImportBatchFiles.Add(new ImportBatchFile
        {
            Id = Guid.NewGuid(),
            Name = file.Name,
            FileHash = file.Hash
        });

        context.ImportBatches.Add(batch);

        var supplierIdsByName = analysis.SupplierIdsByName;

        foreach (var name in analysis.Response.NewSuppliers)
        {
            var supplier = new Supplier { Id = Guid.NewGuid(), Name = name };
            context.Suppliers.Add(supplier);
            supplierIdsByName[name] = supplier.Id;
        }

        foreach (var row in analysis.RowsToImport)
        {
            context.PurchasedGoods.Add(new PurchasedGood
            {
                Id = Guid.NewGuid(),
                SupplierId = supplierIdsByName[row.Supplier!],
                TransactionDate = row.Date,
                Value = row.Net,
                Unit = "sek",
                MeasurementQuality = "measured",
                AccountNo = row.Account,
                ImportBatchId = batch.Id,
                SourceRecordId = row.InvoiceNumber,
                CreatedBy = importedBy,
                CreatedAt = now
            });
        }

        return batch.Id;
    }

    private static Visma4010RowsSummary Summarize(List<ParsedTransaction> rows) =>
        new(rows.Count, rows.Sum(row => row.Net));

    private static string NormalizeName(string name) => Whitespace.Replace(name, " ").Trim();

    private sealed record UploadedFile(string Name, byte[] Content, string Hash);

    private sealed record Analysis(
        Visma4010ImportResponse Response,
        List<ParsedTransaction> RowsToImport,
        Dictionary<string, Guid> SupplierIdsByName);
}
