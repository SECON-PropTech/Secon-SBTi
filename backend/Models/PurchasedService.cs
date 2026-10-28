using System;
using System.Collections.Generic;

namespace SeconSbtiBackend.Models;

public partial class PurchasedService
{
    public Guid ReportId { get; set; }

    /// <summary>
    /// Konto så som 5010
    /// </summary>
    public string AccountNo { get; set; } = null!;

    /// <summary>
    /// Kostnad som positivt belopp (rapportens tecken vänt)
    /// </summary>
    public decimal AmountSek { get; set; }

    public virtual ServiceSpendReport Report { get; set; } = null!;
}
