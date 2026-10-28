using System;
using System.Collections.Generic;

namespace SeconSbtiBackend.Models;

public partial class Attachment
{
    public Guid Id { get; set; }

    public string FileName { get; set; } = null!;

    public string StoragePath { get; set; } = null!;

    public Guid UploadedBy { get; set; }

    public DateTime UploadedAt { get; set; }

    public Guid? PurchasedGoodId { get; set; }

    public Guid? FacilityEnergyId { get; set; }

    public Guid? VehicleUsageId { get; set; }

    public Guid? WasteId { get; set; }

    public Guid? BusinessTravelFlightId { get; set; }

    public Guid? BusinessTravelGroundId { get; set; }

    public virtual BusinessTravelFlight? BusinessTravelFlight { get; set; }

    public virtual BusinessTravelGround? BusinessTravelGround { get; set; }

    public virtual FacilityEnergy? FacilityEnergy { get; set; }

    public virtual PurchasedGood? PurchasedGood { get; set; }

    public virtual AppUser UploadedByNavigation { get; set; } = null!;

    public virtual VehicleUsage? VehicleUsage { get; set; }

    public virtual Waste? Waste { get; set; }
}
