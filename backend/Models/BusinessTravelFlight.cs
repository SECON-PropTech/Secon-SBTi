using System;
using System.Collections.Generic;

namespace SeconSbtiBackend.Models;

public partial class BusinessTravelFlight
{
    public Guid Id { get; set; }

    /// <summary>
    /// FK-BV.05
    /// </summary>
    public string DepartureIataCode { get; set; } = null!;

    public string DestinationIataCode { get; set; } = null!;

    /// <summary>
    /// Behövs för avstämning mot konto 5800
    /// </summary>
    public DateOnly? TravelDate { get; set; }

    public int SingleTripsQuantity { get; set; }

    public string MeasurementQuality { get; set; } = null!;

    public string? Comment { get; set; }

    /// <summary>
    /// Den som åkte på resan (Arbetare, FK-AB.1.3)
    /// </summary>
    public Guid? TravellingUserId { get; set; }

    /// <summary>
    /// Den som loggade resan (Arbetare, FK-AB.1.3)
    /// </summary>
    public Guid CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

    public virtual AppUser CreatedByNavigation { get; set; } = null!;

    public virtual AppUser? TravellingUser { get; set; }
}
