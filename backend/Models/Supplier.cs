using System;
using System.Collections.Generic;

namespace SeconSbtiBackend.Models;

public partial class Supplier
{
    public Guid Id { get; set; }

    /// <summary>
    /// Visma-importen matchar leverantör på namn
    /// </summary>
    public string Name { get; set; } = null!;

    public virtual ICollection<PurchasedGood> PurchasedGoods { get; set; } = new List<PurchasedGood>();

    public virtual ICollection<SupplierCategoryAllocation> SupplierCategoryAllocations { get; set; } = new List<SupplierCategoryAllocation>();
}
