using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SeconSbtiBackend.Data;
using SeconSbtiBackend.Importing.Visma;
using SeconSbtiBackend.Models;

namespace SeconSbtiBackend.Importing.Mobigo;

/// <summary>
/// Imports delivered products from a Mobigo export into mobigo_material_line, for scope 3.11 and 3.12.
/// Exports overlap (the same row can be in several files), so a file is compared row by row with what is already saved,
/// not only by its hash.
/// </summary>
public sealed class MobigoImportService(ApplicationDbContext context) : IMobigoImportService
{
    public const long MaxFileSize = 10 * 1024 * 1024;

    private const string Source = "mobigo";

    private static readonly string[] AllowedExtensions = [".xlsx", ".xls"];
    private static readonly Regex Whitespace = new(@"\s+");

    public async Task<MobigoImportResult> PreviewAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken)
    {
        try
        {
            var file = await ReadFileAsync(content, fileName, cancellationToken);
            var analysis = await AnalyzeAsync(file, cancellationToken);

            return new MobigoImportResult(MobigoImportStatus.Ok, analysis.Response);
        }
        catch (VismaFileFormatException ex)
        {
            return new MobigoImportResult(MobigoImportStatus.InvalidFile, Message: ex.Message);
        }
    }

    public async Task<MobigoImportResult> ImportAsync(
        Stream content,
        string fileName,
        Guid importedBy,
        CancellationToken cancellationToken)
    {
        if (!await context.AppUsers.AnyAsync(user => user.Id == importedBy, cancellationToken))
        {
            return new MobigoImportResult(
                MobigoImportStatus.InvalidUser,
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
            return new MobigoImportResult(MobigoImportStatus.InvalidFile, Message: ex.Message);
        }

        if (!analysis.Response.CanImport)
        {
            return new MobigoImportResult(MobigoImportStatus.Blocked, analysis.Response);
        }

        var batchId = AddBatch(file, analysis, importedBy);

        try
        {
            // One save, so the batch, the file, the new products and the lines are saved together or not at all.
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Another import can have saved the same file or created the same product between our check and our save.
            // The unique indexes stop it, and a new check then tells the user why.
            context.ChangeTracker.Clear();
            var retry = await AnalyzeAsync(file, cancellationToken);

            if (retry.Response.CanImport)
            {
                throw;
            }

            return new MobigoImportResult(MobigoImportStatus.Blocked, retry.Response);
        }

        return new MobigoImportResult(
            MobigoImportStatus.Ok,
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
        var parsed = MobigoParser.Parse(stream);

        var skippedRows = new List<MobigoSkippedRowResponse>();
        var rowIssues = new List<MobigoIssueResponse>();
        var rowsToSave = new List<ParsedMobigoLine>();

        foreach (var row in parsed.Lines)
        {
            if (MobigoImportRules.SkipReason(row) is { } reason)
            {
                skippedRows.Add(ToSkippedRow(row, reason));
            }
            else if (FindIssue(row) is { } issue)
            {
                rowIssues.Add(new MobigoIssueResponse(row.RowNumber, issue));
            }
            else
            {
                rowsToSave.Add(row);
            }
        }

        // Rows with the same work order, article and date are saved as one line, e.g. two rows of water samples
        // sent the same day. Then a line can be recognised in a later export, which has no row ids.
        var lines = rowsToSave
            .GroupBy(row => new LineKey(row.WorkOrder, row.ArticleNumber!, row.DeliveryDate!.Value))
            .Select(group => new FileLine(
                group.Key,
                group.Sum(row => row.Quantity!.Value),
                group.Select(row => row.RowNumber).ToList()))
            .OrderBy(line => line.RowNumbers[0])
            .ToList();

        var articleNumbers = lines.Select(line => line.Key.ArticleNumber).Distinct().ToList();
        var productsByArticle = await context.SoldProducts
            .AsNoTracking()
            .Where(product => product.ArticleNo != null && articleNumbers.Contains(product.ArticleNo))
            .ToDictionaryAsync(product => product.ArticleNo!, product => (product.Id, product.Name), cancellationToken);

        var savedQuantities = await GetSavedQuantitiesAsync(lines, productsByArticle, cancellationToken);

        var linesToImport = new List<FileLine>();
        var linesAlreadyImported = new List<FileLine>();
        var changedLines = new List<(FileLine Line, decimal SavedQuantity)>();

        foreach (var line in lines)
        {
            if (productsByArticle.TryGetValue(line.Key.ArticleNumber, out var product)
                && savedQuantities.TryGetValue((line.Key.WorkOrder, product.Id, line.Key.Date), out var savedQuantity))
            {
                if (savedQuantity == line.Quantity)
                {
                    linesAlreadyImported.Add(line);
                }
                else
                {
                    changedLines.Add((line, savedQuantity));
                }
            }
            else
            {
                linesToImport.Add(line);
            }
        }

        var namesByArticle = MostFrequentNames(parsed.Lines);

        var newProducts = linesToImport
            .Select(line => line.Key.ArticleNumber)
            .Distinct()
            .Where(articleNumber => !productsByArticle.ContainsKey(articleNumber))
            .Order(StringComparer.Ordinal)
            .Select(articleNumber => new MobigoNewProductResponse(
                articleNumber,
                namesByArticle.GetValueOrDefault(articleNumber) ?? articleNumber))
            .ToList();

        string ProductName(string articleNumber) =>
            productsByArticle.TryGetValue(articleNumber, out var product)
                ? product.Name
                : namesByArticle.GetValueOrDefault(articleNumber) ?? articleNumber;

        var previousImport = await context.ImportBatchFiles
            .AsNoTracking()
            .Where(batchFile => batchFile.FileHash == file.Hash)
            .Select(batchFile => new MobigoPreviousImportResponse(
                batchFile.ImportBatch.CreatedAt,
                batchFile.ImportBatch.ImportedByNavigation.FirstName + " " + batchFile.ImportBatch.ImportedByNavigation.LastName))
            .FirstOrDefaultAsync(cancellationToken);

        var blockers = new List<string>();

        if (previousImport is not null)
        {
            blockers.Add(MobigoBlockers.FileAlreadyImported);
        }

        if (parsed.Issues.Count > 0)
        {
            blockers.Add(MobigoBlockers.ReconciliationFailed);
        }

        if (rowIssues.Count > 0)
        {
            blockers.Add(MobigoBlockers.InvalidRows);
        }

        // Not added for a file that has been imported before, FileAlreadyImported already says why.
        if (linesToImport.Count == 0 && previousImport is null)
        {
            blockers.Add(MobigoBlockers.NothingToImport);
        }

        var warnings = new List<string>();

        if (skippedRows.Count > 0)
        {
            warnings.Add(MobigoWarnings.RowsSkipped);
        }

        if (changedLines.Count > 0)
        {
            warnings.Add(MobigoWarnings.ChangedSinceImport);
        }

        var response = new MobigoImportResponse
        {
            FileName = file.Name,
            DeliveryDateFrom = linesToImport.Count > 0 ? linesToImport.Min(line => line.Key.Date) : null,
            DeliveryDateTo = linesToImport.Count > 0 ? linesToImport.Max(line => line.Key.Date) : null,
            Total = parsed.Lines.Sum(row => row.Amount ?? 0),
            FileTotal = parsed.Total,
            CanImport = blockers.Count == 0,
            Blockers = blockers,
            Warnings = warnings,
            Issues = parsed.Issues
                .Select(issue => new MobigoIssueResponse(issue.RowNumber, issue.Code))
                .Concat(rowIssues)
                .OrderBy(issue => issue.RowNumber)
                .ToList(),
            PreviousImport = previousImport,
            ToImport = Summarize(linesToImport),
            AlreadyImported = Summarize(linesAlreadyImported),
            LinesToImport = linesToImport
                .Select(line => new MobigoLineResponse(
                    line.Key.WorkOrder,
                    line.Key.ArticleNumber,
                    ProductName(line.Key.ArticleNumber),
                    !productsByArticle.ContainsKey(line.Key.ArticleNumber),
                    line.Key.Date,
                    line.Quantity,
                    line.RowNumbers))
                .ToList(),
            ChangedLines = changedLines
                .Select(changed => new MobigoChangedLineResponse(
                    changed.Line.Key.WorkOrder,
                    changed.Line.Key.ArticleNumber,
                    ProductName(changed.Line.Key.ArticleNumber),
                    changed.Line.Key.Date,
                    changed.Line.Quantity,
                    changed.SavedQuantity,
                    changed.Line.RowNumbers))
                .ToList(),
            NewProducts = newProducts,
            SkippedRows = skippedRows
        };

        var productIdsByArticle = productsByArticle.ToDictionary(pair => pair.Key, pair => pair.Value.Id);

        return new Analysis(response, linesToImport, productIdsByArticle);
    }

    /// <summary>
    /// Quantity already saved per work order, product and date, for the lines in the file.
    /// Manual lines count too if they have a work order, so a delivery is not counted twice.
    /// </summary>
    private async Task<Dictionary<(string WorkOrder, Guid ProductId, DateOnly Date), decimal>> GetSavedQuantitiesAsync(
        List<FileLine> lines,
        Dictionary<string, (Guid Id, string Name)> productsByArticle,
        CancellationToken cancellationToken)
    {
        var workOrders = lines.Select(line => line.Key.WorkOrder).Distinct().ToList();
        var productIds = productsByArticle.Values.Select(product => product.Id).ToList();

        if (workOrders.Count == 0 || productIds.Count == 0)
        {
            return [];
        }

        var savedLines = await context.MobigoMaterialLines
            .AsNoTracking()
            .Where(line => line.MobigoWorkOrderId != null
                && workOrders.Contains(line.MobigoWorkOrderId)
                && productIds.Contains(line.ProductId))
            .Select(line => new { WorkOrder = line.MobigoWorkOrderId!, line.ProductId, line.Date, line.Quantity })
            .ToListAsync(cancellationToken);

        return savedLines
            .GroupBy(line => (line.WorkOrder, line.ProductId, line.Date))
            .ToDictionary(group => group.Key, group => group.Sum(line => line.Quantity));
    }

    private Guid AddBatch(UploadedFile file, Analysis analysis, Guid importedBy)
    {
        var batch = new ImportBatch
        {
            Id = Guid.NewGuid(),
            Source = Source,
            ImportedBy = importedBy,
            CreatedAt = DateTime.UtcNow
        };

        batch.ImportBatchFiles.Add(new ImportBatchFile
        {
            Id = Guid.NewGuid(),
            Name = file.Name,
            FileHash = file.Hash
        });

        context.ImportBatches.Add(batch);

        var productIdsByArticle = analysis.ProductIdsByArticle;

        // Products only get article number and name. Group, kWh and lifetime are filled in by the user in 3.11/3.12.
        // They are not linked to the import, so they are kept if the import is reverted, like suppliers from Visma.
        foreach (var newProduct in analysis.Response.NewProducts)
        {
            var product = new SoldProduct
            {
                Id = Guid.NewGuid(),
                ArticleNo = newProduct.ArticleNumber,
                Name = newProduct.Name,
                MeasurementQuality = "estimated"
            };

            context.SoldProducts.Add(product);
            productIdsByArticle[newProduct.ArticleNumber] = product.Id;
        }

        foreach (var line in analysis.LinesToImport)
        {
            context.MobigoMaterialLines.Add(new MobigoMaterialLine
            {
                Id = Guid.NewGuid(),
                ImportBatchId = batch.Id,
                ProductId = productIdsByArticle[line.Key.ArticleNumber],
                MobigoWorkOrderId = line.Key.WorkOrder,
                Quantity = line.Quantity,
                Date = line.Key.Date,
                MeasurementQuality = "measured"
            });
        }

        return batch.Id;
    }

    /// <summary>
    /// Why a row that passed the rules can't be saved, as a code from <see cref="MobigoIssueCodes"/>. Null if it can.
    /// </summary>
    private static string? FindIssue(ParsedMobigoLine row) => row switch
    {
        { WorkOrder: "" } => MobigoIssueCodes.WorkOrderMissing,
        { DeliveryDate: null } => MobigoIssueCodes.DeliveryDateMissing,
        { Quantity: null } => MobigoIssueCodes.QuantityMissing,
        { Quantity: < 0 } => MobigoIssueCodes.QuantityNegative,
        _ => null
    };

    /// <summary>
    /// The same article can have several names, e.g. "Vattenprov Legionella 250815". The most frequent one is used
    /// for a new product, and the first one in the file if there is a tie.
    /// </summary>
    private static Dictionary<string, string> MostFrequentNames(IEnumerable<ParsedMobigoLine> rows) =>
        rows
            .Where(row => row.ArticleNumber is not null && row.Name is not null)
            .GroupBy(row => row.ArticleNumber!)
            .ToDictionary(
                article => article.Key,
                article => article
                    .GroupBy(row => Whitespace.Replace(row.Name!, " ").Trim())
                    .OrderByDescending(name => name.Count())
                    .ThenBy(name => name.Min(row => row.RowNumber))
                    .First().Key);

    private static MobigoSkippedRowResponse ToSkippedRow(ParsedMobigoLine row, string reason) =>
        new(row.RowNumber, reason, row.WorkOrder, row.Status, row.ArticleNumber, row.Name, row.DeliveryDate, row.Quantity, row.Unit);

    private static MobigoLinesSummary Summarize(List<FileLine> lines) =>
        new(lines.Count, lines.Sum(line => line.RowNumbers.Count), lines.Sum(line => line.Quantity));

    private sealed record UploadedFile(string Name, byte[] Content, string Hash);

    private readonly record struct LineKey(string WorkOrder, string ArticleNumber, DateOnly Date);

    private sealed record FileLine(LineKey Key, decimal Quantity, List<int> RowNumbers);

    private sealed record Analysis(
        MobigoImportResponse Response,
        List<FileLine> LinesToImport,
        Dictionary<string, Guid> ProductIdsByArticle);
}
