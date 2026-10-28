using System;
using System.Collections.Generic;

namespace SeconSbtiBackend.Models;

public partial class SupplierCategoryAllocation
{
    public Guid SupplierId { get; set; }

    public Guid PurchaseCategoryId { get; set; }

    /// <summary>
    /// 0.5 = 50 procent. Summa per leverantör = 100% (FK-DI.05, kontrolleras i backend/deferred trigger)
    /// </summary>
    public decimal Share { get; set; }

    public virtual PurchaseCategory PurchaseCategory { get; set; } = null!;

    public virtual Supplier Supplier { get; set; } = null!;
}
