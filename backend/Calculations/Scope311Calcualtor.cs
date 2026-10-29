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
}