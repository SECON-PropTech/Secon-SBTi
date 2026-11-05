using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SeconSbtiBackend.Controllers;
using SeconSbtiBackend.Data;
using SeconSbtiBackend.Interfaces;
using SeconSbtiBackend.Models;
using SeconSbtiBackend.Services;
using Xunit;

namespace api_tests;

public class PurchasedGoodsControllerTests
{
    private static readonly Guid SupplierId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid UserId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid GoodId = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private static readonly string[] TextFields = ["goodsName", "comment", "specification", "accountNo"];
    private static readonly string[] RequiredFields = ["supplierId", "transactionDate", "value", "unit", "measurementQuality"];

    public static IEnumerable<object[]> MissingFields()
    {
        foreach (var method in new[] { "POST", "PUT" })
            foreach (var field in RequiredFields)
                yield return [method, field];
    }

    [Theory]
    [MemberData(nameof(MissingFields))]
    public async Task Missing_required_fields_return_field_specific_errors(string method, string field)
    {
        await using var api = await TestApi.Start();
        var body = Payload();
        body.Remove(field);
        using var response = await api.Send(method, WritePath(method), body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await Json(response);
        Assert.True(json.GetProperty("errors").TryGetProperty(field, out _));
    }

    [Theory]
    [InlineData("unit", "kg")]
    [InlineData("measurementQuality", "guessed")]
    public async Task Invalid_values_use_validation_problem_format(string field, string value)
    {
        await using var api = await TestApi.Start();
        var body = Payload();
        body[field] = value;
        using var response = await api.Send("PUT", WritePath("PUT"), body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await Json(response)).GetProperty("errors").TryGetProperty(field, out _));
    }

