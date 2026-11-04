using Microsoft.EntityFrameworkCore;
using SeconSbtiBackend.Data;
using SeconSbtiBackend.DTOs;
using SeconSbtiBackend.Interfaces;

namespace SeconSbtiBackend.Services;

public sealed class PurchasedServicesService(
    ApplicationDbContext context) : IPurchasedServicesService
{
    public async Task<List<PurchasedServiceResponse>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        var query =
            from service in context.PurchasedServices.AsNoTracking()
            join mapping in context.AccountCategoryMappings 
            on service.AccountNo 
            equals mapping.AccountNo 
            into mappings 
            from mapping 
            in mappings.DefaultIfEmpty()
            orderby service.Report.PeriodFrom descending, service.AccountNo, service.ReportId
            select new PurchasedServiceResponse
            {
                ReportId = service.ReportId,
                AccountNo = service.AccountNo,
                ServiceLabel = mapping == null
                    ? null
                    : mapping.ServiceLabel,
                ServiceCategoryId = mapping == null
                    ? null
                    : mapping.ServiceCategoryId,
                ServiceCategoryName = mapping == null
                    ? null
                    : mapping.ServiceCategory.Name,
                PeriodFrom = service.Report.PeriodFrom,
                PeriodTo = service.Report.PeriodTo,
                Value = service.AmountSek,
                Unit = "sek",
                MeasurementQuality = service.MeasurementQuality,
                Comment = service.Comment
            };

        return await query.ToListAsync(cancellationToken);
    }
}