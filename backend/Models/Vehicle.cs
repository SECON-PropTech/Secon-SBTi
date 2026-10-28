using System;
using System.Collections.Generic;

namespace SeconSbtiBackend.Models;

public partial class Vehicle
{
    public string RegistrationNumber { get; set; } = null!;

    /// <summary>
    /// Bil, Skåpbil
    /// </summary>
    public string? VehicleType { get; set; }

    public string? FuelType { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<VehicleUsage> VehicleUsages { get; set; } = new List<VehicleUsage>();
}
