using System.ComponentModel.DataAnnotations;
using SeconSbtiBackend.Validation;

namespace SeconSbtiBackend.DTOs;

public sealed class UpdateProductGroupRequest
{
    [Required(ErrorMessage = "Välj en produktgrupp.")]
    [NotEmptyGuid(ErrorMessage = "Välj en giltig produktgrupp.")]
    public Guid? GroupId { get; init; }
}