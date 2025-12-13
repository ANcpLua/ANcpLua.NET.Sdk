using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ANcpLua.AspNetCore.ServiceDefaults;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder UseANcpLuaConventions(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<ANcpLuaServiceDefaultsOptions>();
        return builder;
    }
}
