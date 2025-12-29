using System.Collections.Immutable;
using System.Xml.Linq;
using Meziantou.Framework;

namespace ANcpLua.Sdk.Tests.Infrastructure;

public static class TargetFrameworks
{
    public const string NetStandard20 = "netstandard2.0";
    public const string Net80 = "net8.0";
    public const string Net100 = "net10.0";
}

public static class MsBuildProperties
{
    public const string TargetFramework = nameof(TargetFramework);
    public const string TargetFrameworks = nameof(TargetFrameworks);
    public const string OutputType = nameof(OutputType);
    public const string Nullable = nameof(Nullable);
    public const string ImplicitUsings = nameof(ImplicitUsings);
    public const string LangVersion = nameof(LangVersion);
    public const string TreatWarningsAsErrors = nameof(TreatWarningsAsErrors);
    public const string IsPackable = nameof(IsPackable);
    public const string GenerateDocumentationFile = nameof(GenerateDocumentationFile);
    public const string ManagePackageVersionsCentrally = nameof(ManagePackageVersionsCentrally);
}

public static class MsBuildItems
{
    public const string PackageReference = nameof(PackageReference);
    public const string PackageVersion = nameof(PackageVersion);
    public const string ProjectReference = nameof(ProjectReference);
    public const string Compile = nameof(Compile);
    public const string Content = nameof(Content);
    public const string None = nameof(None);
    public const string EmbeddedResource = nameof(EmbeddedResource);
}

public static class MsBuildAttributes
{
    public const string Include = nameof(Include);
    public const string Version = nameof(Version);
    public const string PrivateAssets = nameof(PrivateAssets);
    public const string Condition = nameof(Condition);
}

public static class MsBuildValues
{
    public const string Exe = nameof(Exe);
    public const string Library = nameof(Library);
    public const string Enable = "enable";
    public const string Disable = "disable";
    public const string True = "true";
    public const string False = "false";
    public const string Latest = "latest";
    public const string Preview = "preview";
}

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

public static class InjectionPropertyNames
{
    public const string Throw = "InjectSharedThrow";
    public const string StringOrdinalComparer = "InjectStringOrdinalComparer";
    public const string Lock = "InjectLockPolyfill";
    public const string TimeProvider = "InjectTimeProviderPolyfill";
    public const string IndexRange = "InjectIndexRangeOnLegacy";
    public const string IsExternalInit = "InjectIsExternalInitOnLegacy";
    public const string RequiredMember = "InjectRequiredMemberOnLegacy";
    public const string CompilerFeatureRequired = "InjectCompilerFeatureRequiredOnLegacy";
    public const string CallerArgumentExpression = "InjectCallerAttributesOnLegacy";
    public const string ParamCollection = "InjectParamCollectionOnLegacy";
    public const string UnreachableException = "InjectUnreachableExceptionOnLegacy";
    public const string StackTraceHidden = "InjectStackTraceHiddenOnLegacy";
    public const string NullableAttributes = "InjectNullabilityAttributesOnLegacy";
    public const string TrimAttributes = "InjectTrimAttributesOnLegacy";
    public const string ExperimentalAttribute = "InjectExperimentalAttributeOnLegacy";
    public const string DiagnosticClasses = "InjectDiagnosticClassesOnLegacy";
}

public static class PolyfillTypeNames
{
    public const string Lock = "System.Threading.Lock";
    public const string TimeProvider = "System.TimeProvider";
    public const string Index = "System.Index";
    public const string Range = "System.Range";
    public const string IsExternalInit = "System.Runtime.CompilerServices.IsExternalInit";
    public const string RequiredMemberAttribute = "System.Runtime.CompilerServices.RequiredMemberAttribute";

    public const string CompilerFeatureRequiredAttribute =
        "System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute";

    public const string CallerArgumentExpressionAttribute =
        "System.Runtime.CompilerServices.CallerArgumentExpressionAttribute";

    public const string ParamCollectionAttribute = "System.Runtime.CompilerServices.ParamCollectionAttribute";
    public const string UnreachableException = "System.Diagnostics.UnreachableException";
    public const string StackTraceHiddenAttribute = "System.Diagnostics.StackTraceHiddenAttribute";
    public const string AllowNullAttribute = "System.Diagnostics.CodeAnalysis.AllowNullAttribute";

    public const string DynamicallyAccessedMembersAttribute =
        "System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute";

    public const string ExperimentalAttribute = "System.Diagnostics.CodeAnalysis.ExperimentalAttribute";
}

