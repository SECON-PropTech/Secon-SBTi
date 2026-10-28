using System;
using System.Collections.Generic;

namespace SeconSbtiBackend.Models;

public partial class Material
{
    public Guid Id { get; set; }

    /// <summary>
    /// T.ex. Plast
    /// </summary>
    public string Name { get; set; } = null!;

    public Guid EolTreatmentId { get; set; }

    public virtual WasteTreatment EolTreatment { get; set; } = null!;

    public virtual ICollection<SoldProductMaterial> SoldProductMaterials { get; set; } = new List<SoldProductMaterial>();
}
