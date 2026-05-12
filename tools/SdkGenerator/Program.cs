using Meziantou.Framework;

var rootFolder = GetRootFolderPath();
var sdkRootPath = rootFolder / "src" / "Sdk";

var sdks = new (string SdkName, string BaseSdkName, string VariantPropsLines)[]
{
    ("ANcpLua.NET.Sdk", "Microsoft.NET.Sdk", ""),
    ("ANcpLua.NET.Sdk.Web", "Microsoft.NET.Sdk.Web", ""),
    // .Test sets OutputType=Exe at props-phase (before Microsoft.NET.Sdk applies
    // its Library default) so `dotnet test`, which evaluates OutputType statically
    // before any target fires, sees Exe. Tests.targets also sets this inside a
    // build-time target, but that's too late for `dotnet test`'s pre-build check.
    ("ANcpLua.NET.Sdk.Test", "Microsoft.NET.Sdk",
        "\n    <OutputType Condition=\"'$(OutputType)' == ''\">Exe</OutputType>"),
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
