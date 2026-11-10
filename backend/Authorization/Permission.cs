using System.Text.Json.Serialization;

namespace SeconSbtiBackend.Authorization;

/// <summary>
/// All availalbe permissions in the system.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<Permission>))]
public enum Permission
{
    /// <summary>Import supplier invoices from the Visma 4010 export.</summary>
    ImportVisma4010,

    /// <summary>Import material lines from Mobigo.</summary>
    ImportMobigo,

    /// <summary>Create, edit, delete roles and choose which permissions they give. Assign roles to users.</summary>
    ManageRoles,
}
