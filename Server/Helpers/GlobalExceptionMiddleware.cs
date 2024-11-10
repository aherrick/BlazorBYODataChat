using System.Net;
using System.Net.Mime;
using System.Text.Json;
using Shared;

namespace Server.Helpers;

public class GlobalExceptionMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = MediaTypeNames.Application.Json;

        var apiEx = new ApiEx();

        switch (exception)
        {
            case UnsupportedMediaTypeException:
                context.Response.StatusCode = (int)HttpStatusCode.UnsupportedMediaType;
                break;

            case ApplicationException ex:
                if (ex.Message.Contains("Invalid Token"))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    break;
                }
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                break;

            case UnauthorizedAccessException:
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                break;

            case NotImplementedException:
                context.Response.StatusCode = (int)HttpStatusCode.NotImplemented;
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                break;
        }

        apiEx.Message = exception.Message;

        await context.Response.WriteAsync(JsonSerializer.Serialize(apiEx));
    }
}