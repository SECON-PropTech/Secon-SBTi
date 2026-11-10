using tests.DatabaseTests.Infrastructure;

namespace tests.DatabaseTests.Constraints;

public sealed class WasteTests(PostgresFixture fixture) : DbTestBase(fixture)
{
    [Fact]
    public async Task Weight_must_not_be_negative()
    {
        var user = await InsertUserAsync();
        var type = await InsertWasteTypeAsync();
        var treatment = await InsertWasteTreatmentAsync();
        await AssertRejectedAsync(CheckViolation, """
            INSERT INTO waste(waste_type_id,waste_treatment_id,date,weight_kg,measurement_quality,created_by)
            VALUES($1,$2,DATE '2026-01-01',-0.1,'measured',$3)
            """, type, treatment, user);
    }

    [Fact]
    public async Task Waste_requires_type_treatment_and_creator()
    {
        var user = await InsertUserAsync();
        var type = await InsertWasteTypeAsync();
        var treatment = await InsertWasteTreatmentAsync();
        await AssertRejectedAsync(ForeignKeyViolation, """
            INSERT INTO waste(waste_type_id,waste_treatment_id,date,weight_kg,measurement_quality,created_by)
            VALUES($1,$2,DATE '2026-01-01',1,'measured',$3)
            """, Guid.NewGuid(), treatment, user);
        await AssertRejectedAsync(ForeignKeyViolation, """
            INSERT INTO waste(waste_type_id,waste_treatment_id,date,weight_kg,measurement_quality,created_by)
            VALUES($1,$2,DATE '2026-01-01',1,'measured',$3)
            """, type, treatment, Guid.NewGuid());
    }

    [Fact]
    public async Task Waste_measurement_quality_is_checked()
    {
        var user = await InsertUserAsync();
        var type = await InsertWasteTypeAsync();
        var treatment = await InsertWasteTreatmentAsync();
        await AssertRejectedAsync(CheckViolation, """
            INSERT INTO waste(waste_type_id,waste_treatment_id,date,weight_kg,measurement_quality,created_by)
            VALUES($1,$2,DATE '2026-01-01',1,'unknown',$3)
            """, type, treatment, user);
    }
}