public static class PolyfillActivationCode
{
    public const string Lock = "_ = new System.Threading.Lock();";
    public const string TimeProvider = "_ = typeof(System.TimeProvider);";
    public const string Index = "_ = new System.Index(1);";
    public const string IsExternalInit = "_ = typeof(System.Runtime.CompilerServices.IsExternalInit);";
    public const string RequiredMember = "_ = typeof(System.Runtime.CompilerServices.RequiredMemberAttribute);";

    public const string CompilerFeatureRequired =
        "_ = typeof(System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute);";

    public const string CallerArgumentExpression =
        "_ = typeof(System.Runtime.CompilerServices.CallerArgumentExpressionAttribute);";

    public const string ParamCollection = "_ = typeof(System.Runtime.CompilerServices.ParamCollectionAttribute);";
    public const string UnreachableException = "_ = new System.Diagnostics.UnreachableException();";
    public const string StackTraceHidden = "_ = typeof(System.Diagnostics.StackTraceHiddenAttribute);";
    public const string AllowNull = "_ = typeof(System.Diagnostics.CodeAnalysis.AllowNullAttribute);";

    public const string DynamicallyAccessedMembers =
        "_ = typeof(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute);";

    public const string Experimental = "_ = typeof(System.Diagnostics.CodeAnalysis.ExperimentalAttribute);";
}

public readonly record struct PolyfillDefinition(
    string InjectionProperty,
    string RepositoryPath,
    string FullyQualifiedTypeName,
    string ActivationCode,
    string MinimumTargetFramework)
{
    public static readonly PolyfillDefinition Lock = new(
        InjectionPropertyNames.Lock,
        RepositoryPaths.LockPolyfill,
        PolyfillTypeNames.Lock,
        PolyfillActivationCode.Lock,
        TargetFrameworks.Net80);

    public static readonly PolyfillDefinition TimeProvider = new(
        InjectionPropertyNames.TimeProvider,
        RepositoryPaths.TimeProviderPolyfill,
        PolyfillTypeNames.TimeProvider,
        PolyfillActivationCode.TimeProvider,
        TargetFrameworks.NetStandard20);

    public static readonly PolyfillDefinition IndexRange = new(
        InjectionPropertyNames.IndexRange,
        RepositoryPaths.IndexPolyfill,
        PolyfillTypeNames.Index,
        PolyfillActivationCode.Index,
        TargetFrameworks.NetStandard20);

    public static readonly PolyfillDefinition IsExternalInit = new(
        InjectionPropertyNames.IsExternalInit,
        RepositoryPaths.IsExternalInitPolyfill,
        PolyfillTypeNames.IsExternalInit,
        PolyfillActivationCode.IsExternalInit,
        TargetFrameworks.NetStandard20);

    public static readonly PolyfillDefinition RequiredMember = new(
        InjectionPropertyNames.RequiredMember,
        RepositoryPaths.RequiredMemberPolyfill,
        PolyfillTypeNames.RequiredMemberAttribute,
        PolyfillActivationCode.RequiredMember,
        TargetFrameworks.NetStandard20);

    public static readonly PolyfillDefinition CompilerFeatureRequired = new(
        InjectionPropertyNames.CompilerFeatureRequired,
        RepositoryPaths.CompilerFeatureRequiredPolyfill,
        PolyfillTypeNames.CompilerFeatureRequiredAttribute,
        PolyfillActivationCode.CompilerFeatureRequired,
        TargetFrameworks.NetStandard20);

    public static readonly PolyfillDefinition CallerArgumentExpression = new(
        InjectionPropertyNames.CallerArgumentExpression,
        RepositoryPaths.CallerArgumentExpressionPolyfill,
        PolyfillTypeNames.CallerArgumentExpressionAttribute,
        PolyfillActivationCode.CallerArgumentExpression,
        TargetFrameworks.NetStandard20);

    public static readonly PolyfillDefinition ParamCollection = new(
        InjectionPropertyNames.ParamCollection,
        RepositoryPaths.ParamCollectionPolyfill,
        PolyfillTypeNames.ParamCollectionAttribute,
        PolyfillActivationCode.ParamCollection,
        TargetFrameworks.Net80);

    public static readonly PolyfillDefinition UnreachableException = new(
        InjectionPropertyNames.UnreachableException,
        RepositoryPaths.UnreachableExceptionPolyfill,
        PolyfillTypeNames.UnreachableException,
        PolyfillActivationCode.UnreachableException,
        TargetFrameworks.NetStandard20);

    public static readonly PolyfillDefinition StackTraceHidden = new(
        InjectionPropertyNames.StackTraceHidden,
        RepositoryPaths.StackTraceHiddenPolyfill,
        PolyfillTypeNames.StackTraceHiddenAttribute,
        PolyfillActivationCode.StackTraceHidden,
        TargetFrameworks.NetStandard20);

    public static readonly PolyfillDefinition NullableAttributes = new(
        InjectionPropertyNames.NullableAttributes,
        RepositoryPaths.NullableAttributesPolyfill,
        PolyfillTypeNames.AllowNullAttribute,
        PolyfillActivationCode.AllowNull,
        TargetFrameworks.NetStandard20);

    public static readonly PolyfillDefinition TrimAttributes = new(
        InjectionPropertyNames.TrimAttributes,
        RepositoryPaths.TrimAttributesPolyfill,
        PolyfillTypeNames.DynamicallyAccessedMembersAttribute,
        PolyfillActivationCode.DynamicallyAccessedMembers,
        TargetFrameworks.NetStandard20);

    public static readonly PolyfillDefinition ExperimentalAttribute = new(
        InjectionPropertyNames.ExperimentalAttribute,
        RepositoryPaths.ExperimentalAttributePolyfill,
        PolyfillTypeNames.ExperimentalAttribute,
        PolyfillActivationCode.Experimental,
        TargetFrameworks.NetStandard20);

    public static readonly PolyfillDefinition Throw = new(
        InjectionPropertyNames.Throw,
        RepositoryPaths.ThrowHelper,
        "Microsoft.Shared.Diagnostics.Throw",
        "_ = Microsoft.Shared.Diagnostics.Throw.IfNull((object?)null);",
        TargetFrameworks.NetStandard20);

    public static readonly PolyfillDefinition StringOrdinalComparer = new(
        InjectionPropertyNames.StringOrdinalComparer,
        RepositoryPaths.StringOrdinalComparer,
        "ANcpLua.NET.Sdk.shared.Extensions.Comparers.StringOrdinalComparer",
        "_ = ANcpLua.NET.Sdk.shared.Extensions.Comparers.StringOrdinalComparer.Instance;",
        TargetFrameworks.NetStandard20);

    public static readonly PolyfillDefinition DiagnosticClasses = new(
        InjectionPropertyNames.DiagnosticClasses,
        RepositoryPaths.DiagnosticClassesPolyfill,
        "System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute",
        "_ = typeof(System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute);",
        TargetFrameworks.NetStandard20);

    public static ImmutableArray<PolyfillDefinition> All =>
    [
        Lock, TimeProvider, IndexRange, IsExternalInit,
        RequiredMember, CompilerFeatureRequired, CallerArgumentExpression, ParamCollection,
        UnreachableException, StackTraceHidden, NullableAttributes, TrimAttributes, ExperimentalAttribute,
        Throw, StringOrdinalComparer, DiagnosticClasses
    ];
}

