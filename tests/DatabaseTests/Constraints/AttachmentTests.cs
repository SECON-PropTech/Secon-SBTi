using tests.DatabaseTests.Infrastructure;

namespace tests.DatabaseTests.Constraints;

public sealed class AttachmentTests(PostgresFixture fixture) : DbTestBase(fixture)
{
    [Fact]
    public async Task Exactly_one_parent_reference_is_required()
    {
        var user = await InsertUserAsync();
        await AssertRejectedAsync(CheckViolation, """
            INSERT INTO attachment(file_name,storage_path,uploaded_by)
            VALUES('a.pdf','/tmp/a.pdf',$1)
            """, user);

        var good = await InsertPurchasedGoodAsync();
        await AssertRejectedAsync(CheckViolation, """
            INSERT INTO attachment(file_name,storage_path,uploaded_by,purchased_good_id,waste_id)
            VALUES('a.pdf','/tmp/a.pdf',$1,$2,$3)
            """, user, good, Guid.NewGuid());
    }

    [Fact]
    public async Task Attachment_parent_and_uploader_foreign_keys_are_enforced()
    {
        var user = await InsertUserAsync();
        await AssertRejectedAsync(ForeignKeyViolation, """
            INSERT INTO attachment(file_name,storage_path,uploaded_by,purchased_good_id)
            VALUES('a.pdf','/tmp/a.pdf',$1,$2)
            """, user, Guid.NewGuid());

        var good = await InsertPurchasedGoodAsync();
        await AssertRejectedAsync(ForeignKeyViolation, """
            INSERT INTO attachment(file_name,storage_path,uploaded_by,purchased_good_id)
            VALUES('a.pdf','/tmp/a.pdf',$1,$2)
            """, Guid.NewGuid(), good);
    }

    [Fact]
    public async Task Deleting_parent_cascades_to_attachment()
    {
        var user = await InsertUserAsync();
        var good = await InsertPurchasedGoodAsync();
        var id = await ScalarAsync<Guid>("""
            INSERT INTO attachment(file_name,storage_path,uploaded_by,purchased_good_id)
            VALUES('a.pdf','/tmp/a.pdf',$1,$2) RETURNING id
            """, user, good);
        await ExecAsync("DELETE FROM purchased_good WHERE id=$1", good);
        Assert.Equal(0L, await ScalarAsync<long>("SELECT count(*) FROM attachment WHERE id=$1", id));
    }
}
