using System;
using System.Collections.Generic;

namespace SeconSbtiBackend.Models;

public partial class ProductGroup
{
    public Guid Id { get; set; }

    /// <summary>
    /// Rapporteringsnivå för 3.11/3.12, t.ex. Fläktar
    /// </summary>
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<SoldProduct> SoldProducts { get; set; } = new List<SoldProduct>();
}
