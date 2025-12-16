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

