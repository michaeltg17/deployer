using Api.Exceptions;
using Core.Extensions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using System.Net;

namespace Api.Extensions;

public static class ExceptionHandlerExtensions
{
    public static WebApplication UseCustomExceptionHandler(this WebApplication app)
    {
        app.UseExceptionHandler(config => config.Run(async httpContext =>
        {
            httpContext.Response.ContentType = "application/problem+json";
            var problemDetailsService = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();

            var exceptionHandlerFeature = httpContext.Features.GetRequiredFeature<IExceptionHandlerFeature>();
            var exception = exceptionHandlerFeature.Error;

            httpContext.Response.StatusCode = exception switch
            {
                InvalidDeployRequestException => (int)HttpStatusCode.BadRequest,
                _ => (int)HttpStatusCode.InternalServerError,
            };

            var problemDetailsContext = BuildProblemDetailsContext(exception, httpContext);

            await problemDetailsService.WriteAsync(problemDetailsContext);
        }));

        return app;
    }

    static ProblemDetailsContext BuildProblemDetailsContext(Exception exception, HttpContext httpContext)
    {
        var isInternalServerError = httpContext.Response.StatusCode == (int)HttpStatusCode.InternalServerError;

        return new ProblemDetailsContext
        {
            Exception = isInternalServerError ? null : exception,
            HttpContext = httpContext,
            ProblemDetails =
            {
                Title = isInternalServerError ? "InternalServerError" : exception!.GetType().GetNameWithoutGenericArity(),
                Detail = isInternalServerError ? "Check the logs for more information." : exception!.Message,
                Status = httpContext.Response.StatusCode,
                Instance = httpContext.Request.Path
            }
        };
    }
}
