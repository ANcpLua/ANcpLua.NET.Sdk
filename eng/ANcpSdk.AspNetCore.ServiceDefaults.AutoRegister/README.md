 # ANcpSdk.AspNetCore.ServiceDefaults.AutoRegister

Roslyn source generator that auto-injects service defaults into ASP.NET Core applications.

## How It Works

This package intercepts `WebApplication.CreateBuilder()` and `app.Run()` calls to automatically:

1. Call `builder.TryUseANcpSdkConventions()` after builder creation
2. Call `app.MapANcpSdkDefaultEndpoints()` before `app.Run()`

## Usage

Used automatically when referencing `ANcpLua.NET.Sdk.Web`. No manual configuration required.

## Opt-Out

Disable auto-registration in your project file:

```xml
<PropertyGroup>
  <AutoRegisterServiceDefaults>false</AutoRegisterServiceDefaults>
</PropertyGroup>
```

Then manually call the extension methods:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.UseANcpSdkConventions();

var app = builder.Build();
app.MapANcpSdkDefaultEndpoints();
app.Run();
```

## License

MIT