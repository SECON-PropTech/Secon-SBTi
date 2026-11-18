using System.ComponentModel.DataAnnotations;
namespace SeconSbtiBackend.DTOs;

/// <summary>
/// One product group, e.g. "Fläktar". Should be used by the frontend to pick a group for a product.
/// </summary>
public record ProductGroupResponse(
    Guid Id,
    string Name,
    string? Description);


/// <summary>
/// Data for creating or replacing a product group (FK-S.05).
/// </summary>
public class ProductGroupRequest
{
    /// <summary>
    /// Shown as the reporting level in scope 3.11 and 3.12. Must be unique.
    /// </summary>
    [Required(ErrorMessage = "Ange ett namn på produktgruppen.")]
    [StringLength(100, ErrorMessage = "Namnet får vara högst 100 tecken.")]
    public string? Name { get; init; }

    [StringLength(500, ErrorMessage = "Beskrivningen får vara högst 500 tecken.")]
    public string? Description { get; init; }
}