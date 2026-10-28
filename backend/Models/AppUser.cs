using System;
using System.Collections.Generic;

namespace SeconSbtiBackend.Models;

public partial class AppUser
{
    public Guid Id { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string? Email { get; set; }

    /// <summary>
    /// Null means account not activated
    /// </summary>
    public string? PasswordHash { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual ICollection<BusinessTravelFlight> BusinessTravelFlightCreatedByNavigations { get; set; } = new List<BusinessTravelFlight>();

    public virtual ICollection<BusinessTravelFlight> BusinessTravelFlightTravellingUsers { get; set; } = new List<BusinessTravelFlight>();

    public virtual ICollection<BusinessTravelGround> BusinessTravelGroundCreatedByNavigations { get; set; } = new List<BusinessTravelGround>();

    public virtual ICollection<BusinessTravelGround> BusinessTravelGroundTravellingUsers { get; set; } = new List<BusinessTravelGround>();

    public virtual ICollection<FacilityEnergy> FacilityEnergies { get; set; } = new List<FacilityEnergy>();

    public virtual ICollection<ImportBatch> ImportBatches { get; set; } = new List<ImportBatch>();

    public virtual ICollection<PurchasedGood> PurchasedGoods { get; set; } = new List<PurchasedGood>();

    public virtual ICollection<VehicleUsage> VehicleUsages { get; set; } = new List<VehicleUsage>();

    public virtual ICollection<Waste> Wastes { get; set; } = new List<Waste>();

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
}
