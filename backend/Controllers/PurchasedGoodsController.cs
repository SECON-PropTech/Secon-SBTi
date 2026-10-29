using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SeconSbtiBackend.Data;
using SeconSbtiBackend.DTOs;
using SeconSbtiBackend.Models;


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

        [HttpPost]
        [ProducesResponseType(
    typeof(PurchasedGoodResponse),
    StatusCodes.Status201Created)]
        public async Task<ActionResult<PurchasedGoodResponse>>
    CreatePurchasedGood(
        [FromBody] CreatePurchasedGoodRequest request,
        [FromQuery] Guid createdBy,
        CancellationToken cancellationToken)
        {
            var supplierExists = await _context.Suppliers
                .AnyAsync(
                    supplier => supplier.Id == request.SupplierId,
                    cancellationToken);

            if (!supplierExists)
            {
                return BadRequest(new
                {
                    field = "supplierId",
                    message = "The supplier does not exist."
                });
            }

            // Temporary creator input for local testing until authentication is available.
            var userExists = await _context.AppUsers
                .AnyAsync(
                    user => user.Id == createdBy,
                    cancellationToken);

            if (!userExists)
            {
                return BadRequest(new
                {
                    field = "createdBy",
                    message = "Provide the ID of an existing test user."
                });
            }

            if (request.Unit != "sek")
            {
                return BadRequest(new
                {
                    field = "unit",
                    message = "Only sek is supported in this purchase flow."
                });
            }

            if (request.MeasurementQuality is not ("measured" or "estimated"))
            {
                return BadRequest(new
                {
                    field = "measurementQuality",
                    message = "Use measured or estimated."
                });
            }

            var good = new PurchasedGood
            {
                Id = Guid.NewGuid(),
                SupplierId = request.SupplierId,
                TransactionDate = request.TransactionDate,
                Value = request.Value,
                Unit = request.Unit,
                MeasurementQuality = request.MeasurementQuality,
                GoodsName = request.GoodsName,
                Comment = request.Comment,
                Specification = request.Specification,
                AccountNo = request.AccountNo,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };

            _context.PurchasedGoods.Add(good);
            await _context.SaveChangesAsync(cancellationToken);

            var response = new PurchasedGoodResponse
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
            };

            return StatusCode(StatusCodes.Status201Created, response);
        }
    }
}
