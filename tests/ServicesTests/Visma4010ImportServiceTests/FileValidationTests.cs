using FluentAssertions;
using SeconSbtiBackend.Importing.Visma4010;
using static tests.ServicesTests.Visma4010ImportServiceTests.Visma4010ImportServiceTestHelpers;

namespace tests.ServicesTests.Visma4010ImportServiceTests;

/// <summary>
/// Files that are rejected before any import data is analyzed (extension, size, content, read errors).
/// </summary>
public class FileValidationTests
{
    /************************Preview************************/
    [Fact]
    public async Task Preview_should_return_invalid_file_when_extension_is_not_supported()
    {
        var service = CreateService();
        await using var content = new MemoryStream([1, 2, 3]);

        var result = await service.PreviewAsync(content, "visma.pdf", CancellationToken.None);
        result.Status.Should().Be(Visma4010ImportStatus.InvalidFile);
        result.Message.Should().Be("Endast .xlsx- och .xls-filer stöds.");
        result.Response.Should().BeNull();
    }

    [Fact]
    public async Task Preview_should_return_invalid_file_when_file_is_empty()
    {
        var service = CreateService();
        await using var content = new MemoryStream();

        var result = await service.PreviewAsync(content, "visma.xlsx", CancellationToken.None);
        result.Status.Should().Be(Visma4010ImportStatus.InvalidFile);
        result.Message.Should().Be("Filen är tom.");
        result.Response.Should().BeNull();
    }

    [Fact]
    public async Task Preview_should_return_invalid_file_when_file_is_too_large()
    {
        var service = CreateService();
        var bytes = new byte[Visma4010ImportService.MaxFileSize + 1];
        await using var content = new MemoryStream(bytes);

        var result = await service.PreviewAsync(content, "visma.xlsx", CancellationToken.None);
        result.Status.Should().Be(Visma4010ImportStatus.InvalidFile);
        result.Message.Should().Be("Filen är större än 10 MB.");
        result.Response.Should().BeNull();
    }

    [Theory]
    [InlineData("visma.txt")]
    [InlineData("visma.csv")]
    [InlineData("visma.pdf")]
    [InlineData("visma")]
    public async Task Preview_should_reject_unsupported_file_extensions(string fileName)
    {
        var service = CreateService();
        await using var content = new MemoryStream([1, 2, 3]);

        var result = await service.PreviewAsync(content, fileName, CancellationToken.None);
        result.Status.Should().Be(Visma4010ImportStatus.InvalidFile);
        result.Message.Should().Be("Endast .xlsx- och .xls-filer stöds.");
    }

    [Theory]
    [InlineData("visma.XLSX")]
    [InlineData("visma.XLS")]
    public async Task Preview_should_accept_supported_extensions_regardless_of_case(string fileName)
    {
        // The extension is accepted, but the content is not a valid Excel file,
        // so the error comes from Excel reading, not from extension validation.
        var service = CreateService();
        await using var content = new MemoryStream([1, 2, 3]);

        var result = await service.PreviewAsync(content, fileName, CancellationToken.None);
        result.Status.Should().Be(Visma4010ImportStatus.InvalidFile);
        result.Message.Should().NotBe("Endast .xlsx- och .xls-filer stöds.");
    }

    [Theory]
    [InlineData("folder/visma.pdf")]
    [InlineData("folder\\visma.pdf")]
    public async Task Preview_should_validate_extension_of_file_name(string fileName)
    {
        var service = CreateService();
        await using var content = new MemoryStream([1, 2, 3]);

        var result = await service.PreviewAsync(content, fileName, CancellationToken.None);
        result.Status.Should().Be(Visma4010ImportStatus.InvalidFile);
        result.Message.Should().Be("Endast .xlsx- och .xls-filer stöds.");
    }

    [Fact]
    public async Task Preview_should_propagate_cancellation()
    {
        var service = CreateService();
        await using var content = new MemoryStream([1, 2, 3]);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var act = () => service.PreviewAsync(content, "visma.xlsx", cancellation.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Preview_should_propagate_stream_read_errors()
    {
        var service = CreateService();
        await using var content = new ThrowingReadStream();

        var act = () => service.PreviewAsync(content, "visma.xlsx", CancellationToken.None);
        await act.Should().ThrowAsync<IOException>();
    }

    /************************Import************************/
    [Fact]
    public async Task Import_should_return_invalid_file_when_extension_is_not_supported()
    {
        await using var context = TestDatabase.Create();
        var user = await SeedUserAsync(context);
        var service = CreateService(context);
        await using var content = new MemoryStream([1, 2, 3]);

        var result = await service.ImportAsync(content, "visma.pdf", user.Id, CancellationToken.None);

        result.Status.Should().Be(Visma4010ImportStatus.InvalidFile);
        result.Message.Should().Be("Endast .xlsx- och .xls-filer stöds.");
        result.Response.Should().BeNull();
        context.ImportBatches.Should().BeEmpty();
    }

    [Fact]
    public async Task Import_should_return_invalid_file_when_content_is_not_an_excel_file()
    {
        await using var context = TestDatabase.Create();
        var user = await SeedUserAsync(context);
        var service = CreateService(context);
        await using var content = new MemoryStream([1, 2, 3]);

        var result = await service.ImportAsync(content, "visma.xlsx", user.Id, CancellationToken.None);

        result.Status.Should().Be(Visma4010ImportStatus.InvalidFile);
        result.Message.Should().Be("Filen kunde inte läsas som en Excel-fil.");
    }
}