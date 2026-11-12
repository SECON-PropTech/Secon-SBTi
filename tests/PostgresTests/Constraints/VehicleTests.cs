using tests.PostgresTests.Infrastructure;

namespace tests.PostgresTests.Constraints;

public sealed class VehicleTests(PostgresFixture fixture) : DbTestBase(fixture)
{
    [Fact]
    public async Task Vehicle_registration_is_primary_key()
    {
        var registration = await InsertVehicleAsync();
        await AssertRejectedAsync(UniqueViolation,
            "INSERT INTO vehicle(registration_number) VALUES($1)", registration);
    }

    [Fact]
    public async Task Vehicle_usage_requires_existing_vehicle()
    {
        var user = await InsertUserAsync();
        await AssertRejectedAsync(ForeignKeyViolation, """
            INSERT INTO vehicle_usage(registration_number,fuel_type,date,value,unit,measurement_quality,created_by)
            VALUES($1,'Diesel',DATE '2026-01-01',10,'liter','measured',$2)
            """, Unique("REG"), user);
    }

    [Fact]
    public async Task Vehicle_usage_value_and_quality_are_checked()
    {
        var user = await InsertUserAsync();
        var registration = await InsertVehicleAsync();
        await AssertRejectedAsync(CheckViolation, """
            INSERT INTO vehicle_usage(registration_number,fuel_type,date,value,unit,measurement_quality,created_by)
            VALUES($1,'Diesel',DATE '2026-01-01',-1,'liter','measured',$2)
            """, registration, user);
        await AssertRejectedAsync(CheckViolation, """
            INSERT INTO vehicle_usage(registration_number,fuel_type,date,value,unit,measurement_quality,created_by)
            VALUES($1,'Diesel',DATE '2026-01-01',1,'liter','bad',$2)
            """, registration, user);
    }
}
