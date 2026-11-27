using FluentAssertions;
using SeconSbtiBackend.Importing.Visma4010;

namespace tests.ImportingTests.Visma4010;

public class Visma4010ImportResultTests
{
    [Fact]
    public void Should_create_result_with_status_only()
    {
        var result = new Visma4010ImportResult(
             Visma4010ImportStatus.Ok);

        result.Status.Should().Be(Visma4010ImportStatus.Ok);
        result.Response.Should().BeNull();
        result.Message.Should().BeNull();
    }

    [Fact]
    public void Should_create_result_with_all_properties()
    {
        var response = new Visma4010ImportResponse
        {
            FileName = "visma.xlsx",
            Accounts = [],
            Blockers = [],
            Warnings = [],
            Issues = [],
            ToImport = new Visma4010RowsSummary(0, 0),
            AlreadyImported = new Visma4010RowsSummary(0, 0),
            WithoutSupplier = new Visma4010RowsSummary(0, 0),
            NewSuppliers = [],
            RowsWithoutSupplier = []
        };

        var result = new Visma4010ImportResult(
            Visma4010ImportStatus.Ok,
            response,
            "Importen lyckades.");

        result.Status.Should().Be(Visma4010ImportStatus.Ok);
        result.Response.Should().BeSameAs(response);
        result.Message.Should().Be("Importen lyckades.");
    }

    [Fact]
    public void Should_support_record_equality()
    {
        var first = new Visma4010ImportResult(
            Visma4010ImportStatus.Ok,
            null,
            "Klart");

        var second = new Visma4010ImportResult(
            Visma4010ImportStatus.Ok,
            null,
            "Klart");

        first.Should().Be(second);
    }
}