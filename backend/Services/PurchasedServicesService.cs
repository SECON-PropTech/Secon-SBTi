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

    public async Task<PurchasedServiceResult> CreateAsync(
        CreatePurchasedServiceRequest request,
        CancellationToken cancellationToken)
    {
        var accountNo = request.AccountNo!.Trim();

        var mappingExists = await context.AccountCategoryMappings
            .AnyAsync(
                mapping => mapping.AccountNo == accountNo,
                cancellationToken);

        if (!mappingExists)
        {
            return PurchasedServiceResult.ValidationError(
                "accountNo",
                "Kontot saknar en kopplad tjänstekategori. Välj ett kategoriserat konto.");
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

        var response = await QueryServices()
            .SingleAsync(
                saved => saved.ReportId == report.Id && saved.AccountNo == accountNo,
                cancellationToken);

        return PurchasedServiceResult.Success(response);
    }

    public async Task<PurchasedServiceResult> UpdateAsync(
        Guid reportId,
        string accountNo,
        UpdatePurchasedServiceRequest request,
        CancellationToken cancellationToken)
    {
        accountNo = accountNo.Trim();

        var service = await context.PurchasedServices
            .Include(service => service.Report)
            .SingleOrDefaultAsync(
                service => service.ReportId == reportId && service.AccountNo == accountNo,
                cancellationToken);

        if (service is null)
        {
            return PurchasedServiceResult.NotFound("Den köpta tjänsten kunde inte hittas.");
        }

        var periodFromChanged = service.Report.PeriodFrom != request.PeriodFrom!.Value;
        var periodToChanged = service.Report.PeriodTo != request.PeriodTo!.Value;

        if ((periodFromChanged || periodToChanged) &&
            (service.Report.ImportBatchId.HasValue ||
             await context.PurchasedServices.AnyAsync(
                 other => other.ReportId == reportId && other.AccountNo != accountNo,
                 cancellationToken)))
        {
            const string message = "Perioden kan bara ändras för en manuell rapport med en tjänsterad.";
            var errors = new Dictionary<string, string[]>();
            if (periodFromChanged) errors["periodFrom"] = [message];
            if (periodToChanged) errors["periodTo"] = [message];
            return PurchasedServiceResult.ValidationError(errors);
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
            return PurchasedServiceResult.NotFound("Den köpta tjänsten kunde inte hittas.");
        }

        var response = await QueryServices()
            .SingleAsync(
                service => service.ReportId == reportId && service.AccountNo == accountNo,
                cancellationToken);

        return PurchasedServiceResult.Success(response);
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