    [Fact]
    public async Task Unknown_supplier_is_rejected()
    {
        await using var api = await TestApi.Start();
        var body = Payload();
        body["supplierId"] = Guid.NewGuid();
        using var response = await api.Send("PUT", WritePath("PUT"), body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await Json(response)).GetProperty("errors").TryGetProperty("supplierId", out _));
    }

    [Fact]
    public async Task Empty_supplier_id_is_rejected()
    {
        await using var api = await TestApi.Start();
        var body = Payload();
        body["supplierId"] = Guid.Empty;
        using var response = await api.Send("POST", WritePath("POST"), body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await Json(response)).GetProperty("errors").TryGetProperty("supplierId", out _));
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    public async Task Writes_require_authentication(string method)
    {
        await using var api = await TestApi.Start();
        using var response = await api.Send(method, WritePath(method), Payload(), authenticated: false);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_creates_purchase_and_uses_authenticated_user()
    {
        await using var api = await TestApi.Start();
        using var response = await api.Send("POST", WritePath("POST"), Payload());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await Json(response);
        Assert.Equal(UserId, created.GetProperty("createdBy").GetGuid());
        Assert.Equal(100m, created.GetProperty("value").GetDecimal());
        using var list = await api.Send("GET", "/api/purchased-goods?year=2025");
        Assert.Contains((await Json(list)).EnumerateArray(), item => item.GetProperty("id").GetGuid() == created.GetProperty("id").GetGuid());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task Put_accepts_zero_and_credit_values(int value)
    {
        await using var api = await TestApi.Start();
        var body = Payload();
        body["value"] = value;
        using var response = await api.Send("PUT", WritePath("PUT"), body);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal((decimal)value, (await Json(response)).GetProperty("value").GetDecimal());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Put_clears_null_or_omitted_optional_fields(bool omitted)
    {
        await using var api = await TestApi.Start();
        var body = Payload();
        foreach (var field in TextFields)
        {
            if (omitted) body.Remove(field);
            else body[field] = null;
        }
        using var response = await api.Send("PUT", WritePath("PUT"), body);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await Json(response);
        foreach (var field in TextFields)
            Assert.Equal(JsonValueKind.Null, json.GetProperty(field).ValueKind);
        using var list = await api.Send("GET", "/api/purchased-goods?year=2025");
        var saved = (await Json(list)).EnumerateArray().Single(item => item.GetProperty("id").GetGuid() == GoodId);
        foreach (var field in TextFields)
            Assert.Equal(JsonValueKind.Null, saved.GetProperty(field).ValueKind);
    }

    [Theory]
    [InlineData("month=2", "year")]
    [InlineData("year=0", "year")]
    [InlineData("year=2025&month=13", "month")]
    public async Task Invalid_period_returns_field_specific_error(string query, string field)
    {
        await using var api = await TestApi.Start();
        using var response = await api.Send("GET", $"/api/purchased-goods?{query}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await Json(response)).GetProperty("errors").TryGetProperty(field, out _));
    }

    [Fact]
    public async Task February_filter_includes_leap_day_and_excludes_neighboring_months()
    {
        await using var api = await TestApi.Start();
        using var response = await api.Send("GET", "/api/purchased-goods?year=2024&month=2");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dates = (await Json(response)).EnumerateArray().Select(item => item.GetProperty("transactionDate").GetString()).OrderBy(date => date).ToArray();
        Assert.Equal(new[] { "2024-02-01", "2024-02-29" }, dates);
    }

    [Fact]
    public async Task Year_filter_excludes_purchases_from_other_years()
    {
        await using var api = await TestApi.Start();
        using var response = await api.Send("GET", "/api/purchased-goods?year=2024");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rows = (await Json(response)).EnumerateArray().ToArray();
        Assert.Equal(4, rows.Length);
        Assert.All(rows, item => Assert.StartsWith("2024-", item.GetProperty("transactionDate").GetString()));
    }

    private static string WritePath(string method) => method == "POST" ? "/api/purchased-goods" : $"/api/purchased-goods/{GoodId}";
    private static Dictionary<string, object?> Payload() => new()
    {
        ["supplierId"] = SupplierId, ["transactionDate"] = "2025-06-01", ["value"] = 100m,
        ["unit"] = "sek", ["measurementQuality"] = "estimated", ["goodsName"] = "Updated good",
        ["comment"] = "Updated comment", ["specification"] = "Updated specification", ["accountNo"] = "4010"
    };
    private static async Task<JsonElement> Json(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }

    private sealed class TestApi(WebApplication app, HttpClient client) : IAsyncDisposable
    {
        public static async Task<TestApi> Start()
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Logging.ClearProviders();
            var databaseName = Guid.NewGuid().ToString();
            builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(databaseName));
            builder.Services.AddScoped<IPurchasedGoodsService, PurchasedGoodsService>();
            builder.Services.AddControllers(options => options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider()))
                .AddApplicationPart(typeof(PurchasedGoodsController).Assembly);
            builder.Services.AddAuthentication("PurchasedGoodsTest")
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("PurchasedGoodsTest", null);
            builder.Services.AddAuthorization();
            var app = builder.Build();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            await app.StartAsync();
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.AppUsers.Add(new AppUser { Id = UserId, FirstName = "Test", LastName = "User", Email = "test@example.test", CreatedAt = DateTime.UtcNow });
                db.Suppliers.Add(new Supplier { Id = SupplierId, Name = "Test supplier" });
                db.PurchasedGoods.Add(Good(GoodId, new DateOnly(2025, 6, 1)));
                foreach (var date in new[] { new DateOnly(2024, 1, 31), new DateOnly(2024, 2, 1), new DateOnly(2024, 2, 29), new DateOnly(2024, 3, 1) })
                    db.PurchasedGoods.Add(Good(Guid.NewGuid(), date));
                await db.SaveChangesAsync();
            }
            return new TestApi(app, app.GetTestClient());
        }

        private static PurchasedGood Good(Guid id, DateOnly date) => new()
        {
            Id = id, SupplierId = SupplierId, TransactionDate = date, Value = 100, Unit = "sek", MeasurementQuality = "estimated",
            GoodsName = "Original good", Comment = "Faktura saknades", Specification = "Original specification", AccountNo = "4010",
            CreatedBy = UserId, CreatedAt = DateTime.UtcNow
        };

        public async Task<HttpResponseMessage> Send(string method, string path, object? body = null, bool authenticated = true)
        {
            using var request = new HttpRequestMessage(new HttpMethod(method), path);
            if (authenticated) request.Headers.Add("X-PurchasedGoods-Test-User", UserId.ToString());
            if (body is not null) request.Content = JsonContent.Create(body);
            return await client.SendAsync(request);
        }

        public async ValueTask DisposeAsync()
        {
            client.Dispose();
            await app.DisposeAsync();
        }
    }

    public sealed class TestAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-PurchasedGoods-Test-User", out var userId))
                return Task.FromResult(AuthenticateResult.NoResult());
            var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "PurchasedGoodsTest");
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), "PurchasedGoodsTest")));
        }
    }
}
