using System;
using System.Collections.Generic;

namespace SeconSbtiBackend.Models;

public partial class AuditLog
{
    public Guid Id { get; set; }

    public string TableName { get; set; } = null!;

    /// <summary>
    /// Text för att strakställa stöd för både UUID och text-PKs
    /// </summary>
    public string RecordId { get; set; } = null!;

    /// <summary>
    /// insert, update eller delete
    /// </summary>
    public string Action { get; set; } = null!;

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    public Guid? ChangedBy { get; set; }

    public DateTime ChangedAt { get; set; }

    public virtual AppUser? ChangedByNavigation { get; set; }
}
