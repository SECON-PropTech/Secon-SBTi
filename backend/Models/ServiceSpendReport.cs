using System;
using System.Collections.Generic;

namespace SeconSbtiBackend.Models;

public partial class ServiceSpendReport
{
    public Guid Id { get; set; }

    public Guid? ImportBatchId { get; set; }

    /// <summary>
    /// Från rapportens Period-rad
    /// </summary>
    public DateOnly PeriodFrom { get; set; }

    public DateOnly PeriodTo { get; set; }

    public virtual ImportBatch? ImportBatch { get; set; }

    public virtual ICollection<PurchasedService> PurchasedServices { get; set; } = new List<PurchasedService>();
}
