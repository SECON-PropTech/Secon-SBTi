using tests.DatabaseTests.PostgresTests.Infrastructure;

namespace tests.DatabaseTests.PostgresTests.Constraints;

public sealed class SoldProductTests(PostgresFixture fixture) : DbTestBase(fixture)
{
    [Fact]
    public async Task Product_name_must_not_be_blank_and_numeric_assumptions_are_valid()
    {
        await AssertRejectedAsync(CheckViolation, "INSERT INTO sold_product(name) VALUES('   ')");
        await AssertRejectedAsync(CheckViolation, "INSERT INTO sold_product(name,kwh_per_year) VALUES('Valid product',-1)");
        await AssertRejectedAsync(CheckViolation, "INSERT INTO sold_product(name,lifetime_years) VALUES('Another product',0)");
    }

    [Fact]
    public async Task Article_number_is_unique()
    {
        await ExecAsync("INSERT INTO sold_product(article_no,name) VALUES('ART-1','Product one')");
        await AssertRejectedAsync(UniqueViolation,
            "INSERT INTO sold_product(article_no,name) VALUES('ART-1','Product two')");
    }

    [Fact]
    public async Task Product_material_weight_must_be_nonnegative()
    {
        var product = await ScalarAsync<Guid>("INSERT INTO sold_product(name) VALUES($1) RETURNING id", Unique("product"));
        var treatment = await InsertWasteTreatmentAsync();
        var material = await ScalarAsync<Guid>("INSERT INTO material(name,eol_treatment_id) VALUES($1,$2) RETURNING id",
            Unique("material"), treatment);
        await AssertRejectedAsync(CheckViolation,
            "INSERT INTO sold_product_material(product_id,material_id,weight_kg) VALUES($1,$2,-1)", product, material);
    }

    [Fact]
    public async Task V2_measurement_quality_defaults_to_estimated_and_is_checked()
    {
        var id = await ScalarAsync<Guid>("INSERT INTO sold_product(name) VALUES($1) RETURNING id", Unique("product"));
        Assert.Equal("estimated", await ScalarAsync<string>("SELECT measurement_quality FROM sold_product WHERE id=$1", id));
        await AssertRejectedAsync(CheckViolation,
            "INSERT INTO sold_product(name,measurement_quality) VALUES($1,'unknown')", Unique("product"));
    }
}
