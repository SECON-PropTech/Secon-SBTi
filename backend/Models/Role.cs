using System;
using System.Collections.Generic;

namespace SeconSbtiBackend.Models;

public partial class Role
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

    public virtual ICollection<AppUser> Users { get; set; } = new List<AppUser>();
}
