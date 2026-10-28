using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SeconSbtiBackend.Data;
using SeconSbtiBackend.DTOs;


namespace SeconSbtiBackend.Controllers
{

    [ApiController]
    [Route("api/purchased-goods")]
    public class PurchasedGoodsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public PurchasedGoodsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<PurchasedGoodResponse>>>
        GetPurchasedGoods(CancellationToken cancellationToken)
        {
            var goods = await _context.PurchasedGoods
                .AsNoTracking()
                .OrderByDescending(good => good.TransactionDate)
                .ThenBy(good => good.Id)
                .Select(good => new PurchasedGoodResponse
                {
                    Id = good.Id,
                    SupplierId = good.SupplierId,
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
                })
                .ToListAsync(cancellationToken);

            return Ok(goods);
        }
    }
}
