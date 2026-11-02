using Microsoft.AspNetCore.Mvc;

namespace Swapping.Api.Hosting;

/// <summary>
/// Error answers of the API: <c>application/problem+json</c> with a stable <c>code</c> field.
/// Never carries exception details.
/// </summary>
public static class ApiProblem
{
    public const string ContentType = "application/problem+json";

    public static IResult Result(int status, string code, string? detail = null) =>
        Results.Problem(statusCode: status, title: TitleFor(status), detail: detail,
            extensions: new Dictionary<string, object?> { ["code"] = code });

    public static async Task WriteAsync(HttpContext context, int status, string code)
    {
        context.Response.StatusCode = status;
        await Result(status, code).ExecuteAsync(context);
    }

    /// <summary>Default code for answers that no endpoint produced (404 route, 405, 500…).</summary>
    public static string DefaultCode(int status) => status switch
    {
        StatusCodes.Status400BadRequest => "InvalidBody",
        StatusCodes.Status401Unauthorized => "InvalidToken",
        StatusCodes.Status404NotFound => "NotFound",
        StatusCodes.Status405MethodNotAllowed => "MethodNotAllowed",
        StatusCodes.Status415UnsupportedMediaType => "InvalidBody",
        StatusCodes.Status429TooManyRequests => "TooManyRequests",
        _ => "ServerError",
    };

    /// <summary>Adds the default code when missing and removes anything that could leak internals.</summary>
    public static void Customize(ProblemDetailsContext context)
    {
        var problem = context.ProblemDetails;
        problem.Extensions.Remove("exception");
        problem.Extensions.Remove("errors");
        // No per-request id: two refusals that must look the same (unknown name, wrong code) are byte-equal.
        problem.Extensions.Remove("traceId");
        problem.Extensions.TryAdd("code", DefaultCode(problem.Status ?? context.HttpContext.Response.StatusCode));
        if (problem.Status >= 500)
            problem.Detail = null;
    }

    private static string TitleFor(int status) => status switch
    {
        StatusCodes.Status400BadRequest => "Bad request",
        StatusCodes.Status401Unauthorized => "Unauthorized",
        StatusCodes.Status404NotFound => "Not found",
        StatusCodes.Status409Conflict => "Conflict",
        StatusCodes.Status422UnprocessableEntity => "Unprocessable result",
        StatusCodes.Status423Locked => "Locked",
        StatusCodes.Status429TooManyRequests => "Too many requests",
        _ => "Error",
    };
}
