using System;
using System.Collections.Generic;

namespace SeconSbtiBackend.Models;

public partial class ImportBatch
{
    public Guid Id { get; set; }

    /// <summary>
    /// mobigo, visma_4010, visma_services, okq8, autoplan
    /// </summary>
    public string Source { get; set; } = null!;

    public Guid ImportedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<ImportBatchFile> ImportBatchFiles { get; set; } = new List<ImportBatchFile>();

    public virtual AppUser ImportedByNavigation { get; set; } = null!;

    public virtual ICollection<MobigoMaterialLine> MobigoMaterialLines { get; set; } = new List<MobigoMaterialLine>();

    public virtual ICollection<PurchasedGood> PurchasedGoods { get; set; } = new List<PurchasedGood>();

    public virtual ServiceSpendReport? ServiceSpendReport { get; set; }

    public virtual ICollection<VehicleUsage> VehicleUsages { get; set; } = new List<VehicleUsage>();
}
