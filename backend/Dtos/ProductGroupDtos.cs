namespace SeconSbtiBackend.Dtos;

/// <summary>
/// One product group, e.g. "Fläktar". Should be used by the frontend to pick a group for a product.
/// </summary>
public record ProductGroupResponse(
    Guid Id,
    string Name,
    string? Description);