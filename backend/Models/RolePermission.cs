using System;
using System.Collections.Generic;

namespace SeconSbtiBackend.Models;

public partial class RolePermission
{
    public Guid RoleId { get; set; }

    /// <summary>
    /// E.g. import.mobigo
    /// </summary>
    public string Permission { get; set; } = null!;

    public virtual Role Role { get; set; } = null!;
}