public readonly record struct RepositoryRoot
{
    private RepositoryRoot(FullPath path)
    {
        FullPath = path;
    }

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

    public static implicit operator FullPath(RepositoryRoot root)
    {
        return root.FullPath;
    }

    public static implicit operator string(RepositoryRoot root)
    {
        return root.FullPath;
    }
}

public sealed class MsBuildPropertyBuilder : Dictionary<string, string?>
{
    public static MsBuildPropertyBuilder Create()
    {
        return new MsBuildPropertyBuilder();
    }

    public MsBuildPropertyBuilder Set(string property, string? value)
    {
        this[property] = value;
        return this;
    }

    public MsBuildPropertyBuilder WithTargetFramework(string tfm)
    {
        return Set(MsBuildProperties.TargetFramework, tfm);
    }

    public MsBuildPropertyBuilder WithLangVersion(string version)
    {
        return Set(MsBuildProperties.LangVersion, version);
    }

    public MsBuildPropertyBuilder WithOutputType(string type)
    {
        return Set(MsBuildProperties.OutputType, type);
    }

    public MsBuildPropertyBuilder WithNullable(bool enable = true)
    {
        return Set(MsBuildProperties.Nullable, enable ? MsBuildValues.Enable : MsBuildValues.Disable);
    }

    public MsBuildPropertyBuilder Enable(string property)
    {
        return Set(property, MsBuildValues.True);
    }

    public MsBuildPropertyBuilder Disable(string property)
    {
        return Set(property, MsBuildValues.False);
    }

