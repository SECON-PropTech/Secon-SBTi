using System;
using System.Collections.Generic;

namespace SeconSbtiBackend.Models;

public partial class SoldProduct
{
    public Guid Id { get; set; }

    public string? ArticleNo { get; set; }

    public string Name { get; set; } = null!;

    public Guid? GroupId { get; set; }

    /// <summary>
    /// Standardantagande. Null = ej satt
    /// </summary>
    public decimal? KwhPerYear { get; set; }

    /// <summary>
    /// Standardantagande. Null = ej satt
    /// </summary>
    public decimal? LifetimeYears { get; set; }

    public virtual ProductGroup? Group { get; set; }

    public virtual ICollection<MobigoMaterialLine> MobigoMaterialLines { get; set; } = new List<MobigoMaterialLine>();

    public virtual ICollection<SoldProductMaterial> SoldProductMaterials { get; set; } = new List<SoldProductMaterial>();
}
