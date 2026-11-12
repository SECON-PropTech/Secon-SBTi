using tests.DatabaseTests.PostgresTests.Infrastructure;

namespace tests.DatabaseTests.PostgresTests.Constraints;

public sealed class AuditLogTests(PostgresFixture fixture) : DbTestBase(fixture)
{
    [Theory]
    [InlineData("create")]
    [InlineData("")]
    public Task Audit_action_must_be_insert_update_or_delete(string action) =>
        AssertRejectedAsync(CheckViolation, """
            INSERT INTO audit_log(table_name,record_id,action)
            VALUES('purchased_good','123',$1)
            """, action);

    [Fact]
    public async Task Audit_log_accepts_jsonb_and_optional_actor()
    {
        var id = await ScalarAsync<Guid>("""
            INSERT INTO audit_log(table_name,record_id,action,old_values,new_values)
            VALUES('purchased_good','abc','update','{"value":1}'::jsonb,'{"value":2}'::jsonb)
            RETURNING id
            """);
        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task Audit_actor_must_exist_when_supplied()
    {
        await AssertRejectedAsync(ForeignKeyViolation, """
            INSERT INTO audit_log(table_name,record_id,action,changed_by)
            VALUES('purchased_good','abc','insert',$1)
            """, Guid.NewGuid());
    }
}
