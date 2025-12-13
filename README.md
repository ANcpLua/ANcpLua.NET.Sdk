# ANcpLua.NET.Sdk

- [![ANcpLua.NET.Sdk on NuGet](https://img.shields.io/nuget/v/ANcpLua.NET.Sdk.svg)](https://www.nuget.org/packages/ANcpLua.NET.Sdk/)

MSBuild SDK that provides:

- Opinionated defaults for .NET projects
- Naming conventions
- Static analysis with Roslyn analyzers
- Set `ContinuousIntegrationBuild` based on the context
- dotnet test features
    - Dump on crash or hang
    - Loggers when running on GitHub
    - Disable Roslyn analyzers to speed up build
- Relevant NuGet packages based on the project type

# Usage

## Recommended (Layering)

To use it, create a `global.json` file at the solution root with the following content:

````json
{
  "sdk": {
    "version": "10.0.100"
  },
  "msbuild-sdks": {
    "ANcpLua.NET.Sdk": "1.0.0"
  }
}
````

Add the SDK once via `Directory.Build.props`:

````xml
<Project>
  <Sdk Name="ANcpLua.NET.Sdk" Version="1.0.0" />
</Project>
````

Your projects keep using the standard Microsoft SDKs:

````xml
<Project Sdk="Microsoft.NET.Sdk">
</Project>
````

````xml
<Project Sdk="Microsoft.NET.Sdk.Web">
</Project>
````

## Alternative (Root SDK)

You can also use the SDK directly in the project file:

````xml
<Project Sdk="ANcpLua.NET.Sdk/1.0.0">
</Project>
````
