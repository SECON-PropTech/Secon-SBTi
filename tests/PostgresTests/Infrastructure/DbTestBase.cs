using Npgsql;
using Xunit;
using SeconSbtiBackend.Data;
using Microsoft.EntityFrameworkCore;

namespace tests.PostgresTests.Infrastructure;

[Collection("db")]
public abstract class DbTestBase(PostgresFixture fixture)
{
    protected const string UniqueViolation = "23505";
    protected const string ForeignKeyViolation = "23503";
    protected const string CheckViolation = "23514";
    protected const string NotNullViolation = "23502";

    protected PostgresFixture Fixture { get; } = fixture;
    protected static string Unique(string prefix = "x") => $"{prefix}-{Guid.NewGuid():N}";
    protected ApplicationDbContext CreateContext()
{
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseNpgsql(Fixture.ConnectionString)
        .Options;

    return new ApplicationDbContext(options);
}

    protected static async Task<int> ExecOnAsync(string cs, string sql, params object?[] args)
    {
        await using var c = new NpgsqlConnection(cs);
        await c.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, c);
        foreach (var arg in args) cmd.Parameters.Add(new NpgsqlParameter { Value = arg ?? DBNull.Value });
        return await cmd.ExecuteNonQueryAsync();
    }

    protected static async Task<T> ScalarOnAsync<T>(string cs, string sql, params object?[] args)
    {
        await using var c = new NpgsqlConnection(cs);
        await c.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, c);
        foreach (var arg in args) cmd.Parameters.Add(new NpgsqlParameter { Value = arg ?? DBNull.Value });
        var value = await cmd.ExecuteScalarAsync();
        return value is null or DBNull ? default! : (T)value;
    }

    protected Task<int> ExecAsync(string sql, params object?[] args) =>
        ExecOnAsync(Fixture.ConnectionString, sql, args);

    protected Task<T> ScalarAsync<T>(string sql, params object?[] args) =>
        ScalarOnAsync<T>(Fixture.ConnectionString, sql, args);

    protected async Task AssertRejectedAsync(string state, string sql, params object?[] args)
    {
        var ex = await Assert.ThrowsAsync<PostgresException>(() => ExecAsync(sql, args));
        Assert.Equal(state, ex.SqlState);
    }

    protected Task<Guid> InsertUserAsync(string? email = null) =>
        ScalarAsync<Guid>("INSERT INTO app_user(first_name,last_name,email) VALUES('Test','User',$1) RETURNING id",
            email ?? $"{Unique("user")}@test.se");

    protected Task<Guid> InsertRoleAsync(string? name = null) =>
        ScalarAsync<Guid>("INSERT INTO role(name) VALUES($1) RETURNING id", name ?? Unique("role"));

    protected Task<Guid> InsertSupplierAsync(string? name = null) =>
        ScalarAsync<Guid>("INSERT INTO supplier(name) VALUES($1) RETURNING id", name ?? Unique("supplier"));

    protected Task<Guid> InsertPurchaseCategoryAsync(string? name = null) =>
        ScalarAsync<Guid>("INSERT INTO purchase_category(name) VALUES($1) RETURNING id", name ?? Unique("category"));

    protected Task<Guid> InsertWasteTypeAsync() =>
        ScalarAsync<Guid>("INSERT INTO waste_type(name) VALUES($1) RETURNING id", Unique("waste-type"));

    protected Task<Guid> InsertWasteTreatmentAsync() =>
        ScalarAsync<Guid>("INSERT INTO waste_treatment(name) VALUES($1) RETURNING id", Unique("treatment"));

    protected Task<string> InsertVehicleAsync(string? registration = null) =>
        ScalarAsync<string>("INSERT INTO vehicle(registration_number,vehicle_type,fuel_type) VALUES($1,'Car','Diesel') RETURNING registration_number",
            registration ?? Unique("REG"));

    protected async Task<Guid> InsertImportBatchAsync(Guid? userId = null, string source = "mobigo")
    {
        userId ??= await InsertUserAsync();
        return await ScalarAsync<Guid>(
            "INSERT INTO import_batch(source, imported_by) VALUES($1,$2) RETURNING id",
            source, userId);
    }

    protected async Task<Guid> InsertPurchasedGoodAsync(Guid? supplierId = null, Guid? userId = null)
    {
        supplierId ??= await InsertSupplierAsync();
        userId ??= await InsertUserAsync();
        return await ScalarAsync<Guid>("""
            INSERT INTO purchased_good(supplier_id,transaction_date,value,unit,measurement_quality,created_by)
            VALUES($1, DATE '2026-01-01', 100, 'sek', 'measured', $2)
            RETURNING id
            """, supplierId, userId);
    }

    protected async Task<Guid> InsertFacilityEnergyAsync(Guid? userId = null, string? city = null)
    {
        userId ??= await InsertUserAsync();
        return await ScalarAsync<Guid>("""
            INSERT INTO facility_energy(city,energy_carrier,reporting_year,energy_kwh,measurement_quality,created_by)
            VALUES($1,'el',2026,100,'measured',$2) RETURNING id
            """, city ?? Unique("city"), userId);
    }

    protected async Task<Guid> InsertVehicleUsageAsync(Guid? userId = null, string? registration = null)
    {
        userId ??= await InsertUserAsync();
        registration ??= await InsertVehicleAsync();
        return await ScalarAsync<Guid>("""
            INSERT INTO vehicle_usage(registration_number,fuel_type,date,value,unit,measurement_quality,created_by)
            VALUES($1,'Diesel',DATE '2026-01-01',10,'liter','measured',$2) RETURNING id
            """, registration, userId);
    }

    protected async Task<Guid> InsertWasteAsync(Guid? userId = null)
    {
        userId ??= await InsertUserAsync();
        var type = await InsertWasteTypeAsync();
        var treatment = await InsertWasteTreatmentAsync();
        return await ScalarAsync<Guid>("""
            INSERT INTO waste(waste_type_id,waste_treatment_id,date,weight_kg,measurement_quality,created_by)
            VALUES($1,$2,DATE '2026-01-01',5,'measured',$3) RETURNING id
            """, type, treatment, userId);
    }

    protected async Task<Guid> InsertFlightAsync(Guid? creatorId = null, Guid? travellerId = null)
    {
        creatorId ??= await InsertUserAsync();
        return await ScalarAsync<Guid>("""
            INSERT INTO business_travel_flight(departure_iata_code,destination_iata_code,single_trips_quantity,
                measurement_quality,created_by,travelling_user_id)
            VALUES('ARN','LAX',1,'measured',$1,$2) RETURNING id
            """, creatorId, travellerId);
    }

    protected async Task<Guid> InsertGroundAsync(Guid? creatorId = null)
    {
        creatorId ??= await InsertUserAsync();
        return await ScalarAsync<Guid>("""
            INSERT INTO business_travel_ground(travel_mode,date,value,unit,measurement_quality,created_by)
            VALUES('Tåg',DATE '2026-01-01',10,'km','measured',$1) RETURNING id
            """, creatorId);
    }
}
