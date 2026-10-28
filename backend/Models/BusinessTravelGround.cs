using System;
using System.Collections.Generic;

namespace SeconSbtiBackend.Models;

public partial class BusinessTravelGround
{
    public Guid Id { get; set; }

    /// <summary>
    /// Tåg, Bil, Hotell
    /// </summary>
    public string TravelMode { get; set; } = null!;

    public string? Country { get; set; }

    /// <summary>
    /// Null om ej relevant (t.ex. hotell)
    /// </summary>
    public string? FuelType { get; set; }

    public DateOnly Date { get; set; }

    public decimal Value { get; set; }

    /// <summary>
    /// km, sek, gn, kgCO2e
    /// </summary>
    public string Unit { get; set; } = null!;

    public string MeasurementQuality { get; set; } = null!;

    public string? Comment { get; set; }

    /// <summary>
    /// Den som åkte på resan (Arbetare, FK-AB.1.3)
    /// </summary>
    public Guid? TravellingUserId { get; set; }

    /// <summary>
    /// SEK/mil. Null om ej relevant
    /// </summary>
    public decimal? MileageAllowance { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

    public virtual AppUser CreatedByNavigation { get; set; } = null!;

    public virtual AppUser? TravellingUser { get; set; }
}
