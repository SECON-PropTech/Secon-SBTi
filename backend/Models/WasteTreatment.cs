using System;
using System.Collections.Generic;

namespace SeconSbtiBackend.Models;

public partial class WasteTreatment
{
    public Guid Id { get; set; }

    /// <summary>
    /// Materialåtervinning, deponi etc
    /// </summary>
    public string Name { get; set; } = null!;

    public virtual ICollection<Material> Materials { get; set; } = new List<Material>();

    public virtual ICollection<Waste> Wastes { get; set; } = new List<Waste>();
}
