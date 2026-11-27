using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SeconSbtiBackend.Controllers;
using SeconSbtiBackend.Importing.Visma4010;

namespace tests.ControllersTests;

public class Visma4010ImportsControllerTests
{
    private readonly IVisma4010ImportService _importService =
        Substitute.For<IVisma4010ImportService>();

    private Visma4010ImportsController CreateController(Guid? userId = null)
    {
        var controller = new Visma4010ImportsController(_importService);
        var claims = userId.HasValue
            ? new[] { new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()) }
            : [];

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
            }
        };

        return controller;
    }

    private static IFormFile CreateFile(string fileName = "visma.xlsx")
    {
        var content = new MemoryStream([1, 2, 3]);
        return new FormFile(content, 0, content.Length, "file", fileName);
    }

    private static Visma4010ImportResponse CreateResponse(
        List<string>? blockers = null)
    {
        return new Visma4010ImportResponse
        {
            FileName = "visma.xlsx",
            Accounts = [],
            Blockers = blockers ?? [],
            Warnings = [],
            Issues = [],
            ToImport = new Visma4010RowsSummary(0, 0),
            AlreadyImported = new Visma4010RowsSummary(0, 0),
            WithoutSupplier = new Visma4010RowsSummary(0, 0),
            NewSuppliers = [],
            RowsWithoutSupplier = [],
            CanImport = blockers is null or { Count: 0 }
        };
    }

    [Fact]
    public async Task Preview_should_return_bad_request_when_file_is_missing()
    {
        var controller = CreateController();
        var result = await controller.Preview(null, CancellationToken.None);
        result.Result.Should().BeOfType<BadRequestObjectResult>();

        await _importService.DidNotReceive()
            .PreviewAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Preview_should_return_ok_when_service_returns_ok()
    {
        var response = CreateResponse();
        _importService.PreviewAsync(
                Arg.Any<Stream>(),
                "visma.xlsx",
                Arg.Any<CancellationToken>())
            .Returns(new Visma4010ImportResult(Visma4010ImportStatus.Ok, response));

        var controller = CreateController();
        var result = await controller.Preview(CreateFile(), CancellationToken.None);
        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        objectResult.Value.Should().BeSameAs(response);
    }

    [Fact]
    public async Task Preview_should_return_conflict_when_file_was_already_imported()
    {
        var response = CreateResponse([Visma4010Blockers.FileAlreadyImported]);

        _importService.PreviewAsync(
                Arg.Any<Stream>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(new Visma4010ImportResult(Visma4010ImportStatus.Blocked, response));

        var controller = CreateController();
        var result = await controller.Preview(CreateFile(), CancellationToken.None);
        result.Result.Should().BeOfType<ConflictObjectResult>()
            .Which.Value.Should().BeSameAs(response);
    }

    [Fact]
    public async Task Preview_should_return_unprocessable_entity_when_import_is_blocked()
    {
        var response = CreateResponse(["ReconciliationFailed"]);
        _importService.PreviewAsync(
                Arg.Any<Stream>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(new Visma4010ImportResult(Visma4010ImportStatus.Blocked, response));

        var controller = CreateController();
        var result = await controller.Preview(CreateFile(), CancellationToken.None);
        result.Result.Should().BeOfType<UnprocessableEntityObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
    }

    [Fact]
    public async Task Preview_should_return_bad_request_when_file_is_invalid()
    {
        const string message = "Filen kunde inte läsas.";

        _importService.PreviewAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new Visma4010ImportResult(Visma4010ImportStatus.InvalidFile, Message: message));

        var controller = CreateController();
        var result = await controller.Preview(CreateFile(), CancellationToken.None);
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Import_should_return_unauthorized_when_user_id_is_missing()
    {
        var controller = CreateController();
        var result = await controller.Import(CreateFile(), CancellationToken.None);
        result.Result.Should().BeOfType<UnauthorizedObjectResult>();

        await _importService.DidNotReceive().ImportAsync(
            Arg.Any<Stream>(), 
            Arg.Any<string>(), 
            Arg.Any<Guid>(), 
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Import_should_return_unauthorized_when_user_id_is_not_a_guid()
    {
        var controller = CreateController();
        controller.ControllerContext.HttpContext.User =
            new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "not-a-guid")],
                "Test"));

        var result = await controller.Import(CreateFile(), CancellationToken.None);
        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Import_should_return_unauthorized_when_user_id_is_empty_guid()
    {
        var controller = CreateController(Guid.Empty);
        var result = await controller.Import(CreateFile(), CancellationToken.None);
        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Import_should_return_bad_request_when_file_is_missing()
    {
        var controller = CreateController(Guid.NewGuid());
        var result = await controller.Import(null, CancellationToken.None);
        result.Result.Should().BeOfType<BadRequestObjectResult>();

        await _importService.DidNotReceive()
            .ImportAsync(
                Arg.Any<Stream>(), 
                Arg.Any<string>(), 
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Import_should_return_created_when_service_returns_ok()
    {
        var userId = Guid.NewGuid();
        var response = CreateResponse();

        _importService.ImportAsync(Arg.Any<Stream>(), "visma.xlsx", userId, Arg.Any<CancellationToken>())
            .Returns(new Visma4010ImportResult(Visma4010ImportStatus.Ok, response));

        var controller = CreateController(userId);
        var result = await controller.Import(CreateFile(), CancellationToken.None);
        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        objectResult.Value.Should().BeSameAs(response);
    }

    [Fact]
    public async Task Import_should_return_conflict_when_file_was_already_imported()
    {
        var userId = Guid.NewGuid();
        var response = CreateResponse([Visma4010Blockers.FileAlreadyImported]);

        _importService.ImportAsync(Arg.Any<Stream>(), Arg.Any<string>(), userId, Arg.Any<CancellationToken>())
            .Returns(new Visma4010ImportResult(Visma4010ImportStatus.Blocked, response));

        var controller = CreateController(userId);
        var result = await controller.Import(CreateFile(), CancellationToken.None);

        result.Result.Should().BeOfType<ConflictObjectResult>()
            .Which.Value.Should().BeSameAs(response);
    }

    [Fact]
    public async Task Import_should_return_unprocessable_entity_when_import_is_blocked()
    {
        var userId = Guid.NewGuid();
        var response = CreateResponse(["ReconciliationFailed"]);

        _importService.ImportAsync(Arg.Any<Stream>(), Arg.Any<string>(), userId, Arg.Any<CancellationToken>())
            .Returns(new Visma4010ImportResult(Visma4010ImportStatus.Blocked, response));

        var controller = CreateController(userId);
        var result = await controller.Import(CreateFile(), CancellationToken.None);

        result.Result.Should().BeOfType<UnprocessableEntityObjectResult>()
            .Which.Value.Should().BeSameAs(response);
    }

    [Fact]
    public async Task Import_should_return_unauthorized_when_service_returns_invalid_user()
    {
        var userId = Guid.NewGuid();

        _importService.ImportAsync(Arg.Any<Stream>(), Arg.Any<string>(), userId, Arg.Any<CancellationToken>())
            .Returns(new Visma4010ImportResult(
                Visma4010ImportStatus.InvalidUser,
                Message: "Användarkontot finns inte längre."));

        var controller = CreateController(userId);
        var result = await controller.Import(CreateFile(), CancellationToken.None);
        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Import_should_return_bad_request_when_file_is_invalid()
    {
        var userId = Guid.NewGuid();
        _importService.ImportAsync(
            Arg.Any<Stream>(),
            Arg.Any<string>(),
            userId,
            Arg.Any<CancellationToken>()).Returns(new Visma4010ImportResult(
                Visma4010ImportStatus.InvalidFile,
                Message: "Filen är tom."));

        var controller = CreateController(userId);
        var result = await controller.Import(CreateFile(), CancellationToken.None);

        var objectResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        var problem = objectResult.Value.Should().BeOfType<ValidationProblemDetails>().Subject;
        problem.Title.Should().Be("En eller flera uppgifter behöver rättas.");
        problem.Errors.Should().ContainKey("file");
        problem.Errors["file"].Should().Contain("Filen är tom.");
    }
}
