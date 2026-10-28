using System;
using System.Collections.Generic;

namespace SeconSbtiBackend.Models;

public partial class SoldProductMaterial
{
    public Guid ProductId { get; set; }

    public Guid MaterialId { get; set; }

    public decimal WeightKg { get; set; }

    public virtual Material Material { get; set; } = null!;

    public virtual SoldProduct Product { get; set; } = null!;
}
