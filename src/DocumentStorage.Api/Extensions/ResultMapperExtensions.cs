using DocumentStorage.Shared.Contracts;
using DocumentStorage.Shared.Results;
using Microsoft.AspNetCore.Mvc;

namespace DocumentStorage.Api.Extensions;

/// <summary>
/// Extension methods to map Result types to HTTP responses with ApiResponse envelope.
/// </summary>
public static class ResultMapperExtensions
{
    /// <summary>
    /// Map Result (non-generic) to IActionResult with configurable success status code.
    /// </summary>
    public static IActionResult ToActionResult(this ControllerBase controller, Result result, int successStatus = 200)
    {
        if (result.IsSuccess)
        {
            if (successStatus == 204)
                return controller.NoContent();

            var response = ApiResponse.Ok();
            return controller.Ok(response);
        }

        return MapFailureResult(controller, result.Errors);
    }

    /// <summary>
    /// Map Result{T} to IActionResult with configurable success status code.
    /// </summary>
    public static IActionResult ToActionResult<T>(
        this ControllerBase controller,
        Result<T> result,
        int successStatus = 200)
    {
        if (result.IsSuccess)
        {
            if (successStatus == 204)
                return controller.NoContent();

            var response = ApiResponse<T>.Ok(data: result.Value);
            return successStatus switch
            {
                200 => controller.Ok(response),
                201 => controller.Created(string.Empty, response),
                _ => new ObjectResult(response) { StatusCode = successStatus }
            };
        }

        return MapFailureResult(controller, result.Errors);
    }

    private static IActionResult MapFailureResult(ControllerBase controller, IReadOnlyList<AppError> errors)
    {
        var statusCode = DetermineStatusCode(errors);

        var errorResponses = errors.Select(e => new ErrorResponse
        {
            Code = e.Code,
            Message = e.Message,
            Target = e.Detail
        }).ToArray();

        var response = ApiResponse<object>.Fail(
            message: errors.First().Message,
            errors: errorResponses);

        return new ObjectResult(response) { StatusCode = statusCode };
    }

    private static int DetermineStatusCode(IReadOnlyList<AppError> errors)
    {
        if (errors.Any(e => e.Type == ErrorType.Unauthorized))
            return 401;

        if (errors.Any(e => e.Type == ErrorType.Forbidden))
            return 403;

        if (errors.Any(e => e.Type == ErrorType.NotFound))
            return 404;

        if (errors.Any(e => e.Type == ErrorType.Conflict))
            return 409;

        if (errors.Any(e => e.Type == ErrorType.Validation))
            return 422;

        return 400;
    }
}
