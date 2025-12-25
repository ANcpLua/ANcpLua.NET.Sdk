using System.Diagnostics.CodeAnalysis;
using ANcpLua.Sdk.Tests.Helpers;
using ANcpLua.Sdk.Tests.Infrastructure;
using Xunit.Sdk;

[assembly: RegisterXunitSerializer(typeof(PolyfillCaseSerializer), typeof(IPolyfillCase))]

namespace ANcpLua.Sdk.Tests.Infrastructure;

public interface IPolyfillCase
{
    string MarkerTypeName { get; }
    string TargetFramework { get; }
    Task RunPositive(PackageFixture fixture, ITestOutputHelper output);
    Task RunNegative(PackageFixture fixture, ITestOutputHelper output);
}

/// <summary>
///     xUnit v3 serializer for IPolyfillCase - allows Test Explorer to enumerate individual test cases.
/// </summary>
public sealed class PolyfillCaseSerializer : IXunitSerializer
{
    public bool IsSerializable(Type type, object? value, [NotNullWhen(false)] out string? failureReason)
    {
        if (typeof(IPolyfillCase).IsAssignableFrom(type) && value is IPolyfillCase)
        {
            failureReason = null;
            return true;
        }

        failureReason = $"Type {type.Name} is not IPolyfillCase";
        return false;
    }

    public string Serialize(object value)
    {
        var testCase = (IPolyfillCase)value;
        return $"{testCase.MarkerTypeName}|{testCase.TargetFramework}";
    }

    public object Deserialize(Type type, string serializedValue)
    {
        var parts = serializedValue.Split('|');
        var markerTypeName = parts[0];
        var tfm = parts[1];

        return markerTypeName switch
        {
            nameof(TrimAttributesFile) => new PolyfillCase<TrimAttributesFile>(tfm),
            nameof(NullabilityAttributesFile) => new PolyfillCase<NullabilityAttributesFile>(tfm),
            nameof(IsExternalInitFile) => new PolyfillCase<IsExternalInitFile>(tfm),
            nameof(RequiredMemberFile) => new PolyfillCase<RequiredMemberFile>(tfm),
            nameof(CompilerFeatureRequiredFile) => new PolyfillCase<CompilerFeatureRequiredFile>(tfm),
            nameof(CallerArgumentExpressionFile) => new PolyfillCase<CallerArgumentExpressionFile>(tfm),
            nameof(UnreachableExceptionFile) => new PolyfillCase<UnreachableExceptionFile>(tfm),
            nameof(ExperimentalAttributeFile) => new PolyfillCase<ExperimentalAttributeFile>(tfm),
            nameof(IndexRangeFile) => new PolyfillCase<IndexRangeFile>(tfm),
            nameof(ParamCollectionFile) => new PolyfillCase<ParamCollectionFile>(tfm),
            nameof(StackTraceHiddenFile) => new PolyfillCase<StackTraceHiddenFile>(tfm),
            nameof(LockFile) => new PolyfillCase<LockFile>(tfm),
            nameof(TimeProviderFile) => new PolyfillCase<TimeProviderFile>(tfm),
            nameof(ThrowFile) => new PolyfillCase<ThrowFile>(tfm),
            nameof(StringOrdinalComparerFile) => new PolyfillCase<StringOrdinalComparerFile>(tfm),
            nameof(DiagnosticClassesFile) => new PolyfillCase<DiagnosticClassesFile>(tfm),
            _ => throw new InvalidOperationException($"Unknown marker type: {markerTypeName}")
        };
    }
}

public sealed class PolyfillCase<TMarker>(string tfm) : IPolyfillCase
    where TMarker : IPolyfillMarker
{
    public string MarkerTypeName => typeof(TMarker).Name;
    public string TargetFramework => tfm;

    public async Task RunPositive(PackageFixture fixture, ITestOutputHelper output)
    {
        await using var project =
            new ProjectBuilder(fixture, output, SdkImportStyle.SdkElement, PackageFixture.SdkName);

        var properties = new List<(string, string)>
        {
            (TMarker.InjectPropertyName, MsBuildValues.True),
            (MsBuildProperties.TargetFramework, tfm),
            (MsBuildProperties.OutputType, MsBuildValues.Library)
        };

        if (TMarker.InjectPropertyName is "InjectRequiredMemberOnLegacy" or "InjectCompilerFeatureRequiredOnLegacy")
            properties.Add((MsBuildProperties.LangVersion, MsBuildValues.Latest));

        project.AddCsprojFile(properties.ToArray());

        project.AddFile("Smoke.cs", $$"""
                                      #nullable enable
                                      using System;
                                      namespace Consumer;
                                      internal class Smoke
                                      {
                                          public void Run()
                                          {
                                              {{TMarker.ActivationSnippet}}
                                          }
                                      }
                                      """);

        var result = await project.BuildAndGetOutput();
        Assert.True(result.ExitCode is 0,
            $"Build failed for {TMarker.InjectPropertyName} on {tfm} when expected to succeed. Output: {result.ProcessOutput}");
    }

    public async Task RunNegative(PackageFixture fixture, ITestOutputHelper output)
    {
        if (typeof(TMarker) == typeof(DiagnosticClassesFile)) return;

        await using var project =
            new ProjectBuilder(fixture, output, SdkImportStyle.SdkElement, PackageFixture.SdkName);

        var properties = new List<(string, string)>
        {
            (MsBuildProperties.TargetFramework, tfm),
            (MsBuildProperties.OutputType, MsBuildValues.Library)
        };

        if (TMarker.InjectPropertyName is "InjectRequiredMemberOnLegacy" or "InjectCompilerFeatureRequiredOnLegacy")
            properties.Add((MsBuildProperties.LangVersion, MsBuildValues.Latest));

        project.AddCsprojFile(properties.ToArray());

        project.AddFile("Smoke.cs", $@"
            #nullable enable
            using System;
            namespace Consumer;
            internal class Smoke
            {{
                public void Run()
                {{
                    {TMarker.ActivationSnippet}
                }}
            }}
        ");

        var result = await project.BuildAndGetOutput();
        Assert.True(result.ExitCode is not 0,
            $"Build succeeded for {TMarker.InjectPropertyName} on {tfm} when expected to fail without the flag. Output: {result.ProcessOutput}");

        Assert.True(
            result.OutputContains("CS0246") ||
            result.OutputContains("CS0103") ||
            result.OutputContains("CS0234") ||
            result.OutputContains("CS0518") ||
            result.OutputContains("CS1513") ||
            result.OutputContains("CS1022"),
            $"Expected compilation error for missing type {TMarker.ExpectedType}. Output: {result.ProcessOutput}");
    }

    public override string ToString()
    {
        return $"{typeof(TMarker).Name} → {tfm}";
    }
}