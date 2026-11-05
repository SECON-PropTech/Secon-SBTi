using System.ComponentModel.DataAnnotations;
using SeconSbtiBackend.Validation;

namespace SeconSbtiBackend.DTOs;

public class UpdatePurchasedGoodRequest
{
    [Required(ErrorMessage = "Välj en leverantör.")]
    [NotEmptyGuid(ErrorMessage = "Välj en giltig leverantör.")]
    public Guid? SupplierId { get; init; }

    [Required(ErrorMessage = "Ange ett datum för inköpet.")]
    public DateOnly? TransactionDate { get; init; }

    [Required(ErrorMessage = "Ange ett belopp.")]
    public decimal? Value { get; init; }

    [Required(ErrorMessage = "Ange en enhet.")]
    public string? Unit { get; init; }

    [Required(ErrorMessage = "Ange om värdet är uppmätt eller uppskattat.")]
    public string? MeasurementQuality { get; init; }

    public string? GoodsName { get; init; }
    public string? Comment { get; init; }
    public string? Specification { get; init; }
    public string? AccountNo { get; init; }
}
