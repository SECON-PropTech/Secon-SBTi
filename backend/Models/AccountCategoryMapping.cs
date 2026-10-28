using System;
using System.Collections.Generic;

namespace SeconSbtiBackend.Models;

public partial class AccountCategoryMapping
{
    /// <summary>
    /// Visma-konto, t.ex. 5010
    /// </summary>
    public string AccountNo { get; set; } = null!;

    public Guid ServiceCategoryId { get; set; }

    /// <summary>
    /// T.ex. Lokalhyra
    /// </summary>
    public string? ServiceLabel { get; set; }

    public virtual ServiceCategory ServiceCategory { get; set; } = null!;
}
