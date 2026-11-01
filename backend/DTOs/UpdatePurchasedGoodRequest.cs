using System.ComponentModel.DataAnnotations;
using SeconSbtiBackend.Validation;

namespace SeconSbtiBackend.DTOs;

public class UpdatePurchasedGoodRequest
{
    [Required]
    [NotEmptyGuid(ErrorMessage = "Select a valid supplier.")]
    public Guid? SupplierId { get; init; }

    [Required]
    public DateOnly? TransactionDate { get; init; }

    [Required]
    public decimal? Value { get; init; }

    [Required]
    public string? Unit { get; init; }

    [Required]
    public string? MeasurementQuality { get; init; }

    public string? GoodsName { get; init; }
    public string? Comment { get; init; }
    public string? Specification { get; init; }
    public string? AccountNo { get; init; }
}
