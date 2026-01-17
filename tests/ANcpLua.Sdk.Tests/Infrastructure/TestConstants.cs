namespace ANcpLua.Sdk.Tests.Infrastructure;

/// <summary>Target framework monikers - only test polyfill boundaries + LTS</summary>
public static class Tfm
{
    public const string NetStandard20 = "netstandard2.0";
    public const string Net100 = "net10.0";
}

/// <summary>MSBuild property names</summary>
public static class Prop
{
    public const string TargetFramework = "TargetFramework";
    public const string TargetFrameworks = "TargetFrameworks";
    public const string OutputType = "OutputType";
    public const string Nullable = "Nullable";
    public const string ImplicitUsings = "ImplicitUsings";
    public const string LangVersion = "LangVersion";
    public const string TreatWarningsAsErrors = "TreatWarningsAsErrors";
    public const string IsPackable = "IsPackable";
    public const string GenerateDocumentationFile = "GenerateDocumentationFile";
    public const string ManagePackageVersionsCentrally = "ManagePackageVersionsCentrally";

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

/// <summary>MSBuild property values</summary>
public static class Val
{
    public const string Library = "Library";
    public const string Exe = "Exe";
    public const string True = "true";
    public const string False = "false";
    public const string Enable = "enable";
    public const string Disable = "disable";
    public const string Latest = "latest";
    public const string Preview = "preview";
}

/// <summary>MSBuild item names</summary>
public static class Item
{
    public const string PackageReference = "PackageReference";
    public const string PackageVersion = "PackageVersion";
    public const string ProjectReference = "ProjectReference";
    public const string Compile = "Compile";
    public const string Content = "Content";
    public const string None = "None";
    public const string EmbeddedResource = "EmbeddedResource";
}

/// <summary>MSBuild attribute names</summary>
public static class Attr
{
    public const string Include = "Include";
    public const string Version = "Version";
    public const string PrivateAssets = "PrivateAssets";
    public const string Condition = "Condition";
}
