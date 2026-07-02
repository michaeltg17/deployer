using Api.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Hosting;
using System.Net;

namespace Api.Extensions;

internal static class ExceptionHandlerExtensions
{
    public static WebApplication UseCustomExceptionHandler(this WebApplication app)
    {
        app.UseExceptionHandler(static config => config.Run(static async httpContext =>
        {
            httpContext.Response.ContentType = "application/problem+json";
            var problemDetailsService = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();

            var env = httpContext.RequestServices.GetRequiredService<IHostEnvironment>();
            var exceptionHandlerFeature = httpContext.Features.GetRequiredFeature<IExceptionHandlerFeature>();
            var exception = exceptionHandlerFeature.Error;

            httpContext.Response.StatusCode = exception switch
            {
                InvalidDeployRequestException => (int)HttpStatusCode.BadRequest,
                BadHttpRequestException => (int)HttpStatusCode.BadRequest,
                _ => (int)HttpStatusCode.InternalServerError,
            };

            var problemDetailsContext = BuildProblemDetailsContext(exception, httpContext, env);

            await problemDetailsService.WriteAsync(problemDetailsContext).ConfigureAwait(false);
        }));

        return app;
    }

static ProblemDetailsContext BuildProblemDetailsContext(Exception exception, HttpContext httpContext, IHostEnvironment env)
{
        var isInternalServerError = httpContext.Response.StatusCode == (int)HttpStatusCode.InternalServerError;
        var hideDetails = isInternalServerError && !env.IsDevelopment();

        return new ProblemDetailsContext
        {
            Exception = hideDetails ? null : exception,
            HttpContext = httpContext,
            ProblemDetails =
            {
                Title = hideDetails ? "InternalServerError" : exception!.GetType().GetNameWithoutGenericArity(),
                Detail = hideDetails ? "Check the logs for more information." : exception!.Message,
                Status = httpContext.Response.StatusCode,
                Instance = httpContext.Request.Path
            }
        };
    }
}
