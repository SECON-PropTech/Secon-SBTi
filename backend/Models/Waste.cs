using System;
using System.Collections.Generic;

namespace SeconSbtiBackend.Models;

public partial class Waste
{
    public Guid Id { get; set; }

    public Guid WasteTypeId { get; set; }

    public Guid WasteTreatmentId { get; set; }

    public DateOnly Date { get; set; }

    public decimal WeightKg { get; set; }

    public string MeasurementQuality { get; set; } = null!;

    public string? Comment { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

    public virtual AppUser CreatedByNavigation { get; set; } = null!;

    public virtual WasteTreatment WasteTreatment { get; set; } = null!;

    public virtual WasteType WasteType { get; set; } = null!;
}
