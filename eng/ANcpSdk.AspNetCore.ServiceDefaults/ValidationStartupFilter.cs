using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace ANcpSdk.AspNetCore.ServiceDefaults;

internal sealed class ValidationStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            var options = app.ApplicationServices.GetService<ANcpSdkServiceDefaultsOptions>();
            if (options is not null && !options.MapCalled)
                throw new InvalidOperationException(
                    $"You must call {nameof(ANcpSdkServiceDefaults.MapANcpSdkDefaultEndpoints)}.");

            next(app);
        };
    }
}
