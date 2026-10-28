using System;
using System.Collections.Generic;

namespace SeconSbtiBackend.Models;

public partial class VehicleUsage
{
    public Guid Id { get; set; }

    public string? RegistrationNumber { get; set; }

    /// <summary>
    /// El, Diesel, Bensin
    /// </summary>
    public string FuelType { get; set; } = null!;

    /// <summary>
    /// Vid elfordon
    /// </summary>
    public string? EnergyOrigin { get; set; }

    /// <summary>
    /// Leveransdatum / fakturadatum
    /// </summary>
    public DateOnly Date { get; set; }

    public decimal Value { get; set; }

    /// <summary>
    /// liter, kWh
    /// </summary>
    public string Unit { get; set; } = null!;

    public string MeasurementQuality { get; set; } = null!;

    public string? Comment { get; set; }

    public Guid? ImportBatchId { get; set; }

    public string? SourceSystem { get; set; }

    /// <summary>
    /// Fakturanummer
    /// </summary>
    public string? SourceRecordId { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

    public virtual AppUser CreatedByNavigation { get; set; } = null!;

    public virtual ImportBatch? ImportBatch { get; set; }

    public virtual Vehicle? RegistrationNumberNavigation { get; set; }
}
