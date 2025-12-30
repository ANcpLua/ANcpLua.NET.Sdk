using System.Collections.Immutable;
using System.Xml.Linq;
using Meziantou.Framework;

namespace ANcpLua.Sdk.Tests.Infrastructure;

// Legacy aliases for backwards compatibility - use Tfm, Prop, Val, Item, Attr instead
public static class TargetFrameworks
{
    public const string NetStandard20 = Tfm.NetStandard20;
    public const string Net80 = Tfm.Net80;
    public const string Net100 = Tfm.Net100;
}

public static class MsBuildProperties
{
    public const string TargetFramework = Prop.TargetFramework;
    public const string TargetFrameworks = Prop.TargetFrameworks;
    public const string OutputType = Prop.OutputType;
    public const string Nullable = Prop.Nullable;
    public const string ImplicitUsings = Prop.ImplicitUsings;
    public const string LangVersion = Prop.LangVersion;
    public const string TreatWarningsAsErrors = Prop.TreatWarningsAsErrors;
    public const string IsPackable = Prop.IsPackable;
    public const string GenerateDocumentationFile = Prop.GenerateDocumentationFile;
    public const string ManagePackageVersionsCentrally = Prop.ManagePackageVersionsCentrally;
}

public static class MsBuildItems
{
    public const string PackageReference = Item.PackageReference;
    public const string PackageVersion = Item.PackageVersion;
    public const string ProjectReference = Item.ProjectReference;
    public const string Compile = Item.Compile;
    public const string Content = Item.Content;
    public const string None = Item.None;
    public const string EmbeddedResource = Item.EmbeddedResource;
}

public static class MsBuildAttributes
{
    public const string Include = Attr.Include;
    public const string Version = Attr.Version;
    public const string PrivateAssets = Attr.PrivateAssets;
    public const string Condition = Attr.Condition;
}

