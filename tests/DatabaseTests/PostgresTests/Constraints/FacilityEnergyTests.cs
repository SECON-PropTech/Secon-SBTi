using tests.DatabaseTests.PostgresTests.Infrastructure;

namespace tests.DatabaseTests.PostgresTests.Constraints;

public sealed class FacilityEnergyTests(PostgresFixture fixture) : DbTestBase(fixture)
{
    [Theory]
    [InlineData(-1)]
    public async Task Energy_must_not_be_negative(decimal kwh)
    {
        var user = await InsertUserAsync();
        await AssertRejectedAsync(CheckViolation, """
            INSERT INTO facility_energy(city,energy_carrier,reporting_year,energy_kwh,measurement_quality,created_by)
            VALUES('Teststad','el',2026,$1,'measured',$2)
            """, kwh, user);
    }

    [Fact]
    public async Task Energy_carrier_and_measurement_quality_are_checked()
    {
        var user = await InsertUserAsync();
        await AssertRejectedAsync(CheckViolation, """
            INSERT INTO facility_energy(city,energy_carrier,reporting_year,energy_kwh,measurement_quality,created_by)
            VALUES('Teststad','gas',2026,10,'measured',$1)
            """, user);
        await AssertRejectedAsync(CheckViolation, """
            INSERT INTO facility_energy(city,energy_carrier,reporting_year,energy_kwh,measurement_quality,created_by)
            VALUES('Teststad','el',2026,10,'unknown',$1)
            """, user);
    }

    [Fact]
    public async Task City_carrier_year_combination_is_unique()
    {
        var user = await InsertUserAsync();
        await ExecAsync("""
            INSERT INTO facility_energy(city,energy_carrier,reporting_year,energy_kwh,measurement_quality,created_by)
            VALUES('UniqueCity','el',2026,10,'measured',$1)
            """, user);
        await AssertRejectedAsync(UniqueViolation, """
            INSERT INTO facility_energy(city,energy_carrier,reporting_year,energy_kwh,measurement_quality,created_by)
            VALUES('UniqueCity','el',2026,20,'estimated',$1)
            """, user);
    }
}
