using Microsoft.AspNetCore.Http;
using Voltflow.Shared;

namespace Voltflow.Api.Errors;

public static class ApiResults
{
    public static IResult From<T>(this Result<T> result, Func<T, IResult>? success = null)
    {
        if (result.IsSuccess)
            return success?.Invoke(result.Value!) ?? Results.Ok(result.Value);

        var errorCode = result.ErrorCode;
        var typeUri = !string.IsNullOrWhiteSpace(errorCode)
            ? $"urn:voltflow:error:{errorCode}"
            : "https://voltflow.dev/problems/business-validation";

        var extensions = new Dictionary<string, object?>();
        if (!string.IsNullOrWhiteSpace(errorCode))
        {
            extensions["errorCode"] = errorCode;
        }

        return Results.Problem(
            statusCode: StatusCodes.Status422UnprocessableEntity,
            title: "Business validation failed",
            detail: result.Error,
            type: typeUri,
            extensions: extensions.Count > 0 ? extensions : null);
    }

    public static IResult NotFound<T>(this Result<T> result, Func<T, IResult>? success = null)
    {
        if (result.IsSuccess)
            return success?.Invoke(result.Value!) ?? Results.Ok(result.Value);

        return Results.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Resource not found",
            detail: result.Error,
            type: "https://voltflow.dev/problems/not-found");
    }

    public static IResult From(this Result result)
    {
        if (result.IsSuccess)
            return Results.NoContent();

        var errorCode = result.ErrorCode;
        var typeUri = !string.IsNullOrWhiteSpace(errorCode)
            ? $"urn:voltflow:error:{errorCode}"
            : "https://voltflow.dev/problems/business-validation";

        var extensions = new Dictionary<string, object?>();
        if (!string.IsNullOrWhiteSpace(errorCode))
        {
            extensions["errorCode"] = errorCode;
        }

        return Results.Problem(
            statusCode: StatusCodes.Status422UnprocessableEntity,
            title: "Business validation failed",
            detail: result.Error,
            type: typeUri,
            extensions: extensions.Count > 0 ? extensions : null);
    }
}
