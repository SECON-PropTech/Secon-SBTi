using System.ComponentModel.DataAnnotations;

namespace SeconSbtiBackend.DTOs;

public sealed class CreatePurchasedServiceRequest : UpdatePurchasedServiceRequest
{
    [Required(ErrorMessage = "Ange ett kontonummer.")]
    public string? AccountNo { get; init; }
}
