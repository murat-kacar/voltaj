using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Voltflow.Api.Errors;

namespace Voltflow.Tests;

/// <summary>What a client is told about a failed request: the HTTP status and the problem document that came with it.</summary>
public sealed record HandledFailure(int HttpStatus, JsonElement Problem)
{
    public int Status => Problem.GetProperty("status").GetInt32();
    public string Type => Problem.GetProperty("type").GetString()!;
    public string Detail => Problem.GetProperty("detail").GetString()!;

    /// <summary>The whole document as sent, to look for what must not be in it.</summary>
    public string Text => Problem.GetRawText();

    public void AssertNoneOfThisIsSaid(params string[] internals)
    {
        foreach (var inside in internals)
            Assert.DoesNotContain(inside, Text, StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>Runs an exception through the API's own exception handler, as the pipeline does, without a server or a database.</summary>
public static class ExceptionHandling
{
    public static async Task<HandledFailure> HandleAsync(Exception exception)
    {
        await using var services = new ServiceCollection().AddLogging().AddProblemDetails().BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.Path = "/api/anything";
        using var body = new MemoryStream();
        context.Response.Body = body;

        var handler = new GlobalExceptionHandler(services.GetRequiredService<IProblemDetailsService>(), NullLogger<GlobalExceptionHandler>.Instance);
        Assert.True(await handler.TryHandleAsync(context, exception, CancellationToken.None), "The handler must take the exception, or the framework answers with its own 500.");

        body.Position = 0;
        using var document = await JsonDocument.ParseAsync(body);
        return new HandledFailure(context.Response.StatusCode, document.RootElement.Clone());
    }

    public static string ProblemType(string code) => $"https://voltflow.dev/problems/{code}";
}

// How the API answers an exception that nobody handled on the way: the status, the kind of problem, and that nothing of
// the inside of the server (SQL, constraint names, what the JSON parser said) reaches the client.
[Trait("VUT", "08301")]
public sealed class GlobalExceptionHandlerTests
{
    [Theory]
    [InlineData(StatusCodes.Status400BadRequest)]
    [InlineData(StatusCodes.Status408RequestTimeout)]
    [InlineData(StatusCodes.Status413PayloadTooLarge)]
    [InlineData(StatusCodes.Status415UnsupportedMediaType)]
    public async Task ABadHttpRequest_IsAnsweredWithItsOwnStatus_NotAsAServerError(int statusCode)
    {
        var failure = await ExceptionHandling.HandleAsync(new BadHttpRequestException("The request could not be read.", statusCode));

        Assert.Equal((statusCode, statusCode), (failure.HttpStatus, failure.Status));
        Assert.Equal(ExceptionHandling.ProblemType("invalid_request"), failure.Type);
        Assert.Equal("The request is not valid.", failure.Detail);
    }

    [Fact]
    public async Task ABadHttpRequest_DoesNotRepeatWhatTheFrameworkSaidAboutIt()
    {
        var parser = new JsonException("The JSON value could not be converted to System.String. Path: $.name | LineNumber: 0 | BytePositionInLine: 9.");
        var failure = await ExceptionHandling.HandleAsync(new BadHttpRequestException(
            "Failed to read parameter \"SaveSiteRequest request\" from the request body as JSON.", parser));

        Assert.Equal(StatusCodes.Status400BadRequest, failure.HttpStatus);
        failure.AssertNoneOfThisIsSaid("SaveSiteRequest", "System.String", "LineNumber", "BytePositionInLine", "$.name", "Failed to read");
    }

    [Fact]
    public async Task AUniqueViolation_IsAConflict_AndTheSqlAndTheConstraintStayInside()
    {
        var failure = await ExceptionHandling.HandleAsync(UniqueViolation());

        Assert.Equal((StatusCodes.Status409Conflict, StatusCodes.Status409Conflict), (failure.HttpStatus, failure.Status));
        Assert.Equal(ExceptionHandling.ProblemType("unique_violation"), failure.Type);
        failure.AssertNoneOfThisIsSaid("DocumentCounters", "IX_DocumentCounters_Key", "duplicate key value", "INSERT INTO", "(TK)");
    }

    [Fact]
    public async Task AConcurrencyConflict_IsStillAConcurrencyConflict()
    {
        var failure = await ExceptionHandling.HandleAsync(new DbUpdateConcurrencyException("The row was changed by someone else."));

        Assert.Equal((StatusCodes.Status409Conflict, StatusCodes.Status409Conflict), (failure.HttpStatus, failure.Status));
        Assert.Equal(ExceptionHandling.ProblemType("concurrency_conflict"), failure.Type);
    }

    // both are DbUpdateExceptions, so the answer depends on which of the two the handler looks at first
    [Fact]
    public async Task AConcurrencyConflict_IsNotTakenForAUniqueViolationItHappensToWrap()
    {
        var failure = await ExceptionHandling.HandleAsync(new DbUpdateConcurrencyException("The row was changed by someone else.", UniqueViolation().InnerException));

        Assert.Equal(StatusCodes.Status409Conflict, failure.HttpStatus);
        Assert.Equal(ExceptionHandling.ProblemType("concurrency_conflict"), failure.Type);
    }

    [Theory]
    [InlineData(PostgresErrorCodes.ForeignKeyViolation)]
    [InlineData(PostgresErrorCodes.SerializationFailure)]
    [InlineData(null)]
    public async Task AnyOtherFailureOfTheDatabase_IsStillAServerError(string? sqlState)
    {
        var error = sqlState is null
            ? new DbUpdateException("An error occurred while saving the entity changes.")
            : new DbUpdateException("An error occurred while saving the entity changes.", PostgresFailure(sqlState));

        var failure = await ExceptionHandling.HandleAsync(error);

        Assert.Equal((StatusCodes.Status500InternalServerError, ExceptionHandling.ProblemType("internal_error")), (failure.HttpStatus, failure.Type));
    }

    [Fact]
    public async Task TheAnswersThatWereAlreadyThere_AreUntouched()
    {
        (Exception Error, int Status, string Code)[] table =
        [
            (new ArgumentException("A value is wrong."), 400, "invalid_argument"),
            (new KeyNotFoundException("There is no such thing."), 404, "not_found"),
            (new UnauthorizedAccessException(), 401, "unauthorized"),
            (new InvalidOperationException("Not in this state."), 409, "business_rule_violation"),
            (new OperationCanceledException(), 499, "request_cancelled"),
            (new NotSupportedException("Something nobody planned for."), 500, "internal_error")
        ];

        foreach (var (error, status, code) in table)
        {
            var failure = await ExceptionHandling.HandleAsync(error);
            Assert.True(status == failure.HttpStatus, $"{error.GetType().Name} was answered with {failure.HttpStatus}, not {status}.");
            Assert.Equal(ExceptionHandling.ProblemType(code), failure.Type);
        }
    }

    private static PostgresException PostgresFailure(string sqlState) => new(
        messageText: "duplicate key value violates unique constraint \"IX_DocumentCounters_Key\"",
        severity: "ERROR",
        invariantSeverity: "ERROR",
        sqlState: sqlState,
        detail: "Key (\"Key\")=(TK) already exists.",
        tableName: "DocumentCounters",
        constraintName: "IX_DocumentCounters_Key");

    private static DbUpdateException UniqueViolation() => new(
        "An error occurred while saving the entity changes. INSERT INTO \"DocumentCounters\" (\"Id\", \"Key\") VALUES (@p0, @p1)",
        PostgresFailure(PostgresErrorCodes.UniqueViolation));
}