public static class MsBuildValues
{
    public const string Exe = Val.Exe;
    public const string Library = Val.Library;
    public const string Enable = Val.Enable;
    public const string Disable = Val.Disable;
    public const string True = Val.True;
    public const string False = Val.False;
    public const string Latest = Val.Latest;
    public const string Preview = Val.Preview;
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
    public const string Throw = Prop.InjectSharedThrow;
    public const string StringOrdinalComparer = Prop.InjectStringOrdinalComparer;
    public const string Lock = Prop.InjectLockPolyfill;
    public const string TimeProvider = Prop.InjectTimeProviderPolyfill;
    public const string IndexRange = Prop.InjectIndexRangeOnLegacy;
    public const string IsExternalInit = Prop.InjectIsExternalInitOnLegacy;
    public const string RequiredMember = Prop.InjectRequiredMemberOnLegacy;
    public const string CompilerFeatureRequired = Prop.InjectCompilerFeatureRequiredOnLegacy;
    public const string CallerArgumentExpression = Prop.InjectCallerAttributesOnLegacy;
    public const string ParamCollection = Prop.InjectParamCollectionOnLegacy;
    public const string UnreachableException = Prop.InjectUnreachableExceptionOnLegacy;
    public const string StackTraceHidden = Prop.InjectStackTraceHiddenOnLegacy;
    public const string NullableAttributes = Prop.InjectNullabilityAttributesOnLegacy;
    public const string TrimAttributes = Prop.InjectTrimAttributesOnLegacy;
    public const string ExperimentalAttribute = Prop.InjectExperimentalAttributeOnLegacy;
    public const string DiagnosticClasses = Prop.InjectDiagnosticClassesOnLegacy;
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
        Tfm.Net80);

    public static readonly PolyfillDefinition TimeProvider = new(
        InjectionPropertyNames.TimeProvider,
        RepositoryPaths.TimeProviderPolyfill,
        PolyfillTypeNames.TimeProvider,
        PolyfillActivationCode.TimeProvider,
        Tfm.NetStandard20);

    public static readonly PolyfillDefinition IndexRange = new(
        InjectionPropertyNames.IndexRange,
        RepositoryPaths.IndexPolyfill,
        PolyfillTypeNames.Index,
        PolyfillActivationCode.Index,
        Tfm.NetStandard20);

    public static readonly PolyfillDefinition IsExternalInit = new(
        InjectionPropertyNames.IsExternalInit,
        RepositoryPaths.IsExternalInitPolyfill,
        PolyfillTypeNames.IsExternalInit,
        PolyfillActivationCode.IsExternalInit,
        Tfm.NetStandard20);

    public static readonly PolyfillDefinition RequiredMember = new(
        InjectionPropertyNames.RequiredMember,
        RepositoryPaths.RequiredMemberPolyfill,
        PolyfillTypeNames.RequiredMemberAttribute,
        PolyfillActivationCode.RequiredMember,
        Tfm.NetStandard20);

    public static readonly PolyfillDefinition CompilerFeatureRequired = new(
        InjectionPropertyNames.CompilerFeatureRequired,
        RepositoryPaths.CompilerFeatureRequiredPolyfill,
        PolyfillTypeNames.CompilerFeatureRequiredAttribute,
        PolyfillActivationCode.CompilerFeatureRequired,
        Tfm.NetStandard20);

    public static readonly PolyfillDefinition CallerArgumentExpression = new(
        InjectionPropertyNames.CallerArgumentExpression,
        RepositoryPaths.CallerArgumentExpressionPolyfill,
        PolyfillTypeNames.CallerArgumentExpressionAttribute,
        PolyfillActivationCode.CallerArgumentExpression,
        Tfm.NetStandard20);

    public static readonly PolyfillDefinition ParamCollection = new(
        InjectionPropertyNames.ParamCollection,
        RepositoryPaths.ParamCollectionPolyfill,
        PolyfillTypeNames.ParamCollectionAttribute,
        PolyfillActivationCode.ParamCollection,
        Tfm.Net80);

    public static readonly PolyfillDefinition UnreachableException = new(
        InjectionPropertyNames.UnreachableException,
        RepositoryPaths.UnreachableExceptionPolyfill,
        PolyfillTypeNames.UnreachableException,
        PolyfillActivationCode.UnreachableException,
        Tfm.NetStandard20);

    public static readonly PolyfillDefinition StackTraceHidden = new(
        InjectionPropertyNames.StackTraceHidden,
        RepositoryPaths.StackTraceHiddenPolyfill,
        PolyfillTypeNames.StackTraceHiddenAttribute,
        PolyfillActivationCode.StackTraceHidden,
        Tfm.NetStandard20);

    public static readonly PolyfillDefinition NullableAttributes = new(
        InjectionPropertyNames.NullableAttributes,
        RepositoryPaths.NullableAttributesPolyfill,
        PolyfillTypeNames.AllowNullAttribute,
        PolyfillActivationCode.AllowNull,
        Tfm.NetStandard20);

    public static readonly PolyfillDefinition TrimAttributes = new(
        InjectionPropertyNames.TrimAttributes,
        RepositoryPaths.TrimAttributesPolyfill,
        PolyfillTypeNames.DynamicallyAccessedMembersAttribute,
        PolyfillActivationCode.DynamicallyAccessedMembers,
        Tfm.NetStandard20);

    public static readonly PolyfillDefinition ExperimentalAttribute = new(
        InjectionPropertyNames.ExperimentalAttribute,
        RepositoryPaths.ExperimentalAttributePolyfill,
        PolyfillTypeNames.ExperimentalAttribute,
        PolyfillActivationCode.Experimental,
        Tfm.NetStandard20);

    public static readonly PolyfillDefinition Throw = new(
        InjectionPropertyNames.Throw,
        RepositoryPaths.ThrowHelper,
        "Microsoft.Shared.Diagnostics.Throw",
        "_ = Microsoft.Shared.Diagnostics.Throw.IfNull((object?)null);",
        Tfm.NetStandard20);

    public static readonly PolyfillDefinition StringOrdinalComparer = new(
        InjectionPropertyNames.StringOrdinalComparer,
        RepositoryPaths.StringOrdinalComparer,
        "ANcpLua.NET.Sdk.Shared.Extensions.Comparers.StringOrdinalComparer",
        "_ = ANcpLua.NET.Sdk.Shared.Extensions.Comparers.StringOrdinalComparer.Instance;",
        Tfm.NetStandard20);

    public static readonly PolyfillDefinition DiagnosticClasses = new(
        InjectionPropertyNames.DiagnosticClasses,
        RepositoryPaths.DiagnosticClassesPolyfill,
        "System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute",
        "_ = typeof(System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute);",
        Tfm.NetStandard20);

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
                polyfill.InjectionProperty is InjectionPropertyNames.Throw
                    or InjectionPropertyNames.StringOrdinalComparer;

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
    public static string ExpectedType => "ANcpLua.NET.Sdk.Shared.Extensions.Comparers.StringOrdinalComparer";

    public static string ActivationSnippet =>
        "_ = ANcpLua.NET.Sdk.Shared.Extensions.Comparers.StringOrdinalComparer.Instance;";
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