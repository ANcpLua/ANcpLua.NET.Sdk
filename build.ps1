#!/usr/bin/env pwsh
# Build script for CI

param([Parameter(ValueFromRemainingArguments=$true)]$Args)

$ErrorActionPreference = "Stop"

# Build Analyzers first
dotnet build eng/ANcpLua.Analyzers/ANcpLua.Analyzers.csproj -c Release

# Pack NuGet packages
dotnet pack src/ANcpLua.NET.Sdk.csproj -c Release -o artifacts @Args
dotnet pack src/ANcpLua.NET.Sdk.Web.csproj -c Release -o artifacts @Args
dotnet pack src/ANcpLua.NET.Sdk.Test.csproj -c Release -o artifacts @Args

# Pack ServiceDefaults packages (required by SDK.Web)
dotnet pack eng/ANcpSdk.AspNetCore.ServiceDefaults/ANcpSdk.AspNetCore.ServiceDefaults.csproj -c Release -o artifacts @Args
dotnet pack eng/ANcpSdk.AspNetCore.ServiceDefaults.AutoRegister/ANcpSdk.AspNetCore.ServiceDefaults.AutoRegister.csproj -c Release -o artifacts @Args