    public MsBuildPropertyBuilder ForPolyfill(PolyfillDefinition polyfill, string? targetFrameworkOverride = null)
    {
        return WithTargetFramework(targetFrameworkOverride ?? polyfill.MinimumTargetFramework)
            .WithOutputType(MsBuildValues.Library)
            .Enable(polyfill.InjectionProperty);
    }

    public (string Name, string Value)[] ToPropertyArray()
    {
        return this.Where(kv => kv.Value is not null).Select(kv => (kv.Key, kv.Value!)).ToArray();
    }

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
    public static string TargetFramework(string tfm)
    {
        return $"<{MsBuildProperties.TargetFramework}>{tfm}</{MsBuildProperties.TargetFramework}>";
    }

    public static string LangVersion(string version)
    {
        return $"<{MsBuildProperties.LangVersion}>{version}</{MsBuildProperties.LangVersion}>";
    }

    public static string OutputType(string type)
    {
        return $"<{MsBuildProperties.OutputType}>{type}</{MsBuildProperties.OutputType}>";
    }

    public static string Property(string name, string value)
    {
        return $"<{name}>{value}</{name}>";
    }
}

public static class PolyfillTestDataSource
{
    public static TheoryData<string> AllTargetFrameworks =>
    [
        TargetFrameworks.NetStandard20,
        TargetFrameworks.Net100
    ];

    public static TheoryData<string> AllLangVersions =>
    [
        "12",
        "13",
        "14",
        MsBuildValues.Latest
    ];

