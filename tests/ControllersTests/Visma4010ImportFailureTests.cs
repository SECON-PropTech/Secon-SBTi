using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SeconSbtiBackend.Controllers;
using SeconSbtiBackend.Importing.Visma4010;

namespace tests.ControllersTests;

public sealed class Visma4010ImportFailureTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Unexpected_failure_returns_a_clear_error_without_internal_details(bool readFailure)
    {
        var service = Substitute.For<IVisma4010ImportService>();
        Exception failure = readFailure
            ? new IOException("Internal storage details")
            : new DbUpdateException("Internal database details");

        service.ImportAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<Visma4010ImportResult>(failure));

        using var services = new ServiceCollection().AddLogging().AddControllers().Services.BuildServiceProvider();
        var controller = CreateController(service, services);
        using var stream = new MemoryStream([1, 2, 3]);
        var file = new FormFile(stream, 0, stream.Length, "file", "test.xlsx");

        var response = await controller.Import(file, CancellationToken.None);

        var result = Assert.IsType<ObjectResult>(response.Result);
        var problem = Assert.IsType<ProblemDetails>(result.Value);
        Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
        Assert.Equal("Importen kunde inte slutföras.", problem.Title);
        Assert.Contains("Förhandsgranska filen igen", problem.Detail!);
        Assert.DoesNotContain("Internal", problem.Detail!);
    }

    [Fact]
    public async Task Cancellation_is_not_converted_to_a_server_error()
    {
        var service = Substitute.For<IVisma4010ImportService>();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        service.ImportAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromCanceled<Visma4010ImportResult>(cancellation.Token));

        using var services = new ServiceCollection().AddLogging().AddControllers().Services.BuildServiceProvider();
        var controller = CreateController(service, services);
        using var stream = new MemoryStream([1, 2, 3]);
        var file = new FormFile(stream, 0, stream.Length, "file", "test.xlsx");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => controller.Import(file, cancellation.Token));
    }

    private static Visma4010ImportsController CreateController(
        IVisma4010ImportService service,
        IServiceProvider services)
    {
        var controller = new Visma4010ImportsController(service);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                RequestServices = services,
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())], "Test"))
            }
        };
        return controller;
    }
}
