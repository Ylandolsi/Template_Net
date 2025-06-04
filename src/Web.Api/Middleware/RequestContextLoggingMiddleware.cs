using Microsoft.Extensions.Primitives;
using Serilog.Context;

namespace Web.Api.Middleware;

public class RequestContextLoggingMiddleware(RequestDelegate next)
{
    // This middleware is used to log the correlation ID for each request.

    // all the logs  from the same HTTP Request will have the same CorrelationId property

    // helpful for debugging and tracing requests  
    private const string CorrelationIdHeaderName = "Correlation-Id";

    public Task Invoke(HttpContext context)
    {
        using (LogContext.PushProperty("CorrelationId", GetCorrelationId(context)))
        {
            return next.Invoke(context);
        }
    }

    private static string GetCorrelationId(HttpContext context)
    {
        context.Request.Headers.TryGetValue(
            CorrelationIdHeaderName,
            out StringValues correlationId);

        return correlationId.FirstOrDefault() ?? context.TraceIdentifier;
        //     ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^    ^^^^^^^^^^^^^^^^^^^^
        //     From HTTP header (if provided)     ASP.NET Core TraceIdentifier ( automatically generated )
    }

    // REMARK: This middleware should be registered before SerilogRequestLoggingMiddleware

    // EXP : 
        /* All logs for a slow request 
        => help identify the time spent in each part of the request:

            CorrelationId = "slow-req-456"

            10:30:01.000 - Request started
            10:30:01.500 - Database query took 500ms (SLOW!)
            10:30:02.000 - Request completed (1000ms total)
        */
}