    public static TheoryData<PolyfillDefinition, string, bool> InjectionMatrix()
    {
        var data = new TheoryData<PolyfillDefinition, string, bool>();
        foreach (var polyfill in PolyfillDefinition.All)
        {
            data.Add(polyfill, polyfill.MinimumTargetFramework, true);

            var isExtension = polyfill.InjectionProperty is InjectionPropertyNames.Throw or InjectionPropertyNames.StringOrdinalComparer;

            data.Add(polyfill, TargetFrameworks.Net100, isExtension);
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

public interface IInjectedFile
{
    static abstract string RepoRelativePath { get; }
    static abstract string InjectPropertyName { get; }
}

public interface IPolyfillMarker : IInjectedFile
{
    static abstract string ExpectedType { get; }
    static abstract string ActivationSnippet { get; }
}

public sealed class ThrowFile : IPolyfillMarker
{
    public static string RepoRelativePath => RepositoryPaths.ThrowHelper;
    public static string InjectPropertyName => InjectionPropertyNames.Throw;
    public static string ExpectedType => "Microsoft.Shared.Diagnostics.Throw";
    public static string ActivationSnippet => "_ = Microsoft.Shared.Diagnostics.Throw.IfNull((object?)null);";
}

public sealed class StringOrdinalComparerFile : IPolyfillMarker
{
    public static string RepoRelativePath => RepositoryPaths.StringOrdinalComparer;
    public static string InjectPropertyName => InjectionPropertyNames.StringOrdinalComparer;
    public static string ExpectedType => "ANcpLua.NET.Sdk.shared.Extensions.Comparers.StringOrdinalComparer";

    public static string ActivationSnippet =>
        "_ = ANcpLua.NET.Sdk.shared.Extensions.Comparers.StringOrdinalComparer.Instance;";
}

public sealed class DiagnosticClassesFile : IPolyfillMarker
{
    public static string RepoRelativePath => RepositoryPaths.DiagnosticClassesPolyfill;
    public static string InjectPropertyName => InjectionPropertyNames.DiagnosticClasses;
    public static string ExpectedType => "System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute";

    public static string ActivationSnippet =>
        "_ = typeof(System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute);";
}

public sealed class LockFile : IPolyfillMarker
{
    public static string RepoRelativePath => RepositoryPaths.LockPolyfill;
    public static string InjectPropertyName => InjectionPropertyNames.Lock;
    public static string ExpectedType => PolyfillTypeNames.Lock;
    public static string ActivationSnippet => PolyfillActivationCode.Lock;
}

public sealed class TimeProviderFile : IPolyfillMarker
{
    public static string RepoRelativePath => RepositoryPaths.TimeProviderPolyfill;
    public static string InjectPropertyName => InjectionPropertyNames.TimeProvider;
    public static string ExpectedType => PolyfillTypeNames.TimeProvider;
    public static string ActivationSnippet => PolyfillActivationCode.TimeProvider;
}

public sealed class IndexRangeFile : IPolyfillMarker
{
    public static string RepoRelativePath => RepositoryPaths.IndexPolyfill;
    public static string InjectPropertyName => InjectionPropertyNames.IndexRange;
    public static string ExpectedType => PolyfillTypeNames.Index;
    public static string ActivationSnippet => PolyfillActivationCode.Index;
}

public sealed class IsExternalInitFile : IPolyfillMarker
{
    public static string RepoRelativePath => RepositoryPaths.IsExternalInitPolyfill;
    public static string InjectPropertyName => InjectionPropertyNames.IsExternalInit;
    public static string ExpectedType => PolyfillTypeNames.IsExternalInit;
    public static string ActivationSnippet => PolyfillActivationCode.IsExternalInit;
}

public sealed class RequiredMemberFile : IPolyfillMarker
{
    public static string RepoRelativePath => RepositoryPaths.RequiredMemberPolyfill;
    public static string InjectPropertyName => InjectionPropertyNames.RequiredMember;
    public static string ExpectedType => PolyfillTypeNames.RequiredMemberAttribute;
    public static string ActivationSnippet => PolyfillActivationCode.RequiredMember;
}

public sealed class CompilerFeatureRequiredFile : IPolyfillMarker
{
    public static string RepoRelativePath => RepositoryPaths.CompilerFeatureRequiredPolyfill;
    public static string InjectPropertyName => InjectionPropertyNames.CompilerFeatureRequired;
    public static string ExpectedType => PolyfillTypeNames.CompilerFeatureRequiredAttribute;
    public static string ActivationSnippet => PolyfillActivationCode.CompilerFeatureRequired;
}

public sealed class CallerArgumentExpressionFile : IPolyfillMarker
{
    public static string RepoRelativePath => RepositoryPaths.CallerArgumentExpressionPolyfill;
    public static string InjectPropertyName => InjectionPropertyNames.CallerArgumentExpression;
    public static string ExpectedType => PolyfillTypeNames.CallerArgumentExpressionAttribute;
    public static string ActivationSnippet => PolyfillActivationCode.CallerArgumentExpression;
}

public sealed class ParamCollectionFile : IPolyfillMarker
{
    public static string RepoRelativePath => RepositoryPaths.ParamCollectionPolyfill;
    public static string InjectPropertyName => InjectionPropertyNames.ParamCollection;
    public static string ExpectedType => PolyfillTypeNames.ParamCollectionAttribute;
    public static string ActivationSnippet => PolyfillActivationCode.ParamCollection;
}

public sealed class UnreachableExceptionFile : IPolyfillMarker
{
    public static string RepoRelativePath => RepositoryPaths.UnreachableExceptionPolyfill;
    public static string InjectPropertyName => InjectionPropertyNames.UnreachableException;
    public static string ExpectedType => PolyfillTypeNames.UnreachableException;
    public static string ActivationSnippet => PolyfillActivationCode.UnreachableException;
}

public sealed class StackTraceHiddenFile : IPolyfillMarker
{
    public static string RepoRelativePath => RepositoryPaths.StackTraceHiddenPolyfill;
    public static string InjectPropertyName => InjectionPropertyNames.StackTraceHidden;
    public static string ExpectedType => PolyfillTypeNames.StackTraceHiddenAttribute;
    public static string ActivationSnippet => PolyfillActivationCode.StackTraceHidden;
}

public sealed class NullabilityAttributesFile : IPolyfillMarker
{
    public static string RepoRelativePath => RepositoryPaths.NullableAttributesPolyfill;
    public static string InjectPropertyName => InjectionPropertyNames.NullableAttributes;
    public static string ExpectedType => PolyfillTypeNames.AllowNullAttribute;
    public static string ActivationSnippet => PolyfillActivationCode.AllowNull;
}

public sealed class TrimAttributesFile : IPolyfillMarker
{
    public static string RepoRelativePath => RepositoryPaths.TrimAttributesPolyfill;
    public static string InjectPropertyName => InjectionPropertyNames.TrimAttributes;
    public static string ExpectedType => PolyfillTypeNames.DynamicallyAccessedMembersAttribute;
    public static string ActivationSnippet => PolyfillActivationCode.DynamicallyAccessedMembers;
}

public sealed class ExperimentalAttributeFile : IPolyfillMarker
{
    public static string RepoRelativePath => RepositoryPaths.ExperimentalAttributePolyfill;
    public static string InjectPropertyName => InjectionPropertyNames.ExperimentalAttribute;
    public static string ExpectedType => PolyfillTypeNames.ExperimentalAttribute;
    public static string ActivationSnippet => PolyfillActivationCode.Experimental;
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