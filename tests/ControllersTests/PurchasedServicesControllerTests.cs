using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SeconSbtiBackend.Controllers;
using SeconSbtiBackend.Data;
using SeconSbtiBackend.Interfaces;
using SeconSbtiBackend.Models;
using SeconSbtiBackend.Services;

namespace api_tests;

public class PurchasedServicesControllerTests
{
    private static readonly Guid ReportId = Guid.Parse("40000000-0000-0000-0000-000000000001");
    private const string UpdatePath = "/api/purchased-services/40000000-0000-0000-0000-000000000001";

    [Fact]
    public async Task Update_trims_the_account_number_from_the_query()
    {
        await using var api = await TestApi.Start();
        using var response = await api.Client.PutAsJsonAsync(
            UpdatePath + "?accountNo=%206540%20", Payload());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("6540", saved.GetProperty("accountNo").GetString());
        Assert.Equal(200m, saved.GetProperty("value").GetDecimal());
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task Shared_report_period_errors_identify_each_changed_field(
        bool changeStart, bool changeEnd)
    {
        await using var api = await TestApi.Start(sharedReport: true);
        var body = Payload();
        if (changeStart) body["periodFrom"] = "2026-11-02";
        if (changeEnd) body["periodTo"] = "2026-11-29";
        using var response = await api.Client.PutAsJsonAsync(
            UpdatePath + "?accountNo=6540", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("errors");
        Assert.Equal(changeStart, errors.TryGetProperty("periodFrom", out _));
        Assert.Equal(changeEnd, errors.TryGetProperty("periodTo", out _));
        using var list = await api.Client.GetAsync("/api/purchased-services?year=2026");
        var rows = await list.Content.ReadFromJsonAsync<JsonElement>();
        Assert.All(rows.EnumerateArray(), row =>
        {
            Assert.Equal("2026-11-01", row.GetProperty("periodFrom").GetString());
            Assert.Equal("2026-11-30", row.GetProperty("periodTo").GetString());
        });
    }

    [Theory]
    [InlineData("POST", 2000, HttpStatusCode.Created)]
    [InlineData("POST", 2001, HttpStatusCode.BadRequest)]
    [InlineData("PUT", 2000, HttpStatusCode.OK)]
    [InlineData("PUT", 2001, HttpStatusCode.BadRequest)]
    public async Task Comments_are_limited_on_create_and_update(
        string method, int length, HttpStatusCode expectedStatus)
    {
        await using var api = await TestApi.Start();
        var body = Payload();
        body["comment"] = new string('a', length);
        var path = method == "POST" ? "/api/purchased-services" : UpdatePath + "?accountNo=6540";
        using var request = new HttpRequestMessage(new HttpMethod(method), path)
        {
            Content = JsonContent.Create(body)
        };
        using var response = await api.Client.SendAsync(request);

        Assert.Equal(expectedStatus, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        if (expectedStatus == HttpStatusCode.BadRequest)
            Assert.True(json.GetProperty("errors").TryGetProperty("comment", out _));
        else
            Assert.Equal(length, json.GetProperty("comment").GetString()!.Length);
    }

    [Fact]
    public async Task Missing_account_mapping_returns_a_field_error_without_saving()
    {
        await using var api = await TestApi.Start();
        var body = Payload();
        body["accountNo"] = "9999";
        using var response = await api.Client.PostAsJsonAsync("/api/purchased-services", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("errors");
        Assert.Contains("tjänstekategori", errors.GetProperty("accountNo")[0].GetString()!);
        using var list = await api.Client.GetAsync("/api/purchased-services");
        Assert.Single((await list.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray());
    }

    [Theory]
    [InlineData("unit", "kg")]
    [InlineData("measurementQuality", "unknown")]
    [InlineData("value", -1)]
    [InlineData("periodTo", "2026-10-31")]
    public async Task Create_keeps_the_shared_validation_rules(string field, object value)
    {
        await using var api = await TestApi.Start();
        var body = Payload();
        body[field] = value;
        using var response = await api.Client.PostAsJsonAsync("/api/purchased-services", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("errors");
        Assert.True(errors.TryGetProperty(field, out _));
    }

    [Fact]
    public async Task Created_response_matches_the_saved_service()
    {
        await using var api = await TestApi.Start();
        using var response = await api.Client.PostAsJsonAsync("/api/purchased-services", Payload());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Test category", created.GetProperty("serviceCategoryName").GetString());
        using var list = await api.Client.GetAsync("/api/purchased-services?year=2026&month=11");
        var saved = (await list.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray()
            .Single(row => row.GetProperty("reportId").GetGuid() == created.GetProperty("reportId").GetGuid());
        Assert.Equal(created.GetRawText(), saved.GetRawText());
    }

    private static Dictionary<string, object?> Payload() => new()
    {
        ["accountNo"] = "6540",
        ["periodFrom"] = "2026-11-01",
        ["periodTo"] = "2026-11-30",
        ["value"] = 200m,
        ["unit"] = "sek",
        ["measurementQuality"] = "estimated",
        ["comment"] = "Test comment"
    };

    private sealed class TestApi(WebApplication app, HttpClient client) : IAsyncDisposable
    {
        public HttpClient Client { get; } = client;

        public static async Task<TestApi> Start(bool sharedReport = false)
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Logging.ClearProviders();
            var databaseName = Guid.NewGuid().ToString();
            builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(databaseName));
            builder.Services.AddScoped<IPurchasedServicesService, PurchasedServicesService>();
            builder.Services.AddControllers(options =>
                options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider()))
                .AddApplicationPart(typeof(PurchasedServicesController).Assembly);
            var app = builder.Build();
            app.MapControllers();
            await app.StartAsync();
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var category = new ServiceCategory { Id = Guid.NewGuid(), Name = "Test category" };
                db.AccountCategoryMappings.Add(new AccountCategoryMapping
                {
                    AccountNo = "6540", ServiceCategory = category, ServiceLabel = "Test service"
                });
                var report = new ServiceSpendReport
                {
                    Id = ReportId, PeriodFrom = new DateOnly(2026, 11, 1), PeriodTo = new DateOnly(2026, 11, 30)
                };
                db.PurchasedServices.Add(new PurchasedService
                {
                    Report = report, AccountNo = "6540", AmountSek = 100m, MeasurementQuality = "measured"
                });
                if (sharedReport)
                    db.PurchasedServices.Add(new PurchasedService
                    {
                        Report = report, AccountNo = "6550", AmountSek = 50m, MeasurementQuality = "measured"
                    });
                await db.SaveChangesAsync();
            }
            return new TestApi(app, app.GetTestClient());
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await app.DisposeAsync();
        }
    }
}
