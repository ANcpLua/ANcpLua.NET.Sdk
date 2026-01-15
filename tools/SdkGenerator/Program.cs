using Meziantou.Framework;

var rootFolder = GetRootFolderPath();
var sdkRootPath = rootFolder / "src" / "Sdk";

var sdks = new (string SdkName, string BaseSdkName)[]
{
    ("ANcpLua.NET.Sdk", "Microsoft.NET.Sdk"), ("ANcpLua.NET.Sdk.Web", "Microsoft.NET.Sdk.Web")
};

foreach (var (sdkName, baseSdkName) in sdks)
{
    var propsPath = sdkRootPath / sdkName / "Sdk.props";
    var targetsPath = sdkRootPath / sdkName / "Sdk.targets";

    propsPath.CreateParentDirectory();

    // Sdk.props
    File.WriteAllText(propsPath.Value, $"""
                                        <Project>
                                          <PropertyGroup>
                                            <ANcpLuaSdkName>{sdkName}</ANcpLuaSdkName>
                                            <_MustImportMicrosoftNETSdk Condition="'$(UsingMicrosoftNETSdk)' != 'true'">true</_MustImportMicrosoftNETSdk>

                                            <CustomBeforeDirectoryBuildProps>$(CustomBeforeDirectoryBuildProps);$(MSBuildThisFileDirectory)../common/Common.props</CustomBeforeDirectoryBuildProps>
                                            <BeforeMicrosoftNETSdkTargets>$(BeforeMicrosoftNETSdkTargets);$(MSBuildThisFileDirectory)../common/Common.targets</BeforeMicrosoftNETSdkTargets>
                                          </PropertyGroup>

                                          <Import Project="Sdk.props" Sdk="{baseSdkName}" Condition="'$(_MustImportMicrosoftNETSdk)' == 'true'"/>
                                          <Import Project="$(MSBuildThisFileDirectory)../common/Common.props" Condition="'$(_MustImportMicrosoftNETSdk)' != 'true'"/>
                                        </Project>
                                        """);

    // Sdk.targets
    File.WriteAllText(targetsPath.Value, $"""
                                          <Project>
                                            <Import Project="Sdk.targets" Sdk="{baseSdkName}" Condition="'$(_MustImportMicrosoftNETSdk)' == 'true'"/>
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
