using SeconSbtiBackend.Dtos;
using SeconSbtiBackend.Models;

namespace SeconSbtiBackend.Calculations;
/// <summary>
/// Calculations for scope 3.11, use of sold products (FK-S.05).
/// </summary>
public static class Scope311Calculator
{
    /// <summary>
    /// Lifetime energy use in kWh: quantity × kWh/year × lifetime.
    /// Returns null when data is missing, so it is never mistaken for 0 (IF-SG.03).
    /// A product with 0 kWh/year gives 0 even without a lifetime.
    /// 
    /// Static since it is a pure calculation, no state or dependencies needed.
    /// </summary>
    public static decimal? Activity(decimal quantity, decimal? kwhPerYear, decimal? lifetimeYears)
    {
        if (kwhPerYear is null)
        {
            return null;   
        }

        if (kwhPerYear == 0)
        {
            return 0;
        }
        
        if (lifetimeYears is null)
        {
            return null;
        }
        
        return quantity * kwhPerYear.Value * lifetimeYears.Value;
    }

    /// <summary>
    /// What a product lacks before it can be reported: "kwh", "lifetime" and/or "group".
    /// Empty list = complete. Lifetime is not needed when kWh/year is 0.
    /// 
    /// Static since it is a pure calculation, no state or dependencies needed.
    /// </summary>
    public static IReadOnlyList<string> Missing(decimal? kwhPerYear, decimal? lifetimeYears, Guid? groupId)
    {
        var missing = new List<string>();

        if (kwhPerYear is null)
        {
            missing.Add("kwh");
        }
        
        if (lifetimeYears is null && kwhPerYear != 0)
        {
            missing.Add("lifetime");
        }
         
        if (groupId is null)
        {
            missing.Add("group");
        } 

        return missing;
    }

    /// <summary>
    /// Builds the full scope 3.11 report from the products sold during the year.
    /// Only products with a calculated activity count towards totals and shares.
    /// </summary>
    public static Scope311Response Build(int year, IReadOnlyList<(SoldProduct Product, decimal Quantity)> soldProducts)
    {
        var productRows = soldProducts
            .Select(soldProduct => ToProductRow(soldProduct.Product, soldProduct.Quantity))
            .OrderBy(productRow => productRow.Name)
            .ToList();

        var totalActivityKwh = productRows.Sum(productRow => productRow.ActivityKwh ?? 0);

        var groupSummaries = productRows
            .GroupBy(productRow => (productRow.GroupId, productRow.GroupName))
            .Select(rowsInGroup => ToGroupSummary(rowsInGroup.Key.GroupId, rowsInGroup.Key.GroupName, rowsInGroup.ToList(), totalActivityKwh))
            .OrderByDescending(groupSummary => groupSummary.ActivityKwh)
            .ToList();

        return new Scope311Response(year, totalActivityKwh, groupSummaries, productRows);
    }

    /// <summary>
    /// Turns one sold product into a report row with activity and warnings.
    /// </summary>
    private static Scope311ProductRow ToProductRow(SoldProduct product, decimal quantity)
    {
        return new Scope311ProductRow(
            ProductId: product.Id,
            ArticleNo: product.ArticleNo,
            Name: product.Name,
            GroupId: product.GroupId,
            GroupName: product.Group?.Name,
            Quantity: quantity,
            KwhPerYear: product.KwhPerYear,
            LifetimeYears: product.LifetimeYears,
            MeasurementQuality: product.MeasurementQuality,
            Comment: product.Comment,
            ActivityKwh: Activity(quantity, product.KwhPerYear, product.LifetimeYears),
            Missing: Missing(product.KwhPerYear, product.LifetimeYears, product.GroupId));
    }

    /// <summary>
    /// Sums the products in one group and calculates the group's share of the total.
    /// Products without a group end up in "Ingen grupp".
    /// </summary>
    private static Scope311GroupSummary ToGroupSummary(
        Guid? groupId, string? groupName, List<Scope311ProductRow> rowsInGroup, decimal totalActivityKwh)
    {
        var groupActivityKwh = rowsInGroup.Sum(productRow => productRow.ActivityKwh ?? 0);

        return new Scope311GroupSummary(
            GroupId: groupId,
            GroupName: groupName ?? "Ingen grupp",
            ActivityKwh: groupActivityKwh,
            SharePercent: SharePercent(groupActivityKwh, totalActivityKwh),
            ProductCount: rowsInGroup.Count,
            IncompleteCount: rowsInGroup.Count(productRow => productRow.Missing.Count > 0));
    }

    /// <summary>
    /// A part's share of the total in percent, rounded to two decimals.
    /// Returns 0 instead of dividing by zero when no product has activity yet.
    /// </summary>    
    private static decimal SharePercent(decimal partKwh, decimal totalKwh)
    {
        return totalKwh == 0 ? 0 : Math.Round(partKwh / totalKwh * 100, 2);
    }

}