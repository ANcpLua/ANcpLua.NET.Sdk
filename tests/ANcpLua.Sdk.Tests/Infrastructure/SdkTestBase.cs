using ANcpLua.Sdk.Tests.Helpers;

namespace ANcpLua.Sdk.Tests.Infrastructure;

/// <summary>
///     Base class for SDK tests providing common helper methods.
/// </summary>
public abstract class SdkTestBase(PackageFixture fixture, ITestOutputHelper output)
{
    private PackageFixture Fixture { get; } = fixture;

    private ITestOutputHelper Output { get; } = output;

    /// <summary>Creates a new ProjectBuilder with default settings</summary>
    private ProjectBuilder CreateProject(
        SdkImportStyle style = SdkImportStyle.SdkElement,
        string sdk = PackageFixture.SdkName) =>
        new(Fixture, Output, style, sdk);

    /// <summary>Quick build helper for simple test cases</summary>
    protected async Task<BuildResult> QuickBuild(
        string code,
        string tfm = Tfm.Net100,
        params (string Key, string Value)[] extraProps)
    {
        await using var project = CreateProject();
        return await project
            .WithTargetFramework(tfm)
            .WithOutputType(Val.Library)
            .WithProperties(extraProps)
            .AddSource("Code.cs", code)
            .BuildAsync();
    }

    /// <summary>Quick build helper with library output type</summary>
    protected Task<BuildResult> BuildLibrary(
        string code,
        string tfm = Tfm.Net100,
        params (string Key, string Value)[] extraProps) =>
        QuickBuild(code, tfm, extraProps);

    /// <summary>Quick build helper with exe output type</summary>
    protected async Task<BuildResult> BuildExe(
        string code,
        string tfm = Tfm.Net100,
        params (string Key, string Value)[] extraProps)
    {
        await using var project = CreateProject();
        return await project
            .WithTargetFramework(tfm)
            .WithOutputType(Val.Exe)
            .WithProperties(extraProps)
            .AddSource("Program.cs", code)
            .BuildAsync();
    }
}
