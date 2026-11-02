namespace Swapping.Api.Hosting;

/// <summary>
/// Refuses request bodies over the limit (4 KB) with 400 <c>InvalidBody</c>, with or without a
/// <c>Content-Length</c> header. Bodies under the limit are buffered so endpoints read them normally.
/// </summary>
public sealed class BodySizeLimit(RequestDelegate next)
{
    public const int MaxBytes = 4096;

    public async Task InvokeAsync(HttpContext context)
    {
        var length = context.Request.ContentLength;
        if (length > MaxBytes)
        {
            await ApiProblem.WriteAsync(context, StatusCodes.Status400BadRequest, "InvalidBody");
            return;
        }

        if (length is null && HasBody(context.Request))
        {
            var buffer = new MemoryStream();
            var chunk = new byte[1024];
            int read;
            while ((read = await context.Request.Body.ReadAsync(chunk, context.RequestAborted)) > 0)
            {
                buffer.Write(chunk, 0, read);
                if (buffer.Length > MaxBytes)
                {
                    await ApiProblem.WriteAsync(context, StatusCodes.Status400BadRequest, "InvalidBody");
                    return;
                }
            }

            buffer.Position = 0;
            context.Request.Body = buffer;
            context.Request.ContentLength = buffer.Length;
        }

        await next(context);
    }

    private static bool HasBody(HttpRequest request) =>
        !HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method) &&
        !HttpMethods.IsOptions(request.Method);
}
