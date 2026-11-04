using SeconSbtiBackend.DTOs;

namespace SeconSbtiBackend.Interfaces;

public interface IPurchasedServicesService
{
    Task<List<PurchasedServiceResponse>> GetAllAsync(
        CancellationToken cancellationToken);
}