using System;
using System.Collections.Generic;

namespace SeconSbtiBackend.Models;

public partial class WasteType
{
    public Guid Id { get; set; }

    /// <summary>
    /// Papper och kartong, plast, elektronik etc
    /// </summary>
    public string Name { get; set; } = null!;

    public virtual ICollection<Waste> Wastes { get; set; } = new List<Waste>();
}
