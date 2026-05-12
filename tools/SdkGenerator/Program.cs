using Meziantou.Framework;

var rootFolder = GetRootFolderPath();
var sdkRootPath = rootFolder / "src" / "Sdk";

var sdks = new (string SdkName, string BaseSdkName, string VariantPropsLines)[]
{
    ("ANcpLua.NET.Sdk", "Microsoft.NET.Sdk", ""),
    ("ANcpLua.NET.Sdk.Web", "Microsoft.NET.Sdk.Web", ""),
    // .Test sets OutputType=Exe at props-phase so `dotnet test`, which evaluates
    // OutputType statically before any target fires, sees Exe. Tests.targets also
    // sets this inside a build-time target, but that's too late for `dotnet test`'s
    // pre-build check and too late for the CSC executable-host decision.
    //
    // The condition handles all three SdkImportStyles:
    //   ProjectElement (<Project Sdk="ANcpLua.NET.Sdk.Test">) — our Sdk.props runs
    //     before the inner Microsoft.NET.Sdk import; OutputType is empty here, so
    //     the OutputType=='' branch fires.
    //   SdkElement (<Sdk Name="ANcpLua.NET.Sdk.Test"/> inside a Microsoft.NET.Sdk
    //     project) and SdkElementDirectoryBuildProps — Microsoft.NET.Sdk's Sdk.props
    //     already applied OutputType=Library by the time our .Test Sdk.props runs,
    //     so the OutputType=='Library' branch fires.
    //
    // Consumer override: a csproj <OutputType>Library</OutputType> in the project's
    // PropertyGroup body runs *after* this Sdk.props set and always wins, which is
    // the documented escape hatch for the rare VSTest-on-.Test-SDK scenario.
    // SafetyGuard_WarnsWhenMTPWithLibraryOutputType verifies that path stays intact.
    //
    // No UseMicrosoftTestingPlatform gate: we can't read csproj-set properties from
    // Sdk.props (they evaluate later), and `.Test` SDK is documented as MTP-only
    // (see MtpDetectionTests class doc). Consumers wanting VSTest-Library set
    // <OutputType>Library</OutputType> explicitly — the same contract as
    // Microsoft.NET.Sdk.Web (web projects are Exe; want a library? use the base SDK).
    ("ANcpLua.NET.Sdk.Test", "Microsoft.NET.Sdk",
        "\n    <OutputType Condition=\"'$(OutputType)' == '' OR '$(OutputType)' == 'Library'\">Exe</OutputType>"),
    ("ANcpLua.NET.Sdk.BitNet", "Microsoft.NET.Sdk.Web", ""),
};

foreach (var (sdkName, baseSdkName, variantPropsLines) in sdks)
{
    var propsPath = sdkRootPath / sdkName / "Sdk.props";
    var targetsPath = sdkRootPath / sdkName / "Sdk.targets";

    propsPath.CreateParentDirectory();

    File.WriteAllText(propsPath.Value, $"""
                                        <Project>
                                          <PropertyGroup>
                                            <ANcpLuaSdkName>{sdkName}</ANcpLuaSdkName>
                                            <_MustImportMicrosoftNETSdk Condition="'$(UsingMicrosoftNETSdk)' != 'true'">true</_MustImportMicrosoftNETSdk>{variantPropsLines}

                                            <CustomBeforeDirectoryBuildProps>$(CustomBeforeDirectoryBuildProps);$(MSBuildThisFileDirectory)..\Build\Common\Common.props</CustomBeforeDirectoryBuildProps>
                                            <BeforeMicrosoftNETSdkTargets>$(BeforeMicrosoftNETSdkTargets);$(MSBuildThisFileDirectory)..\Build\Common\Common.targets</BeforeMicrosoftNETSdkTargets>
                                          </PropertyGroup>

                                          <Import Project="$(MSBuildThisFileDirectory)..\Build\Common\Version.props" />
                                          <Import Project="$(MSBuildThisFileDirectory)..\Build\Common\GlobalPackages.props" />

                                          <Import Project="Sdk.props" Sdk="{baseSdkName}" Condition="'$(_MustImportMicrosoftNETSdk)' == 'true'" />
                                          <Import Project="$(MSBuildThisFileDirectory)..\Build\Common\Common.props" Condition="'$(_MustImportMicrosoftNETSdk)' != 'true'" />

                                          <Import Project="$(MSBuildThisFileDirectory)..\Build\Enforcement\Enforcement.props" />
                                          <Import Project="$(MSBuildThisFileDirectory)..\Build\Enforcement\DeterminismAndSourceLink.props" />
                                        </Project>

                                        """);

    File.WriteAllText(targetsPath.Value, $"""
                                          <Project>
                                            <Import Project="Sdk.targets" Sdk="{baseSdkName}" Condition="'$(_MustImportMicrosoftNETSdk)' == 'true'" />
                                          </Project>

                                          """);

    Console.WriteLine($"Generated {sdkName}");
}

Console.WriteLine();
Console.WriteLine("Note: .nuspec and .csproj files are maintained manually in src/");

static FullPath GetRootFolderPath()
{
    var path = FullPath.CurrentDirectory();
    while (!path.IsEmpty)
    {
        if (Directory.Exists(path / ".git"))
            return path;

        path = path.Parent;
    }

    return path.IsEmpty ? throw new InvalidOperationException("Cannot find the root folder") : path;
}
