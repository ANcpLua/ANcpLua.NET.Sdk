using System.Collections.Immutable;
using System.Xml.Linq;
using Meziantou.Framework;

namespace ANcpLua.Sdk.Tests.Infrastructure;

/// <summary>Repository paths for polyfill source files.</summary>
public static class RepositoryPaths
{
    public const string DirectoryBuildProps = "Directory.Build.props";

    public const string ThrowHelper = "eng/Shared/Throw/Throw.cs";
    public const string StringOrdinalComparer = "eng/Extensions/Comparers/StringOrdinalComparer.cs";
    public const string LockPolyfill = "eng/MSBuild/Polyfills/Lock.cs";
    public const string DiagnosticClassesPolyfill = "eng/MSBuild/Polyfills/DiagnosticClasses.cs";
    public const string TimeProviderPolyfill = "eng/LegacySupport/TimeProvider/TimeProvider.cs";
    public const string IndexPolyfill = "eng/LegacySupport/IndexRange/Index.cs";
    public const string IsExternalInitPolyfill = "eng/LegacySupport/IsExternalInit/IsExternalInit.cs";
    public const string RequiredMemberPolyfill = "eng/LegacySupport/LanguageFeatures/RequiredMemberAttribute.cs";

    public const string CompilerFeatureRequiredPolyfill =
        "eng/LegacySupport/LanguageFeatures/CompilerFeatureRequiredAttribute.cs";

    public const string CallerArgumentExpressionPolyfill =
        "eng/LegacySupport/LanguageFeatures/CallerArgumentExpressionAttribute.cs";

    public const string ParamCollectionPolyfill = "eng/LegacySupport/LanguageFeatures/ParamCollectionAttribute.cs";
    public const string UnreachableExceptionPolyfill = "eng/LegacySupport/Exceptions/UnreachableException.cs";
    public const string StackTraceHiddenPolyfill = "eng/LegacySupport/Diagnostics/StackTraceHiddenAttribute.cs";
    public const string NullableAttributesPolyfill = "eng/LegacySupport/DiagnosticAttributes/NullableAttributes.cs";

    public const string TrimAttributesPolyfill =
        "eng/LegacySupport/TrimAttributes/DynamicallyAccessedMembersAttribute.cs";

    public const string ExperimentalAttributePolyfill = "eng/LegacySupport/Experimental/ExperimentalAttribute.cs";
}

