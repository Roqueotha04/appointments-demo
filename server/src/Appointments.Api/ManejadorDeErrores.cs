using Appointments.Application;
using Microsoft.AspNetCore.Diagnostics;

namespace Appointments.Api;

public sealed class ManejadorDeErrores : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not ReglaDeNegocioException regla)
            return false;

        httpContext.Response.StatusCode = regla.Status;
        await httpContext.Response.WriteAsJsonAsync(new
        {
            title = regla.Titulo,
            detail = regla.Message,
            status = regla.Status
        }, cancellationToken);
        return true;
    }
}
