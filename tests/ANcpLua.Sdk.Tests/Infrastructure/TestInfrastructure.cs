using System.Collections.Immutable;
using Xunit.Sdk;

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
    public const string CompilerFeatureRequiredPolyfill = "eng/LegacySupport/LanguageFeatures/CompilerFeatureRequiredAttribute.cs";
    public const string CallerArgumentExpressionPolyfill = "eng/LegacySupport/LanguageFeatures/CallerArgumentExpressionAttribute.cs";
    public const string ParamCollectionPolyfill = "eng/LegacySupport/LanguageFeatures/ParamCollectionAttribute.cs";
    public const string UnreachableExceptionPolyfill = "eng/LegacySupport/Exceptions/UnreachableException.cs";
    public const string StackTraceHiddenPolyfill = "eng/LegacySupport/Diagnostics/StackTraceHiddenAttribute.cs";
    public const string NullableAttributesPolyfill = "eng/LegacySupport/DiagnosticAttributes/NullableAttributes.cs";
    public const string TrimAttributesPolyfill = "eng/LegacySupport/TrimAttributes/DynamicallyAccessedMembersAttribute.cs";
    public const string ExperimentalAttributePolyfill = "eng/LegacySupport/Experimental/ExperimentalAttribute.cs";
}

/// <summary>
///     Single source of truth for polyfill test data.
///     Implements IXunitSerializable for Test Explorer enumeration.
/// </summary>
public sealed class PolyfillDefinition : IXunitSerializable
{
    [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
    public PolyfillDefinition()
    {
    }

    public string InjectionProperty { get; private set; } = "";
    public string RepositoryPath { get; private set; } = "";
    public string MinimumTargetFramework { get; private set; } = "";
    public string ActivationCode { get; private set; } = "";
    public string ExpectedType { get; private set; } = "";
    public bool HasNegativeTest { get; private set; } = true;
    public bool RequiresLangVersionLatest { get; private set; }
    public bool DisablesSharedThrowForNegative { get; private set; }

    public PolyfillDefinition(
        string injectionProperty,
        string repositoryPath,
        string minimumTargetFramework,
        string activationCode,
        string expectedType,
        bool hasNegativeTest = true,
        bool requiresLangVersionLatest = false,
        bool disablesSharedThrowForNegative = false)
    {
        InjectionProperty = injectionProperty;
        RepositoryPath = repositoryPath;
        MinimumTargetFramework = minimumTargetFramework;
        ActivationCode = activationCode;
        ExpectedType = expectedType;
        HasNegativeTest = hasNegativeTest;
        RequiresLangVersionLatest = requiresLangVersionLatest;
        DisablesSharedThrowForNegative = disablesSharedThrowForNegative;
    }

    void IXunitSerializable.Deserialize(IXunitSerializationInfo info)
    {
        InjectionProperty = info.GetValue<string>(nameof(InjectionProperty))!;
        RepositoryPath = info.GetValue<string>(nameof(RepositoryPath))!;
        MinimumTargetFramework = info.GetValue<string>(nameof(MinimumTargetFramework))!;
        ActivationCode = info.GetValue<string>(nameof(ActivationCode))!;
        ExpectedType = info.GetValue<string>(nameof(ExpectedType))!;
        HasNegativeTest = info.GetValue<bool>(nameof(HasNegativeTest));
        RequiresLangVersionLatest = info.GetValue<bool>(nameof(RequiresLangVersionLatest));
        DisablesSharedThrowForNegative = info.GetValue<bool>(nameof(DisablesSharedThrowForNegative));
    }

    void IXunitSerializable.Serialize(IXunitSerializationInfo info)
    {
        info.AddValue(nameof(InjectionProperty), InjectionProperty);
        info.AddValue(nameof(RepositoryPath), RepositoryPath);
        info.AddValue(nameof(MinimumTargetFramework), MinimumTargetFramework);
        info.AddValue(nameof(ActivationCode), ActivationCode);
        info.AddValue(nameof(ExpectedType), ExpectedType);
        info.AddValue(nameof(HasNegativeTest), HasNegativeTest);
        info.AddValue(nameof(RequiresLangVersionLatest), RequiresLangVersionLatest);
        info.AddValue(nameof(DisablesSharedThrowForNegative), DisablesSharedThrowForNegative);
    }

    public static ImmutableArray<PolyfillDefinition> All { get; } =
    [
        new(SdkProp.InjectTrimAttributesOnLegacy, RepositoryPaths.TrimAttributesPolyfill, Tfm.NetStandard20,
            "_ = typeof(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute);",
            "System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute"),

        new(SdkProp.InjectNullabilityAttributesOnLegacy, RepositoryPaths.NullableAttributesPolyfill, Tfm.NetStandard20,
            "_ = typeof(System.Diagnostics.CodeAnalysis.AllowNullAttribute);",
            "System.Diagnostics.CodeAnalysis.AllowNullAttribute",
            disablesSharedThrowForNegative: true),

        new(SdkProp.InjectIsExternalInitOnLegacy, RepositoryPaths.IsExternalInitPolyfill, Tfm.NetStandard20,
            "_ = typeof(System.Runtime.CompilerServices.IsExternalInit);",
            "System.Runtime.CompilerServices.IsExternalInit"),

        new(SdkProp.InjectRequiredMemberOnLegacy, RepositoryPaths.RequiredMemberPolyfill, Tfm.NetStandard20,
            "_ = typeof(System.Runtime.CompilerServices.RequiredMemberAttribute);",
            "System.Runtime.CompilerServices.RequiredMemberAttribute",
            requiresLangVersionLatest: true),

        new(SdkProp.InjectCompilerFeatureRequiredOnLegacy, RepositoryPaths.CompilerFeatureRequiredPolyfill, Tfm.NetStandard20,
            "_ = typeof(System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute);",
            "System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute",
            requiresLangVersionLatest: true),

        new(SdkProp.InjectCallerAttributesOnLegacy, RepositoryPaths.CallerArgumentExpressionPolyfill, Tfm.NetStandard20,
            "_ = typeof(System.Runtime.CompilerServices.CallerArgumentExpressionAttribute);",
            "System.Runtime.CompilerServices.CallerArgumentExpressionAttribute",
            disablesSharedThrowForNegative: true),

        new(SdkProp.InjectUnreachableExceptionOnLegacy, RepositoryPaths.UnreachableExceptionPolyfill, Tfm.NetStandard20,
            "_ = new System.Diagnostics.UnreachableException();",
            "System.Diagnostics.UnreachableException"),

        new(SdkProp.InjectExperimentalAttributeOnLegacy, RepositoryPaths.ExperimentalAttributePolyfill, Tfm.NetStandard20,
            "_ = typeof(System.Diagnostics.CodeAnalysis.ExperimentalAttribute);",
            "System.Diagnostics.CodeAnalysis.ExperimentalAttribute"),

        new(SdkProp.InjectIndexRangeOnLegacy, RepositoryPaths.IndexPolyfill, Tfm.NetStandard20,
            "_ = new System.Index(1);",
            "System.Index"),

        new(SdkProp.InjectParamCollectionOnLegacy, RepositoryPaths.ParamCollectionPolyfill, Tfm.NetStandard20,
            "_ = typeof(System.Runtime.CompilerServices.ParamCollectionAttribute);",
            "System.Runtime.CompilerServices.ParamCollectionAttribute"),

        new(SdkProp.InjectStackTraceHiddenOnLegacy, RepositoryPaths.StackTraceHiddenPolyfill, Tfm.NetStandard20,
            "_ = typeof(System.Diagnostics.StackTraceHiddenAttribute);",
            "System.Diagnostics.StackTraceHiddenAttribute"),

        new(SdkProp.InjectLockPolyfill, RepositoryPaths.LockPolyfill, Tfm.NetStandard20,
            "_ = new System.Threading.Lock();",
            "System.Threading.Lock"),

        new(SdkProp.InjectTimeProviderPolyfill, RepositoryPaths.TimeProviderPolyfill, Tfm.NetStandard20,
            "_ = typeof(System.TimeProvider);",
            "System.TimeProvider"),

        new(SdkProp.InjectSharedThrow, RepositoryPaths.ThrowHelper, Tfm.NetStandard20,
            "_ = Microsoft.Shared.Diagnostics.Throw.IfNull((object?)null);",
            "Microsoft.Shared.Diagnostics.Throw",
            disablesSharedThrowForNegative: true),

        new(SdkProp.InjectStringOrdinalComparer, RepositoryPaths.StringOrdinalComparer, Tfm.NetStandard20,
            "_ = ANcpLua.NET.Sdk.Shared.Extensions.Comparers.StringOrdinalComparer.Instance;",
            "ANcpLua.NET.Sdk.Shared.Extensions.Comparers.StringOrdinalComparer"),

        new(SdkProp.InjectDiagnosticClassesOnLegacy, RepositoryPaths.DiagnosticClassesPolyfill, Tfm.NetStandard20,
            "_ = typeof(System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute);",
            "System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute",
            hasNegativeTest: false)
    ];

    public override string ToString() => $"{Path.GetFileNameWithoutExtension(RepositoryPath)} → {MinimumTargetFramework}";
}

/// <summary>
///     Single source of truth for all SDK branding strings.
/// </summary>
public static class SdkBrandingConstants
{
    public const string Author = "ANcpLua";
    public const string SdkMetadataKey = "ANcpLua.Sdk.Name";
}

public static class PolyfillTestDataSource
{
    public static TheoryData<PolyfillDefinition, string, bool> InjectionMatrix()
    {
        var data = new TheoryData<PolyfillDefinition, string, bool>();
        foreach (var polyfill in PolyfillDefinition.All)
        {
            data.Add(polyfill, polyfill.MinimumTargetFramework, true);

            var isExtension = polyfill.InjectionProperty is SdkProp.InjectSharedThrow or SdkProp.InjectStringOrdinalComparer;
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
