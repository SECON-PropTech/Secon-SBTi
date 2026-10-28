using System;
using System.Collections.Generic;

namespace SeconSbtiBackend.Models;

public partial class ServiceCategory
{
    public Guid Id { get; set; }

    /// <summary>
    /// De 26 tjänstekategorierna, t.ex. Fastighetstjänster
    /// </summary>
    public string Name { get; set; } = null!;

    public virtual ICollection<AccountCategoryMapping> AccountCategoryMappings { get; set; } = new List<AccountCategoryMapping>();
}
