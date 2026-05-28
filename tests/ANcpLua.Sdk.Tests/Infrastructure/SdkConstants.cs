namespace ANcpLua.Sdk.Tests.Infrastructure;

public enum NetSdkVersion
{
    Net100
}

public enum SdkImportStyle
{
    ProjectElement,
    SdkElement,
    SdkElementDirectoryBuildProps
}

public readonly record struct NuGetReference(string Name, string Version);

public static class Tfm
{
    public const string NetStandard20 = "netstandard2.0";
    public const string Net80 = "net8.0";
    public const string Net100 = "net10.0";
}

public static class Prop
{
    public const string TargetFramework = "TargetFramework";
    public const string OutputType = "OutputType";
    public const string LangVersion = "LangVersion";
}

public static class Val
{
    public const string Library = "Library";
    public const string Exe = "Exe";
    public const string Latest = "latest";
}

public static class SdkBrandingConstants
{
    public const string Author = "ANcpLua";
    public const string SdkMetadataKey = "ANcpLua.Sdk.Name";
}
