namespace SeconSbtiBackend.Importing.Mobigo;

/// <summary>
/// Which rows of a Mobigo export are saved as delivered products (mobigo_material_line).
/// Version 1, the rules are kept here so they are easy to change when Secon has decided:
/// - Rows without an article number are work, rent, travel etc. and are not saved.
/// - Only finished work orders are saved, the others can still change in Mobigo.
///   They are saved by a later import, once they are finished.
/// - Only rows counted in pieces (st, or no unit) are saved.
/// - Invoiced and not invoiced rows are both saved. Not decided yet, see Fakturerad in the plan.
/// </summary>
public static class MobigoImportRules
{
    private static readonly string[] FinishedStatuses = ["Arkiverat", "Klart"];

    private static readonly string[] PieceUnits = ["st"];

    /// <summary>
    /// Why the row is not saved, as a code from <see cref="MobigoSkipReasons"/>. Null if it is saved.
    /// </summary>
    public static string? SkipReason(ParsedMobigoLine line)
    {
        if (line.ArticleNumber is null)
        {
            return MobigoSkipReasons.WithoutArticleNumber;
        }

        if (line.Status is null || !FinishedStatuses.Contains(line.Status, StringComparer.OrdinalIgnoreCase))
        {
            return MobigoSkipReasons.NotFinished;
        }

        if (line.Unit is not null && !PieceUnits.Contains(line.Unit, StringComparer.OrdinalIgnoreCase))
        {
            return MobigoSkipReasons.OtherUnit;
        }

        return null;
    }
}

public static class MobigoSkipReasons
{
    public const string WithoutArticleNumber = "WithoutArticleNumber";
    public const string NotFinished = "NotFinished";
    public const string OtherUnit = "OtherUnit";
}
