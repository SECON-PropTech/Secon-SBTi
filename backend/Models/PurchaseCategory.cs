using System;
using System.Collections.Generic;

namespace SeconSbtiBackend.Models;

public partial class PurchaseCategory
{
    public Guid Id { get; set; }

    /// <summary>
    /// Kategorier från PropTech för köpt varor, t.ex. Trä, Elektronik
    /// </summary>
    public string Name { get; set; } = null!;

    public virtual ICollection<SupplierCategoryAllocation> SupplierCategoryAllocations { get; set; } = new List<SupplierCategoryAllocation>();
}
