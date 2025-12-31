// NUKE Build Target: Local Feed Canary
// Validates SDK packaging catches MSB4099/TFM issues in <10 seconds

using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build : NukeBuild
{
    AbsolutePath ArtifactsNuGetDirectory => RootDirectory / "artifacts" / "nuget";
    AbsolutePath SdkProject => RootDirectory / "src" / "ANcpLua.NET.Sdk.csproj";
    AbsolutePath CanaryProject => RootDirectory / "tests" / "ANcpLua.Sdk.Canary" / "ANcpLua.Sdk.Canary.csproj";

    Target LocalFeedCanary => _ => _
        .Description("Validates SDK package via ephemeral local feed - catches MSB4099/TFM errors in <10s")
        .DependsOn(Compile)
        .Executes(() =>
        {
            // Step 1: Clean artifacts
            ArtifactsNuGetDirectory.CreateOrCleanDirectory();

            // Step 2: Pack SDK to local feed
            DotNetPack(s => s
                .SetProject(SdkProject)
                .SetOutputDirectory(ArtifactsNuGetDirectory)
                .SetConfiguration(Configuration)
                .EnableNoBuild()
                .SetProperty("PackageVersion", "0.0.0-canary"));

            // Step 3: Clear NuGet cache for our package (ensures fresh restore)
            var packageCachePath = EnvironmentInfo.SpecialFolder(SpecialFolders.UserProfile)
                / ".nuget" / "packages" / "ancplua.net.sdk";
            packageCachePath.DeleteDirectory();

            // Step 4: Restore canary project from local feed ONLY
            DotNetRestore(s => s
                .SetProjectFile(CanaryProject)
                .SetProperty("UseLocalSdk", "false")
                .SetProperty("RestorePackagesPath", RootDirectory / "artifacts" / "canary-packages")
                .AddSources(ArtifactsNuGetDirectory)
                .SetConfigFile(RootDirectory / "nuget.config")
                .SetVerbosity(DotNetVerbosity.Minimal));

            // Step 5: Build canary - this is where MSB4099/TFM errors surface
            DotNetBuild(s => s
                .SetProjectFile(CanaryProject)
                .SetProperty("UseLocalSdk", "false")
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .SetVerbosity(DotNetVerbosity.Minimal));

            Serilog.Log.Information("✓ SDK Canary passed - package is valid");
        });

    Target QuickValidate => _ => _
        .Description("Fast validation: compile + canary only (no full test suite)")
        .DependsOn(LocalFeedCanary)
        .Executes(() =>
        {
            Serilog.Log.Information("✓ Quick validation complete");
        });
}
