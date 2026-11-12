using tests.DatabaseTests.PostgresTests.Infrastructure;

namespace tests.DatabaseTests.PostgresTests.Constraints;

public sealed class UserRoleTests(PostgresFixture fixture) : DbTestBase(fixture)
{
    [Fact]
    public async Task UserRole_requires_existing_user_and_role()
    {
        var role = await InsertRoleAsync();
        await AssertRejectedAsync(ForeignKeyViolation,
            "INSERT INTO user_role(user_id,role_id) VALUES($1,$2)", Guid.NewGuid(), role);

        var user = await InsertUserAsync();
        await AssertRejectedAsync(ForeignKeyViolation,
            "INSERT INTO user_role(user_id,role_id) VALUES($1,$2)", user, Guid.NewGuid());
    }

    [Fact]
    public async Task UserRole_rejects_duplicate_pair_and_cascades_on_user_delete()
    {
        var user = await InsertUserAsync();
        var role = await InsertRoleAsync();
        await ExecAsync("INSERT INTO user_role(user_id,role_id) VALUES($1,$2)", user, role);
        await AssertRejectedAsync(UniqueViolation,
            "INSERT INTO user_role(user_id,role_id) VALUES($1,$2)", user, role);

        await ExecAsync("DELETE FROM app_user WHERE id=$1", user);
        Assert.Equal(0L, await ScalarAsync<long>("SELECT count(*) FROM user_role WHERE user_id=$1", user));
    }

}
