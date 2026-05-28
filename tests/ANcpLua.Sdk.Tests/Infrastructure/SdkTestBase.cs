using System.Diagnostics.CodeAnalysis;

namespace ANcpLua.Sdk.Tests.Infrastructure;

public abstract class SdkTestBase(PackageFixture fixture)
{
    protected PackageFixture Fixture { get; } = fixture;

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
        Justification = "The factory returns the builder unowned; every call site wraps the result in `await using`.")]
    protected SdkProjectBuilder CreateProject(
        SdkImportStyle style,
        string sdkName,
        NetSdkVersion sdkVersion,
        params string[] recordedProperties) =>
        SdkProjectBuilder.Create(Fixture, style, sdkName)
            .WithDotnetSdkVersion(sdkVersion)
            .RecordProperties(recordedProperties);

    protected async Task<BuildResult> BuildLibraryAsync(
        string code,
        string tfm = Tfm.Net100,
        params (string Key, string Value)[] properties)
    {
        await using var project = SdkProjectBuilder.Create(Fixture);
        return await project
            .WithTargetFramework(tfm)
            .WithOutputType(Val.Library)
            .WithProperties(properties)
            .AddSource("Code.cs", code)
            .BuildAsync();
    }
}
