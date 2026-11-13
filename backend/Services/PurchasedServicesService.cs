using Microsoft.EntityFrameworkCore;
using SeconSbtiBackend.Data;
using SeconSbtiBackend.DTOs;
using SeconSbtiBackend.Interfaces;
using SeconSbtiBackend.Models;

namespace SeconSbtiBackend.Services;

public sealed class PurchasedServicesService(
    ApplicationDbContext context) : IPurchasedServicesService
{
    public async Task<List<PurchasedServiceResponse>> GetAllAsync(PurchasedServicesQuery filter, CancellationToken cancellationToken)
    {
        var query = QueryServices();

        if (filter.Year is int year)
        {
            var startDate = new DateOnly(year, filter.Month ?? 1, 1);
            var endMonth = filter.Month ?? 12;
            var endDate = new DateOnly(year, endMonth, DateTime.DaysInMonth(year, endMonth));

            query = query.Where(service =>
                service.PeriodFrom >= startDate && service.PeriodTo <= endDate);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<PurchasedServiceResponse?> CreateAsync(
        CreatePurchasedServiceRequest request,
        CancellationToken cancellationToken)
    {
        var accountNo = request.AccountNo!.Trim();

        var mapping = await context.AccountCategoryMappings
            .AsNoTracking()
            .Include(mapping => mapping.ServiceCategory)
            .SingleOrDefaultAsync(
                mapping => mapping.AccountNo == accountNo,
                cancellationToken);

        if (mapping is null)
        {
            return null;
        }

        var report = new ServiceSpendReport
        {
            Id = Guid.NewGuid(),
            ImportBatchId = null,
            PeriodFrom = request.PeriodFrom!.Value,
            PeriodTo = request.PeriodTo!.Value
        };

        var service = new PurchasedService
        {
            ReportId = report.Id,
            Report = report,
            AccountNo = accountNo,
            AmountSek = request.Value!.Value,
            MeasurementQuality = request.MeasurementQuality!,
            Comment = request.Comment
        };

        context.PurchasedServices.Add(service);
        await context.SaveChangesAsync(cancellationToken);

        return new PurchasedServiceResponse
        {
            ReportId = report.Id,
            AccountNo = service.AccountNo,
            ServiceLabel = mapping.ServiceLabel,
            ServiceCategoryId = mapping.ServiceCategoryId,
            ServiceCategoryName = mapping.ServiceCategory.Name,
            PeriodFrom = report.PeriodFrom,
            PeriodTo = report.PeriodTo,
            Value = service.AmountSek,
            Unit = "sek",
            MeasurementQuality = service.MeasurementQuality,
            Comment = service.Comment
        };
    }

    public async Task<PurchasedServiceUpdateResult> UpdateAsync(
        Guid reportId,
        string accountNo,
        UpdatePurchasedServiceRequest request,
        CancellationToken cancellationToken)
    {
        var service = await context.PurchasedServices
            .Include(service => service.Report)
            .SingleOrDefaultAsync(
                service => service.ReportId == reportId && service.AccountNo == accountNo,
                cancellationToken);

        if (service is null)
        {
            return PurchasedServiceUpdateResult.NotFound("Den köpta tjänsten kunde inte hittas.");
        }

        var periodChanged = service.Report.PeriodFrom != request.PeriodFrom!.Value ||
            service.Report.PeriodTo != request.PeriodTo!.Value;

        if (periodChanged &&
            (service.Report.ImportBatchId.HasValue ||
             await context.PurchasedServices.AnyAsync(
                 other => other.ReportId == reportId && other.AccountNo != accountNo,
                 cancellationToken)))
        {
            return PurchasedServiceUpdateResult.ValidationError(
                "periodTo",
                "Perioden kan bara ändras för en manuell rapport med en tjänsterad.");
        }

        service.AmountSek = request.Value!.Value;
        service.MeasurementQuality = request.MeasurementQuality!;
        service.Comment = request.Comment ?? service.Comment;
        service.Report.PeriodFrom = request.PeriodFrom!.Value;
        service.Report.PeriodTo = request.PeriodTo!.Value;

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return PurchasedServiceUpdateResult.NotFound("Den köpta tjänsten kunde inte hittas.");
        }

        var response = await QueryServices()
            .SingleAsync(
                service => service.ReportId == reportId && service.AccountNo == accountNo,
                cancellationToken);

        return PurchasedServiceUpdateResult.Success(response);
    }

    private IQueryable<PurchasedServiceResponse> QueryServices()
    {
        return
            from service in context.PurchasedServices.AsNoTracking()
            join mapping in context.AccountCategoryMappings
                on service.AccountNo equals mapping.AccountNo into mappings
            from mapping in mappings.DefaultIfEmpty()
            orderby service.Report.PeriodFrom descending, service.AccountNo, service.ReportId
            select new PurchasedServiceResponse
            {
                ReportId = service.ReportId,
                AccountNo = service.AccountNo,
                ServiceLabel = mapping == null ? null : mapping.ServiceLabel,
                ServiceCategoryId = mapping == null ? null : mapping.ServiceCategoryId,
                ServiceCategoryName = mapping == null ? null : mapping.ServiceCategory.Name,
                PeriodFrom = service.Report.PeriodFrom,
                PeriodTo = service.Report.PeriodTo,
                Value = service.AmountSek,
                Unit = "sek",
                MeasurementQuality = service.MeasurementQuality,
                Comment = service.Comment
            };
    }
}
