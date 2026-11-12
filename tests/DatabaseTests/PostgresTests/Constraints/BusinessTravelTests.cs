using tests.DatabaseTests.PostgresTests.Infrastructure;

namespace tests.DatabaseTests.PostgresTests.Constraints;

public sealed class BusinessTravelTests(PostgresFixture fixture) : DbTestBase(fixture)
{
    [Fact]
    public async Task Flight_codes_must_be_three_uppercase_letters()
    {
        var user = await InsertUserAsync();
        await AssertRejectedAsync(CheckViolation, """
            INSERT INTO business_travel_flight(departure_iata_code,destination_iata_code,single_trips_quantity,measurement_quality,created_by)
            VALUES('arn','LAX',1,'measured',$1)
            """, user);
        await AssertRejectedAsync(CheckViolation, """
            INSERT INTO business_travel_flight(departure_iata_code,destination_iata_code,single_trips_quantity,measurement_quality,created_by)
            VALUES('ARN','LONG',1,'measured',$1)
            """, user);
    }

    [Fact]
    public async Task Flight_quantity_must_be_positive()
    {
        var user = await InsertUserAsync();
        await AssertRejectedAsync(CheckViolation, """
            INSERT INTO business_travel_flight(departure_iata_code,destination_iata_code,single_trips_quantity,measurement_quality,created_by)
            VALUES('ARN','LAX',0,'measured',$1)
            """, user);
    }

    [Fact]
    public async Task Ground_travel_value_must_be_positive_and_allowance_nonnegative()
    {
        var user = await InsertUserAsync();
        await AssertRejectedAsync(CheckViolation, """
            INSERT INTO business_travel_ground(travel_mode,date,value,unit,measurement_quality,created_by)
            VALUES('Tåg',DATE '2026-01-01',0,'km','measured',$1)
            """, user);
        await AssertRejectedAsync(CheckViolation, """
            INSERT INTO business_travel_ground(travel_mode,date,value,unit,measurement_quality,mileage_allowance,created_by)
            VALUES('Bil',DATE '2026-01-01',10,'km','measured',-1,$1)
            """, user);
    }
}