/// <summary>
///     Single source of truth for polyfill test data.
///     No serializer needed - xUnit v3 handles record structs natively.
/// </summary>
public readonly record struct PolyfillDefinition(
    string InjectionProperty,
    string RepositoryPath,
    string MinimumTargetFramework,
    string ActivationCode,
    string ExpectedType,
    bool HasNegativeTest = true,
    bool RequiresLangVersionLatest = false,
    bool DisablesSharedThrowForNegative = false)
{
    public static readonly PolyfillDefinition Lock = new(
        Prop.InjectLockPolyfill,
        RepositoryPaths.LockPolyfill,
        Tfm.Net80,
        "_ = new System.Threading.Lock();",
        "System.Threading.Lock");

    public static readonly PolyfillDefinition TimeProvider = new(
        Prop.InjectTimeProviderPolyfill,
        RepositoryPaths.TimeProviderPolyfill,
        Tfm.NetStandard20,
        "_ = typeof(System.TimeProvider);",
        "System.TimeProvider");

    public static readonly PolyfillDefinition IndexRange = new(
        Prop.InjectIndexRangeOnLegacy,
        RepositoryPaths.IndexPolyfill,
        Tfm.NetStandard20,
        "_ = new System.Index(1);",
        "System.Index");

    public static readonly PolyfillDefinition IsExternalInit = new(
        Prop.InjectIsExternalInitOnLegacy,
        RepositoryPaths.IsExternalInitPolyfill,
        Tfm.NetStandard20,
        "_ = typeof(System.Runtime.CompilerServices.IsExternalInit);",
        "System.Runtime.CompilerServices.IsExternalInit");

    public static readonly PolyfillDefinition RequiredMember = new(
        Prop.InjectRequiredMemberOnLegacy,
        RepositoryPaths.RequiredMemberPolyfill,
        Tfm.NetStandard20,
        "_ = typeof(System.Runtime.CompilerServices.RequiredMemberAttribute);",
        "System.Runtime.CompilerServices.RequiredMemberAttribute",
        RequiresLangVersionLatest: true);

    public static readonly PolyfillDefinition CompilerFeatureRequired = new(
        Prop.InjectCompilerFeatureRequiredOnLegacy,
        RepositoryPaths.CompilerFeatureRequiredPolyfill,
        Tfm.NetStandard20,
        "_ = typeof(System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute);",
        "System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute",
        RequiresLangVersionLatest: true);

    public static readonly PolyfillDefinition CallerArgumentExpression = new(
        Prop.InjectCallerAttributesOnLegacy,
        RepositoryPaths.CallerArgumentExpressionPolyfill,
        Tfm.NetStandard20,
        "_ = typeof(System.Runtime.CompilerServices.CallerArgumentExpressionAttribute);",
        "System.Runtime.CompilerServices.CallerArgumentExpressionAttribute",
        DisablesSharedThrowForNegative: true);

    public static readonly PolyfillDefinition ParamCollection = new(
        Prop.InjectParamCollectionOnLegacy,
        RepositoryPaths.ParamCollectionPolyfill,
        Tfm.Net80,
        "_ = typeof(System.Runtime.CompilerServices.ParamCollectionAttribute);",
        "System.Runtime.CompilerServices.ParamCollectionAttribute");

    public static readonly PolyfillDefinition UnreachableException = new(
        Prop.InjectUnreachableExceptionOnLegacy,
        RepositoryPaths.UnreachableExceptionPolyfill,
        Tfm.NetStandard20,
        "_ = new System.Diagnostics.UnreachableException();",
        "System.Diagnostics.UnreachableException");

    public static readonly PolyfillDefinition StackTraceHidden = new(
        Prop.InjectStackTraceHiddenOnLegacy,
        RepositoryPaths.StackTraceHiddenPolyfill,
        Tfm.NetStandard20,
        "_ = typeof(System.Diagnostics.StackTraceHiddenAttribute);",
        "System.Diagnostics.StackTraceHiddenAttribute");

    public static readonly PolyfillDefinition NullableAttributes = new(
        Prop.InjectNullabilityAttributesOnLegacy,
        RepositoryPaths.NullableAttributesPolyfill,
        Tfm.NetStandard20,
        "_ = typeof(System.Diagnostics.CodeAnalysis.AllowNullAttribute);",
        "System.Diagnostics.CodeAnalysis.AllowNullAttribute",
        DisablesSharedThrowForNegative: true);

    public static readonly PolyfillDefinition TrimAttributes = new(
        Prop.InjectTrimAttributesOnLegacy,
        RepositoryPaths.TrimAttributesPolyfill,
        Tfm.NetStandard20,
        "_ = typeof(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute);",
        "System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute");

    public static readonly PolyfillDefinition ExperimentalAttribute = new(
        Prop.InjectExperimentalAttributeOnLegacy,
        RepositoryPaths.ExperimentalAttributePolyfill,
        Tfm.NetStandard20,
        "_ = typeof(System.Diagnostics.CodeAnalysis.ExperimentalAttribute);",
        "System.Diagnostics.CodeAnalysis.ExperimentalAttribute");

    public static readonly PolyfillDefinition Throw = new(
        Prop.InjectSharedThrow,
        RepositoryPaths.ThrowHelper,
        Tfm.NetStandard20,
        "_ = Microsoft.Shared.Diagnostics.Throw.IfNull((object?)null);",
        "Microsoft.Shared.Diagnostics.Throw",
        DisablesSharedThrowForNegative: true);

    public static readonly PolyfillDefinition StringOrdinalComparer = new(
        Prop.InjectStringOrdinalComparer,
        RepositoryPaths.StringOrdinalComparer,
        Tfm.NetStandard20,
        "_ = ANcpLua.NET.Sdk.Shared.Extensions.Comparers.StringOrdinalComparer.Instance;",
        "ANcpLua.NET.Sdk.Shared.Extensions.Comparers.StringOrdinalComparer");

    public static readonly PolyfillDefinition DiagnosticClasses = new(
        Prop.InjectDiagnosticClassesOnLegacy,
        RepositoryPaths.DiagnosticClassesPolyfill,
        Tfm.NetStandard20,
        "_ = typeof(System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute);",
        "System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute",
        false);

    public static ImmutableArray<PolyfillDefinition> All =>
    [
        TrimAttributes, NullableAttributes, IsExternalInit, RequiredMember,
        CompilerFeatureRequired, CallerArgumentExpression, UnreachableException, ExperimentalAttribute,
        IndexRange, ParamCollection, StackTraceHidden, Lock,
        TimeProvider, Throw, StringOrdinalComparer, DiagnosticClasses
    ];

    public override string ToString() => $"{Path.GetFileNameWithoutExtension(RepositoryPath)} → {MinimumTargetFramework}";
}

public readonly record struct RepositoryRoot
{
    private RepositoryRoot(FullPath path) => FullPath = path;

    public FullPath FullPath { get; }

    public FullPath this[string relativePath] => FullPath / relativePath;

    public static RepositoryRoot Locate()
    {
        var directory = FullPath.CurrentDirectory();
        while (true)
        {
            if (File.Exists(directory / "ANcpLua.NET.Sdk.slnx") || File.Exists(directory / "ANcpLua.NET.Sdk.sln"))
                return new RepositoryRoot(directory);
            var parent = directory.Parent;
            if (parent == directory)
                throw new DirectoryNotFoundException("Repository root not found");
            directory = parent;
        }
    }

    public static implicit operator FullPath(RepositoryRoot root) => root.FullPath;

    public static implicit operator string(RepositoryRoot root) => root.FullPath;
}

