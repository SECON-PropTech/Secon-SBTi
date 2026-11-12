using SeconSbtiBackend.DTOs;

namespace SeconSbtiBackend.Interfaces;

public interface IPurchasedServicesService
{
    Task<List<PurchasedServiceResponse>> GetAllAsync(PurchasedServicesQuery filter, CancellationToken cancellationToken);

    Task<PurchasedServiceResponse?> CreateAsync(
        CreatePurchasedServiceRequest request,
        CancellationToken cancellationToken);

    Task<PurchasedServiceUpdateResult> UpdateAsync(
        Guid reportId,
        string accountNo,
        UpdatePurchasedServiceRequest request,
        CancellationToken cancellationToken);
}
