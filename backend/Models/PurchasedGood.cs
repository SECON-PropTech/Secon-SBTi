using System;
using System.Collections.Generic;

namespace SeconSbtiBackend.Models;

public partial class PurchasedGood
{
    public Guid Id { get; set; }

    public Guid SupplierId { get; set; }

    public DateOnly TransactionDate { get; set; }

    /// <summary>
    /// Krediter kan vara negativa
    /// </summary>
    public decimal Value { get; set; }

    /// <summary>
    /// sek, kg, st
    /// </summary>
    public string Unit { get; set; } = null!;

    /// <summary>
    /// measured/estimated
    /// </summary>
    public string MeasurementQuality { get; set; } = null!;

    /// <summary>
    /// Vara, ofta okänd vid Visma-import
    /// </summary>
    public string? GoodsName { get; set; }

    /// <summary>
    /// Kolumn i Atmoz-excel
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// Kolumn i Atmoz-excel
    /// </summary>
    public string? Specification { get; set; }

    /// <summary>
    /// Konto, t.ex. 4010
    /// </summary>
    public string? AccountNo { get; set; }

    /// <summary>
    /// Null = manuellt registrerad
    /// </summary>
    public Guid? ImportBatchId { get; set; }

    /// <summary>
    /// Visma LevFktnr från 4010 exporten
    /// </summary>
    public string? SourceRecordId { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

    public virtual AppUser CreatedByNavigation { get; set; } = null!;

    public virtual ImportBatch? ImportBatch { get; set; }

    public virtual Supplier Supplier { get; set; } = null!;
}
