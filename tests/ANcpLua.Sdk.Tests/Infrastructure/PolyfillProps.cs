namespace ANcpLua.Sdk.Tests.Infrastructure;

/// <summary>
///     SDK-specific polyfill injection MSBuild property names.
/// </summary>
/// <remarks>
///     Contains only SDK-specific polyfill properties. Common properties (TargetFramework, OutputType, etc.)
///     come from <see cref="ANcpLua.Roslyn.Utilities.Testing.MSBuild.Prop"/>.
/// </remarks>
public static class SdkProp
{
    // SDK-specific polyfill injection properties
    public const string InjectSharedThrow = "InjectSharedThrow";
    public const string InjectStringOrdinalComparer = "InjectStringOrdinalComparer";
    public const string InjectLockPolyfill = "InjectLockPolyfill";
    public const string InjectTimeProviderPolyfill = "InjectTimeProviderPolyfill";
    public const string InjectIndexRangeOnLegacy = "InjectIndexRangeOnLegacy";
    public const string InjectIsExternalInitOnLegacy = "InjectIsExternalInitOnLegacy";
    public const string InjectRequiredMemberOnLegacy = "InjectRequiredMemberOnLegacy";
    public const string InjectCompilerFeatureRequiredOnLegacy = "InjectCompilerFeatureRequiredOnLegacy";
    public const string InjectCallerAttributesOnLegacy = "InjectCallerAttributesOnLegacy";
    public const string InjectParamCollectionOnLegacy = "InjectParamCollectionOnLegacy";
    public const string InjectUnreachableExceptionOnLegacy = "InjectUnreachableExceptionOnLegacy";
    public const string InjectStackTraceHiddenOnLegacy = "InjectStackTraceHiddenOnLegacy";
    public const string InjectNullabilityAttributesOnLegacy = "InjectNullabilityAttributesOnLegacy";
    public const string InjectTrimAttributesOnLegacy = "InjectTrimAttributesOnLegacy";
    public const string InjectExperimentalAttributeOnLegacy = "InjectExperimentalAttributeOnLegacy";
    public const string InjectDiagnosticClassesOnLegacy = "InjectDiagnosticClassesOnLegacy";
}
