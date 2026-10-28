using System;
using System.Collections.Generic;

namespace SeconSbtiBackend.Models;

public partial class FacilityEnergy
{
    public Guid Id { get; set; }

    public string City { get; set; } = null!;

    public decimal? AreaM2 { get; set; }

    /// <summary>
    /// el eller fjärrvärme
    /// </summary>
    public string EnergyCarrier { get; set; } = null!;

    /// <summary>
    /// Ursprung: Vattenkraft, Ospecifierat, Förnybar ospec
    /// </summary>
    public string? EnergyOrigin { get; set; }

    public int ReportingYear { get; set; }

    /// <summary>
    /// Nollvärde tillåtet (FK-S.01)
    /// </summary>
    public decimal EnergyKwh { get; set; }

    public string MeasurementQuality { get; set; } = null!;

    public string? Comment { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

    public virtual AppUser CreatedByNavigation { get; set; } = null!;
}
