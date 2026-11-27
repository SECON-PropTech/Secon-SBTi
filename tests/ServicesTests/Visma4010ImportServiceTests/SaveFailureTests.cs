using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SeconSbtiBackend.Data;
using SeconSbtiBackend.Importing.Visma4010;
using SeconSbtiBackend.Models;
using tests.ImportingTests.Visma4010;
using static tests.ServicesTests.Visma4010ImportServiceTests.Visma4010ImportServiceTestHelpers;

namespace tests.ServicesTests.Visma4010ImportServiceTests;

/// <summary>
/// What happens when saving fails, e.g. because two imports run at the same time.
/// </summary>
public class SaveFailureTests
{
    [Fact]
    public async Task Import_should_return_blocked_when_save_fails_because_another_import_saved_the_same_file()
    {
        var options = NewOptions();
        await using var seedContext = new ApplicationDbContext(options);
        var user = await SeedUserAsync(seedContext);

        var bytes = Visma4010FileBuilder.Standard().ToBytes();
        var hash = HashOf(bytes);

        // Right before our save fails, another import saves the same file.
        await using var context = new FailingSaveDbContext(options, async () =>
        {
            await using var other = new ApplicationDbContext(options);
            other.ImportBatches.Add(new ImportBatch
            {
                Id = Guid.NewGuid(),
                Source = "visma_4010",
                ImportedBy = user.Id,
                CreatedAt = DateTime.UtcNow,
                ImportBatchFiles =
                {
                    new ImportBatchFile { Id = Guid.NewGuid(), Name = "visma4010.xlsx", FileHash = hash }
                }
            });
            await other.SaveChangesAsync();
        });

        var service = CreateService(context);
        await using var file = new MemoryStream(bytes);

        var result = await service.ImportAsync(file, "visma4010.xlsx", user.Id, CancellationToken.None);

        result.Status.Should().Be(Visma4010ImportStatus.Blocked);
        result.Response!.CanImport.Should().BeFalse();
        result.Response.Blockers.Should().Contain(Visma4010Blockers.FileAlreadyImported);
        result.Response.PreviousImport.Should().NotBeNull();
    }

    [Fact]
    public async Task Import_should_rethrow_when_save_fails_and_nothing_explains_why()
    {
        var options = NewOptions();
        await using var seedContext = new ApplicationDbContext(options);
        var user = await SeedUserAsync(seedContext);

        await using var context = new FailingSaveDbContext(options);
        var service = CreateService(context);
        await using var file = Visma4010FileBuilder.Standard().ToStream();

        var act = () => service.ImportAsync(file, "visma4010.xlsx", user.Id, CancellationToken.None);

        await act.Should().ThrowAsync<DbUpdateException>();
    }
}