public sealed class MsBuildPropertyBuilder : Dictionary<string, string?>
{
    public static MsBuildPropertyBuilder Create() => new();

    public MsBuildPropertyBuilder Set(string property, string? value)
    {
        this[property] = value;
        return this;
    }

    public MsBuildPropertyBuilder WithTargetFramework(string tfm) => Set(Prop.TargetFramework, tfm);

    public MsBuildPropertyBuilder WithLangVersion(string version) => Set(Prop.LangVersion, version);

    public MsBuildPropertyBuilder WithOutputType(string type) => Set(Prop.OutputType, type);

    public MsBuildPropertyBuilder WithNullable(bool enable = true) =>
        Set(Prop.Nullable, enable ? Val.Enable : Val.Disable);

    public MsBuildPropertyBuilder Enable(string property) => Set(property, Val.True);

    public MsBuildPropertyBuilder Disable(string property) => Set(property, Val.False);

    public MsBuildPropertyBuilder ForPolyfill(PolyfillDefinition polyfill, string? targetFrameworkOverride = null) =>
        WithTargetFramework(targetFrameworkOverride ?? polyfill.MinimumTargetFramework)
            .WithOutputType(Val.Library)
            .Enable(polyfill.InjectionProperty);

    public (string Name, string Value)[] ToPropertyArray() =>
        this.Where(static kv => kv.Value is not null).Select(static kv => (kv.Key, kv.Value!)).ToArray();

    public static MsBuildPropertyBuilder FromXmlSnippets(params string[] xmlSnippets)
    {
        var builder = Create();
        foreach (var snippet in xmlSnippets)
        {
            if (string.IsNullOrWhiteSpace(snippet)) continue;
            var element = XElement.Parse(snippet);
            builder[element.Name.LocalName] = element.Value;
        }

        return builder;
    }
}

public static class XmlSnippetBuilder
{
    public static string TargetFramework(string tfm) => $"<{Prop.TargetFramework}>{tfm}</{Prop.TargetFramework}>";

    public static string LangVersion(string version) => $"<{Prop.LangVersion}>{version}</{Prop.LangVersion}>";

    public static string OutputType(string type) => $"<{Prop.OutputType}>{type}</{Prop.OutputType}>";

    public static string Property(string name, string value) => $"<{name}>{value}</{name}>";
}

public static class PolyfillTestDataSource
{
    public static TheoryData<string> AllTargetFrameworks =>
    [
        Tfm.NetStandard20,
        Tfm.Net100
    ];

    public static TheoryData<string> AllLangVersions =>
    [
        "12",
        "13",
        "14",
        Val.Latest
    ];

    public static TheoryData<PolyfillDefinition, string, bool> InjectionMatrix()
    {
        var data = new TheoryData<PolyfillDefinition, string, bool>();
        foreach (var polyfill in PolyfillDefinition.All)
        {
            data.Add(polyfill, polyfill.MinimumTargetFramework, true);

            var isExtension =
                polyfill.InjectionProperty is Prop.InjectSharedThrow or Prop.InjectStringOrdinalComparer;

            data.Add(polyfill, Tfm.Net100, isExtension);
        }

        return data;
    }

    public static TheoryData<PolyfillDefinition> ActivationMatrix()
    {
        var data = new TheoryData<PolyfillDefinition>();
        foreach (var polyfill in PolyfillDefinition.All)
            data.Add(polyfill);
        return data;
    }
}

// ============================================================================
// CRITICAL: ALL SDK BRANDING STRINGS MUST COME FROM HERE
//
// DO NOT hardcode "randomanme", "wellknownname",  names anywhere.
// This file is the SINGLE SOURCE OF TRUTH for all branding.
//
// History: 7 hours lost debugging because of hardcoded legacy names (2025-12-16)
// ============================================================================

/// <summary>
///     Single source of truth for all SDK branding strings.
///     NEVER hardcode these values - always reference this class.
/// </summary>
public static class SdkBrandingConstants
{
    public const string Author = "ANcpLua";
    public const string SdkMetadataKey = "ANcpLua.Sdk.Name";

    // ═══════════════════════════════════════════════════════════════════════
    // LEGACY NAMES - FOR DETECTION/MIGRATION ONLY - DO NOT USE IN NEW CODE!
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    ///     Legacy names that should NEVER appear in code. Used for validation only.
    ///     If you see these in a PR review, REJECT IT.
    /// </summary>
    public static readonly string[] LegacyNamesToBlock =
    [
        "Meziantou",
        "meziantou",
        "MEZIANTOU",
        "UseMeziantouConventions",
        "MeziantouServiceDefaultsOptions",
        "Meziantou.AspNetCore.ServiceDefaults",
        "Meziantou.Sdk.Name"
    ];
}
