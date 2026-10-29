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
}