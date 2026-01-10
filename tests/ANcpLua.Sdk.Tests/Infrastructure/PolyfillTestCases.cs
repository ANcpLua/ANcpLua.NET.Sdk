using System.Collections.Frozen;
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
    private static readonly FrozenDictionary<string, Func<string, IPolyfillCase>> Factories =
        new Dictionary<string, Func<string, IPolyfillCase>>
        {
            [nameof(TrimAttributesFile)] = static tfm => new PolyfillCase<TrimAttributesFile>(tfm),
            [nameof(NullabilityAttributesFile)] = static tfm => new PolyfillCase<NullabilityAttributesFile>(tfm),
            [nameof(IsExternalInitFile)] = static tfm => new PolyfillCase<IsExternalInitFile>(tfm),
            [nameof(RequiredMemberFile)] = static tfm => new PolyfillCase<RequiredMemberFile>(tfm),
            [nameof(CompilerFeatureRequiredFile)] = static tfm => new PolyfillCase<CompilerFeatureRequiredFile>(tfm),
            [nameof(CallerArgumentExpressionFile)] = static tfm => new PolyfillCase<CallerArgumentExpressionFile>(tfm),
            [nameof(UnreachableExceptionFile)] = static tfm => new PolyfillCase<UnreachableExceptionFile>(tfm),
            [nameof(ExperimentalAttributeFile)] = static tfm => new PolyfillCase<ExperimentalAttributeFile>(tfm),
            [nameof(IndexRangeFile)] = static tfm => new PolyfillCase<IndexRangeFile>(tfm),
            [nameof(ParamCollectionFile)] = static tfm => new PolyfillCase<ParamCollectionFile>(tfm),
            [nameof(StackTraceHiddenFile)] = static tfm => new PolyfillCase<StackTraceHiddenFile>(tfm),
            [nameof(LockFile)] = static tfm => new PolyfillCase<LockFile>(tfm),
            [nameof(TimeProviderFile)] = static tfm => new PolyfillCase<TimeProviderFile>(tfm),
            [nameof(ThrowFile)] = static tfm => new PolyfillCase<ThrowFile>(tfm),
            [nameof(StringOrdinalComparerFile)] = static tfm => new PolyfillCase<StringOrdinalComparerFile>(tfm),
            [nameof(DiagnosticClassesFile)] = static tfm => new PolyfillCase<DiagnosticClassesFile>(tfm)
        }.ToFrozenDictionary();

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

        return Factories.TryGetValue(markerTypeName, out var factory)
            ? factory(tfm)
            : throw new InvalidOperationException($"Unknown marker type: {markerTypeName}");
    }
}

public sealed class PolyfillCase<TMarker>(string tfm) : IPolyfillCase
    where TMarker : IPolyfillMarker
{
    public string MarkerTypeName => typeof(TMarker).Name;
    public string TargetFramework { get; } = tfm;

    public async Task RunPositive(PackageFixture fixture, ITestOutputHelper output)
    {
        await using var project =
            new ProjectBuilder(fixture, output, SdkImportStyle.SdkElement, PackageFixture.SdkName);

        var properties = new List<(string Name, string Value)>
        {
            (TMarker.InjectPropertyName, Val.True),
            (Name: Prop.TargetFramework, Value: TargetFramework),
            (Prop.OutputType, Val.Library)
        };

        if (TMarker.InjectPropertyName is Prop.InjectRequiredMemberOnLegacy
            or Prop.InjectCompilerFeatureRequiredOnLegacy)
            properties.Add((Prop.LangVersion, Val.Latest));

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
        result.ShouldSucceed($"Build failed for {TMarker.InjectPropertyName} on {TargetFramework}");
    }

    public async Task RunNegative(PackageFixture fixture, ITestOutputHelper output)
    {
        if (typeof(TMarker) == typeof(DiagnosticClassesFile)) return;

        await using var project =
            new ProjectBuilder(fixture, output, SdkImportStyle.SdkElement, PackageFixture.SdkName);

        var properties = new List<(string Name, string Value)>
        {
            (Name: Prop.TargetFramework, Value: TargetFramework),
            (Prop.OutputType, Val.Library)
        };

        if (TMarker.InjectPropertyName is Prop.InjectRequiredMemberOnLegacy
            or Prop.InjectCompilerFeatureRequiredOnLegacy)
            properties.Add((Prop.LangVersion, Val.Latest));

        // InjectSharedThrow defaults to true, which auto-enables:
        // - InjectCallerAttributesOnLegacy
        // - InjectNullabilityAttributesOnLegacy
        // For negative tests of these polyfills, we must explicitly disable InjectSharedThrow
        if (TMarker.InjectPropertyName is Prop.InjectSharedThrow
            or Prop.InjectCallerAttributesOnLegacy
            or Prop.InjectNullabilityAttributesOnLegacy)
            properties.Add((Prop.InjectSharedThrow, Val.False));

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
        result.ShouldFail(
            $"Build succeeded for {TMarker.InjectPropertyName} on {TargetFramework} when expected to fail without the flag");

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
        return $"{typeof(TMarker).Name} → {TargetFramework}";
    }
}
