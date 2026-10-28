using System;
using System.Collections.Generic;

namespace SeconSbtiBackend.Models;

public partial class MobigoMaterialLine
{
    public Guid Id { get; set; }

    public Guid ImportBatchId { get; set; }

    public Guid ProductId { get; set; }

    /// <summary>
    /// Uppdragsnummer
    /// </summary>
    public string MobigoWorkOrderId { get; set; } = null!;

    public decimal Quantity { get; set; }

    /// <summary>
    /// Leveransdatum, avgör rapporteringsår
    /// </summary>
    public DateOnly Date { get; set; }

    public virtual ImportBatch ImportBatch { get; set; } = null!;

    public virtual SoldProduct Product { get; set; } = null!;
}
