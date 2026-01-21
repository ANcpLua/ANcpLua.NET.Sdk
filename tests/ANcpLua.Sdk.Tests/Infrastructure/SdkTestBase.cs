namespace ANcpLua.Sdk.Tests.Infrastructure;

/// <summary>
///     Base class for SDK tests providing common helper methods.
/// </summary>
/// <remarks>
///     <para>
///         Provides convenient factory and helper methods for creating SDK test projects.
///         Uses <see cref="SdkProjectBuilder.Create" /> internally for consistent defaults.
///     </para>
/// </remarks>
/// <example>
///     <code>
/// public class MyTests(PackageFixture fixture) : SdkTestBase(fixture)
/// {
///     [Fact]
///     public async Task MyTest()
///     {
///         // Quick one-liner for simple tests
///         var result = await QuickBuild("public class Foo {}");
///         result.ShouldSucceed();
///
///         // Or use CreateProject for more control
///         await using var project = CreateProject();
///         var buildResult = await project
///             .WithProperty("Nullable", "enable")
///             .AddSource("Code.cs", code)
///             .BuildAsync();
///     }
/// }
/// </code>
/// </example>
public abstract class SdkTestBase(PackageFixture fixture)
{
    /// <summary>
    ///     Gets the package fixture for SDK tests.
    /// </summary>
    protected PackageFixture Fixture { get; } = fixture;

    /// <summary>
    ///     Creates a new <see cref="SdkProjectBuilder" /> with default settings.
    /// </summary>
    /// <param name="style">The SDK import style. Defaults to <see cref="SdkImportStyle.SdkElement" />.</param>
    /// <param name="sdkName">The SDK name. Defaults to <see cref="PackageFixture.SdkName" />.</param>
    /// <returns>A fully-configured <see cref="SdkProjectBuilder" /> with TFM=net10.0, OutputType=Library.</returns>
    /// <remarks>
    ///     Uses <see cref="SdkProjectBuilder.Create" /> internally.
    ///     The returned builder is pre-configured with sensible defaults.
    /// </remarks>
    protected SdkProjectBuilder CreateProject(
        SdkImportStyle style = SdkImportStyle.SdkElement,
        string? sdkName = null) =>
        SdkProjectBuilder.Create(Fixture, style, sdkName);

    /// <summary>
    ///     Quick build helper for simple test cases with library output.
    /// </summary>
    /// <param name="code">The C# source code to compile.</param>
    /// <param name="tfm">The target framework. Defaults to net10.0.</param>
    /// <param name="extraProps">Additional MSBuild properties to set.</param>
    /// <returns>The build result.</returns>
    /// <remarks>
    ///     Creates a project with OutputType=Library, builds it, and disposes.
    ///     For more control, use <see cref="CreateProject" /> directly.
    /// </remarks>
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

    /// <summary>
    ///     Quick build helper with library output type (alias for <see cref="QuickBuild" />).
    /// </summary>
    protected Task<BuildResult> BuildLibrary(
        string code,
        string tfm = Tfm.Net100,
        params (string Key, string Value)[] extraProps) =>
        QuickBuild(code, tfm, extraProps);

    /// <summary>
    ///     Quick build helper with executable output type.
    /// </summary>
    /// <param name="code">The C# source code to compile (should include entry point).</param>
    /// <param name="tfm">The target framework. Defaults to net10.0.</param>
    /// <param name="extraProps">Additional MSBuild properties to set.</param>
    /// <returns>The build result.</returns>
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
