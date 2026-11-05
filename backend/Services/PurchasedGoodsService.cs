using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SeconSbtiBackend.Data;
using SeconSbtiBackend.DTOs;
using SeconSbtiBackend.Interfaces;
using SeconSbtiBackend.Models;

namespace SeconSbtiBackend.Services;

public sealed class PurchasedGoodsService(
    ApplicationDbContext context) : IPurchasedGoodsService
{
    private static readonly Expression<
        Func<PurchasedGood, PurchasedGoodResponse>>
        ResponseProjection = good => new PurchasedGoodResponse
        {
            Id = good.Id,
            SupplierId = good.SupplierId,

            Categories = good.Supplier.SupplierCategoryAllocations
                .OrderBy(allocation => allocation.PurchaseCategory.Name)
                .ThenBy(allocation => allocation.PurchaseCategoryId)
                .Select(allocation => new PurchasedGoodCategoryResponse
                {
                    PurchaseCategoryId = allocation.PurchaseCategoryId,
                    Name = allocation.PurchaseCategory.Name,
                    Share = allocation.Share
                })
                .ToList(),

            TransactionDate = good.TransactionDate,
            Value = good.Value,
            Unit = good.Unit,
            MeasurementQuality = good.MeasurementQuality,
            GoodsName = good.GoodsName,
            Comment = good.Comment,
            Specification = good.Specification,
            AccountNo = good.AccountNo,
            CreatedBy = good.CreatedBy,
            CreatedAt = good.CreatedAt
        };

    public async Task<PurchasedGoodsResult<List<PurchasedGoodResponse>>> GetAllAsync(
        int? year,
        int? month,
        CancellationToken cancellationToken)
    {
        if (month.HasValue && !year.HasValue)
        {
            return PurchasedGoodsResult<List<PurchasedGoodResponse>>.ValidationError(
                "year",
                "Ange ett år när du filtrerar på månad.");
        }

        if (year is < 1 or > 9999)
        {
            return PurchasedGoodsResult<List<PurchasedGoodResponse>>.ValidationError(
                "year",
                "Ange siffror mellan 1 och 9999.");
        }

        if (month is < 1 or > 12)
        {
            return PurchasedGoodsResult<List<PurchasedGoodResponse>>.ValidationError(
                "month",
                "Ange en månad mellan 1 och 12.");
        }

        var query = context.PurchasedGoods.AsNoTracking();

        if (year.HasValue)
        {
            var startDate = new DateOnly(
                year.Value,
                month ?? 1,
                1);

            var endDate = month.HasValue
                ? new DateOnly(
                    year.Value,
                    month.Value,
                    DateTime.DaysInMonth(year.Value, month.Value))
                : new DateOnly(year.Value, 12, 31);

            query = query.Where(good =>
                good.TransactionDate >= startDate &&
                good.TransactionDate <= endDate);
        }

        var goods = await query
            .OrderByDescending(good => good.TransactionDate)
            .ThenBy(good => good.Id)
            .Select(ResponseProjection)
            .ToListAsync(cancellationToken);

        return PurchasedGoodsResult<List<PurchasedGoodResponse>>.Success(goods);
    }

    public async Task<PurchasedGoodsResult<PurchasedGoodResponse>> CreateAsync(
        CreatePurchasedGoodRequest request,
        Guid createdBy,
        CancellationToken cancellationToken)
    {
        var userExists = await context.AppUsers
            .AnyAsync(user => user.Id == createdBy, cancellationToken);

        if (!userExists)
        {
            return PurchasedGoodsResult<PurchasedGoodResponse>.InvalidUser(
                "Användarkontot finns inte längre. Kontakta en administratör.");
        }

        if (!await SupplierExistsAsync(request.SupplierId!.Value, cancellationToken))
        {
            return PurchasedGoodsResult<PurchasedGoodResponse>.ValidationError(
                "supplierId",
                "Leverantören finns inte. Välj en annan leverantör.");
        }

        if (request.Unit != "sek")
        {
            return PurchasedGoodsResult<PurchasedGoodResponse>.ValidationError(
                "unit",
                "Endast belopp i SEK stöds för köpta varor.");
        }

        if (!IsValidMeasurementQuality(request.MeasurementQuality))
        {
            return PurchasedGoodsResult<PurchasedGoodResponse>.ValidationError(
                "measurementQuality",
                "Välj uppmätt eller uppskattat.");
        }

        var good = new PurchasedGood
        {
            Id = Guid.NewGuid(),
            SupplierId = request.SupplierId!.Value,
            TransactionDate = request.TransactionDate!.Value,
            Value = request.Value!.Value,
            Unit = request.Unit!,
            MeasurementQuality = request.MeasurementQuality!,
            GoodsName = request.GoodsName,
            Comment = request.Comment,
            Specification = request.Specification,
            AccountNo = request.AccountNo,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        context.PurchasedGoods.Add(good);
        await context.SaveChangesAsync(cancellationToken);

        var response = await GetResponseAsync(good.Id, cancellationToken);
        return PurchasedGoodsResult<PurchasedGoodResponse>.Success(response);
    }

    public async Task<PurchasedGoodsResult<PurchasedGoodResponse>> UpdateAsync(
        Guid id,
        UpdatePurchasedGoodRequest request,
        CancellationToken cancellationToken)
    {
        var good = await context.PurchasedGoods
            .SingleOrDefaultAsync(good => good.Id == id, cancellationToken);

        if (good is null)
        {
            return PurchasedGoodsResult<PurchasedGoodResponse>.NotFound(
                "Den köpta varan kunde inte hittas.");
        }

        if (good.Unit != "sek" || request.Unit != "sek")
        {
            return PurchasedGoodsResult<PurchasedGoodResponse>.ValidationError(
                "unit",
                "Endast inköp i SEK kan redigeras i detta flöde.");
        }

        if (!IsValidMeasurementQuality(request.MeasurementQuality))
        {
            return PurchasedGoodsResult<PurchasedGoodResponse>.ValidationError(
                "measurementQuality",
                "Välj uppmätt eller uppskattat.");
        }

        if (!await SupplierExistsAsync(request.SupplierId!.Value, cancellationToken))
        {
            return PurchasedGoodsResult<PurchasedGoodResponse>.ValidationError(
                "supplierId",
                "Leverantören finns inte. Välj en annan leverantör.");
        }

        good.SupplierId = request.SupplierId!.Value;
        good.TransactionDate = request.TransactionDate!.Value;
        good.Value = request.Value!.Value;
        good.Unit = request.Unit!;
        good.MeasurementQuality = request.MeasurementQuality!;
        good.GoodsName = request.GoodsName;
        good.Comment = request.Comment;
        good.Specification = request.Specification;
        good.AccountNo = request.AccountNo;

        await context.SaveChangesAsync(cancellationToken);

        var response = await GetResponseAsync(good.Id, cancellationToken);
        return PurchasedGoodsResult<PurchasedGoodResponse>.Success(response);
    }

    private static bool IsValidMeasurementQuality(string? measurementQuality) =>
        measurementQuality is "measured" or "estimated";

    private Task<bool> SupplierExistsAsync(
        Guid supplierId,
        CancellationToken cancellationToken) =>
        context.Suppliers.AnyAsync(supplier => supplier.Id == supplierId, cancellationToken);

    private async Task<PurchasedGoodResponse> GetResponseAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await context.PurchasedGoods
            .AsNoTracking()
            .Where(good => good.Id == id)
            .Select(ResponseProjection)
            .SingleAsync(cancellationToken);
    }
}
