# MSBuild Customizations

This directory contains custom MSBuild `.props` and `.targets` files that extend the .NET SDK build process.

## Usage

These files are typically imported automatically by the `ANcpLua.NET.Sdk`. Individual properties defined within them can be controlled via `<PropertyGroup>` elements in your `.csproj` files.

For example, to configure polyfills or extensions, refer to the respective `README.md` files in `eng/LegacySupport` and `eng/Extensions`.