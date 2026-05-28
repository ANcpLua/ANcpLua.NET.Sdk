using System.IO.Compression;
using System.Xml.Linq;
using Meziantou.Framework;
using NuGet.Packaging;
using NuGet.Packaging.Licenses;
using static ANcpLua.Sdk.Tests.Infrastructure.PackageFixture;

namespace ANcpLua.Sdk.Tests;

public sealed class Sdk100RootTests(PackageFixture fixture)
    : SdkTests(fixture, NetSdkVersion.Net100, SdkImportStyle.ProjectElement);

public sealed class Sdk100InnerTests(PackageFixture fixture)
    : SdkTests(fixture, NetSdkVersion.Net100, SdkImportStyle.SdkElement);

public sealed class Sdk100DirectoryBuildPropsTests(PackageFixture fixture)
    : SdkTests(fixture, NetSdkVersion.Net100, SdkImportStyle.SdkElementDirectoryBuildProps);

public abstract class SdkTests(
    PackageFixture fixture,
    NetSdkVersion dotnetSdkVersion,
    SdkImportStyle sdkImportStyle) : SdkTestBase(fixture)
{
    private static readonly string[] s_recordedProperties =
    [
        "LangVersion",
        "PublishRepositoryUrl",
        "DebugType",
        "EmbedAllSources",
        "EnableNETAnalyzers",
        "AnalysisLevel",
        "EnablePackageValidation",
        "RollForward",
        "GenerateSBOM",
        "_IsGitHubActions"
    ];

    private SdkProjectBuilder CreateProject(string? sdkName = null) =>
        CreateProject(sdkImportStyle, sdkName ?? SdkName, dotnetSdkVersion, s_recordedProperties);

    [Fact]
    public void Validate_WhenPackageReferencesInPropsAndTargets_HasVersionOrVersionOverride()
    {
        var root = RepositoryRoot.Locate()["src"];
        var files = Directory.GetFiles(root, "*", SearchOption.AllDirectories).Select(FullPath.FromPath);
        foreach (var file in files)
            if (file.Extension is ".props" or ".targets")
            {
                var nodes = XDocument.Load(file).Descendants("PackageReference")
                    .Where(static n => n.Attribute("Update") == null)
                    .Where(static n => n.Parent?.Name.LocalName != "ItemDefinitionGroup")
                    .Where(static n => !IsCpmItemGroup(n.Parent));
                foreach (var node in nodes)
                    if (node.Attribute("Version") is null && node.Attribute("VersionOverride") is null)
                        Assert.Fail("Missing Version or VersionOverride attribute on " + node);
            }

        static bool IsCpmItemGroup(XElement? parent)
        {
            if (parent?.Name.LocalName != "ItemGroup")
                return false;
            return parent.Attribute("Condition")?.Value
                .Contains("ManagePackageVersionsCentrally", StringComparison.Ordinal) == true;
        }
    }

    [Fact]
    public async Task Build_WhenProjectCreatedWithSdk_SetsDefaultProperties()
    {
        await using var project = CreateProject();

        var result = await project
            .WithOutputType(Val.Library)
            .AddSource("sample.cs", """
                namespace TestProject;

                /// <summary>Sample class for SDK validation.</summary>
                public class Sample { }
                """)
            .BuildAsync();

        result.ShouldHaveRecordedProperty("LangVersion", "latest");
        result.ShouldHaveRecordedProperty("PublishRepositoryUrl", "true");
        result.ShouldHaveRecordedProperty("DebugType", "portable");
        result.ShouldHaveRecordedProperty("EmbedAllSources", "true");
        result.ShouldHaveRecordedProperty("EnableNETAnalyzers", "true");
        result.ShouldHaveRecordedProperty("AnalysisLevel", "latest-all");
        result.ShouldHaveRecordedProperty("EnablePackageValidation", "true");
        result.ShouldHaveRecordedProperty("RollForward", "LatestMajor");
    }

    [Fact]
    public async Task Build_WhenProjectIsTestSdk_DoesNotSetRollForward()
    {
        await using var project = CreateProject(SdkTestName);

        var result = await project.BuildAsync();

        result.ShouldHaveRecordedProperty("RollForward", null);
    }

    [Fact]
    public async Task Build_WhenLangVersionIsPreview_UsesPreviewVersion()
    {
        await using var project = CreateProject();

        var result = await project
            .WithLangVersion("preview")
            .AddSource("sample.cs", "Console.WriteLine();")
            .BuildAsync();

        result.ShouldHaveRecordedProperty("LangVersion", "preview");
    }

    [Fact]
    public async Task Pack_WhenContinuousIntegrationBuildIsTrue_GeneratesSbom()
    {
        await using var project = CreateProject();

        var result = await project
            .WithProperty("ContinuousIntegrationBuild", "true")
            .AddSource("Program.cs", "Console.WriteLine();")
            .PackAsync();

        result.ShouldHaveRecordedProperty("GenerateSBOM", "true");

        var nupkg = NuGetPackage.FindSingle(project.RootFolder);
        await using var archive = await ZipFile.OpenReadAsync(nupkg, TestContext.Current.CancellationToken);
        Assert.Contains(archive.Entries,
            static e => e.FullName.EndsWith("manifest.spdx.json", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Pack_WhenContinuousIntegrationBuildIsNotSet_DoesNotGenerateSbom()
    {
        await using var project = CreateProject();

        var result = await project
            .AddSource("Program.cs", "Console.WriteLine();")
            .PackAsync();

        Assert.False(
            result.OutputContains("SbomFilePath=") || result.OutputContains("manifest.spdx.json"),
            result.Output);
    }

    [Fact]
    public async Task Build_WhenRollForwardIsMinor_UsesMinorVersion()
    {
        await using var project = CreateProject();

        var result = await project
            .WithProperty("RollForward", "Minor")
            .AddSource("sample.cs", "Console.WriteLine();")
            .BuildAsync();

        result.ShouldHaveRecordedProperty("RollForward", "Minor");
    }

    [Fact]
    public async Task Build_WhenOutputTypeIsLibrary_SetsRollForwardLatestMajor()
    {
        await using var project = CreateProject();

        var result = await project
            .WithOutputType(Val.Library)
            .BuildAsync();

        result.ShouldHaveRecordedProperty("RollForward", "LatestMajor");
    }

    [Fact]
    public async Task Build_WhenCodeContainsUnsafeBlock_Succeeds()
    {
        await using var project = CreateProject();

        var result = await project
            .AddSource("sample.cs", """
                unsafe
                {
                    int* p = null;
                }
                """)
            .BuildAsync();

        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task Build_WhenCodeUsesStaticTypeWithIs_ReportsCS7023()
    {
        await using var project = CreateProject();

        var result = await project
            .AddSource("sample.cs", """
                var o = new object();
                if (o is Math) // Error CS7023 The second operand of an 'is' or 'as' operator may not be static type 'Math'
                {
                }
                """)
            .BuildAsync();

        Assert.True(result.HasWarning("CS7023"));
    }

    [Fact]
    public async Task Build_WhenCodeUsesDateTime_Now_ReportsRS0030()
    {
        await using var project = CreateProject();

        var result = await project
            .AddSource("sample.cs", "_ = System.DateTime.Now;")
            .BuildAsync();

        Assert.True(result.HasWarning("RS0030"));

        var files = result.GetBinLogFiles();
        Assert.Contains(files, static f => f.EndsWith("BannedSymbols.txt", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Build_WhenCodeUsesNewtonsoftJson_ReportsRS0030()
    {
        await using var project = CreateProject();

        var result = await project
            .WithPackage("Newtonsoft.Json", "13.0.4")
            .AddSource("sample.cs", """_ = Newtonsoft.Json.JsonConvert.SerializeObject("test");""")
            .BuildAsync();

        Assert.True(result.HasWarning("RS0030"));
    }

    [Fact]
    public async Task Build_WhenBannedNewtonsoftJsonSymbolsDisabled_DoesNotReportRS0030()
    {
        await using var project = CreateProject();

        var result = await project
            .WithProperty("BannedNewtonsoftJsonSymbols", "false")
            .WithPackage("Newtonsoft.Json", "13.0.4")
            .AddSource("sample.cs", """_ = Newtonsoft.Json.JsonConvert.SerializeObject("test");""")
            .BuildAsync();

        Assert.False(result.HasWarning("RS0030"));
    }

    [Fact]
    public async Task Build_WhenEditorConfigFileExists_IncludedInBinlog()
    {
        await using var project = CreateProject();

        var localFile = project.AddFile(".editorconfig", "");

        var result = await project
            .AddSource("sample.cs", "_ = System.DateTime.Now;")
            .BuildAsync();

        var files = result.GetBinLogFiles();
        Assert.Contains(files, static f => f.EndsWith(".editorconfig", StringComparison.Ordinal));
        Assert.Contains(files, f => f == localFile || f == "/private" + localFile);
    }

    [Fact]
    public async Task Build_WhenRunOnGitHubActionsWithBannedSymbols_TreatsWarningAsError()
    {
        await using var project = CreateProject();

        var result = await project
            .AddSource("sample.cs", "_ = System.DateTime.Now;")
            .BuildAsync(environmentVariables: [.. project.GitHubEnvironmentVariables]);

        Assert.True(result.HasError("RS0030"));
    }

    [Fact]
    public async Task Build_WhenTreatWarningsAsErrorsIsFalse_ReportsIDEWarning()
    {
        await using var project = CreateProject();

        var result = await project
            .WithProperty("TreatWarningsAsErrors", "false")
            .AddSource("sample.cs", """
                _ = "";

                class Sample
                {
                    private readonly int field;

                    public Sample(int a) => field = a;

                    public int A() => field;
                }
                """)
            .BuildAsync(["--configuration", "Release"]);

        Assert.True(result.HasWarning("IDE1006"));
    }

    [Fact]
    public async Task Build_WhenNamingConventionIsInvalid_ReportsIDE1006()
    {
        await using var project = CreateProject();

        var result = await project
            .AddSource("sample.cs", """
                _ = "";

                class Sample
                {
                    private readonly int field;

                    public Sample(int a) => field = a;

                    public int A() => field;
                }
                """)
            .BuildAsync(["--configuration", "Release"]);

        Assert.True(result.HasError("IDE1006"));
    }

    [Fact]
    public async Task Build_WhenNamingConventionIsValid_DoesNotReportIDE1006()
    {
        await using var project = CreateProject();

        var result = await project
            .AddSource("sample.cs", """
                _ = "";

                class Sample
                {
                    private int _field;
                }
                """)
            .BuildAsync(["--configuration", "Release"]);

        Assert.False(result.HasError("IDE1006"));
        Assert.False(result.HasWarning("IDE1006"));
    }

    [Fact]
    public async Task Build_WhenCodeIsValidExpression_ProducesNoWarnings()
    {
        await using var project = CreateProject();

        var result = await project
            .AddSource("Program.cs", """
                A();

                static void A()
                {
                    System.Console.WriteLine();
                }
                """)
            .BuildAsync(["--configuration", "Release"]);

        Assert.False(result.HasWarning());
        Assert.False(result.HasError());
    }

    [Fact]
    public async Task Build_WhenExpressionValueIsNeverUsed_DoesNotReportWarning()
    {
        await using var project = CreateProject();

        var result = await project
            .AddSource("Program.cs", """
                var sb = new System.Text.StringBuilder();
                sb.AppendLine();

                """)
            .BuildAsync(["--configuration", "Release"]);

        Assert.False(result.HasWarning());
        Assert.False(result.HasError());
    }

    [Fact]
    public async Task Build_WhenRunInDebugWithLocalEditorConfig_DoesNotEnforceCodeStyle()
    {
        await using var project = CreateProject();

        project.AddFile(".editorconfig", """
            root = true

            [*.cs]
            csharp_style_expression_bodied_local_functions = true
            dotnet_diagnostic.IDE0061.severity = warning
            """);

        var result = await project
            .AddSource("Program.cs", """
                _ = "";

                sealed class Sample
                {
                    public static void A()
                    {
                        B();

                        static void B()
                        {
                            System.Console.WriteLine();
                        }
                    }
                }

                """)
            .BuildAsync(["--configuration", "Debug"]);

        result.HasWarning("IDE0061").Should().BeFalse();
        result.HasError().Should().BeFalse();
    }

    [Fact]
    public async Task Build_WhenRunInReleaseWithLocalEditorConfig_OverridesGlobalConfig()
    {
        await using var project = CreateProject();

        project.AddFile(".editorconfig", """
            root = true

            [*.cs]
            csharp_style_expression_bodied_local_functions = true
            dotnet_diagnostic.IDE0061.severity = error
            """);

        var result = await project
            .AddSource("Program.cs", """
                sealed class Sample
                {
                    public static void A()
                    {
                        B();

                        static void B()
                        {
                            System.Console.WriteLine();
                        }
                    }
                }
                """)
            .BuildAsync(["--configuration", "Release"]);

        result.HasError("IDE0061").Should().BeTrue();
    }

    [Fact]
    public async Task Build_WhenRunOnGitHubActionsWithVulnerablePackage_ReportsNU1903AsError()
    {
        await using var project = CreateProject();

        var result = await project
            .WithPackage("System.Net.Http", "4.3.3")
            .AddSource("Program.cs", "System.Console.WriteLine();")
            .BuildAsync(environmentVariables: [.. project.GitHubEnvironmentVariables]);

        Assert.True(result.OutputContains("error NU1903"));
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Build_WhenVulnerablePackageIsPresent_ReportsNU1903AsWarning()
    {
        await using var project = CreateProject();

        var result = await project
            .WithPackage("System.Net.Http", "4.3.3")
            .AddSource("Program.cs", "System.Console.WriteLine();")
            .BuildAsync();

        Assert.True(result.OutputContains("warning NU1903"));
        Assert.True(result.OutputDoesNotContain("error NU1903"));
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task Build_WhenRunInReleaseWithCustomWarning_TreatsAsError()
    {
        await using var project = CreateProject();

        var customTarget = new XElement("Target", new XAttribute("Name", "Custom"), new XAttribute("BeforeTargets", "Build"),
            new XElement("Warning", new XAttribute("Text", "CustomWarning")));

        var result = await project
            .WithAdditionalProjectElement(customTarget)
            .AddSource("Program.cs", "System.Console.WriteLine();")
            .BuildAsync(["--configuration", "Release"]);

        Assert.True(result.OutputContains("error : CustomWarning"));
    }

    [Fact]
    public async Task Build_WhenRunInDebugWithCustomWarning_ReportsAsWarning()
    {
        await using var project = CreateProject();

        var customTarget = new XElement("Target", new XAttribute("Name", "Custom"), new XAttribute("BeforeTargets", "Build"),
            new XElement("Warning", new XAttribute("Text", "CustomWarning")));

        var result = await project
            .WithAdditionalProjectElement(customTarget)
            .AddSource("Program.cs", "System.Console.WriteLine();")
            .BuildAsync(["--configuration", "Debug"]);

        Assert.True(result.OutputContains("warning : CustomWarning"));
    }

    [Fact]
    public async Task Build_WhenTypeIsFileLocal_DoesNotReportCA1708()
    {
        await using var project = CreateProject();

        project.AddSource("Sample1.cs", """
            System.Console.WriteLine();

            class A {}

            file class Sample
            {
            }
            """);

        var result = await project
            .AddSource("Sample2.cs", """
                class B {}

                file class sample
                {
                }
                """)
            .BuildAsync(["--configuration", "Release"]);

        Assert.False(result.HasError("CA1708"));
        Assert.False(result.HasWarning("CA1708"));
    }

    [Fact]
    public async Task Build_WhenRunDotnetBuildInRelease_EmbedsPdbInfo()
    {
        await using var project = CreateProject();

        var result = await project
            .AddSource("Program.cs", "Console.WriteLine();")
            .BuildAsync(["--configuration", "Release"]);

        result.ShouldSucceed();

        var outputFiles = Directory.GetFiles(project.RootFolder / "bin", "*", SearchOption.AllDirectories);
        AssertDebugInfoExists(outputFiles);
    }

    [Fact]
    public async Task Pack_WhenPackingClassLibraryInRelease_ProducesNupkgWithEmbeddedPdb()
    {
        await using var project = CreateProject();

        await project
            .WithOutputType(Val.Library)
            .PackAsync(["--configuration", "Release"]);

        var nupkg = NuGetPackage.FindSingle(project.RootFolder / "bin" / "Release");
        var outputFiles = await NuGetPackage.ExtractAsync(nupkg, project.RootFolder / "extracted");

        AssertDebugInfoExists(outputFiles, isPackOutput: true);
        Assert.Contains(outputFiles, static f => f.EndsWith(".xml", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Pack_WhenPackingInRelease_EmbedsPdbAndIncludesXmlDoc()
    {
        await using var project = CreateProject();

        var result = await project
            .WithOutputType(Val.Library)
            .AddSource("Class1.cs", """
                namespace TestProject;

                /// <summary>Test class.</summary>
                public class Class1 { }
                """)
            .PackAsync(["--configuration", "Release"]);

        Assert.True(result.ExitCode == 0, $"Pack failed with exit code {result.ExitCode}: {result.Output}");

        var nupkg = NuGetPackage.FindSingle(project.RootFolder / "bin" / "Release");
        var outputFiles = await NuGetPackage.ExtractAsync(nupkg, project.RootFolder / "extracted");

        AssertDebugInfoExists(outputFiles, isPackOutput: true);
        Assert.Contains(outputFiles, static f => f.EndsWith(".xml", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Pack_WhenPackingClassLibrary_IncludesXmlDocumentation()
    {
        await using var project = CreateProject();

        var result = await project
            .WithOutputType(Val.Library)
            .AddSource("Class1.cs", """
                namespace TestProject;

                /// <summary>Test class.</summary>
                public class Class1 { }
                """)
            .PackAsync();

        Assert.True(result.ExitCode == 0, $"Pack failed with exit code {result.ExitCode}: {result.Output}");

        var nupkg = NuGetPackage.FindSingle(project.RootFolder / "bin" / "Release");
        var outputFiles = await NuGetPackage.ExtractAsync(nupkg, project.RootFolder / "extracted");

        Assert.Contains(outputFiles, static f => f.EndsWith(".xml", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("readme.md")]
    [InlineData("Readme.md")]
    [InlineData("ReadMe.md")]
    [InlineData("README.md")]
    public async Task Pack_WhenReadmeFileExistsInRoot_IncludesReadmeInPackage(string readmeFileName)
    {
        await using var project = CreateProject();

        project.AddFile(readmeFileName, "sample");

        var result = await project
            .WithOutputType(Val.Library)
            .AddSource("Class1.cs", """
                namespace TestProject;

                /// <summary>Test class.</summary>
                public class Class1 { }
                """)
            .PackAsync(["--configuration", "Release"]);

        Assert.True(result.ExitCode == 0, $"Pack failed with exit code {result.ExitCode}: {result.Output}");

        var extractedPath = project.RootFolder / "extracted";
        var nupkg = NuGetPackage.FindSingle(project.RootFolder / "bin" / "Release");
        await NuGetPackage.ExtractAsync(nupkg, extractedPath);

        var allFiles = Directory.GetFiles(extractedPath);
        Assert.Contains("README.md", allFiles.Select(Path.GetFileName));
        Assert.Equal("sample",
            await File.ReadAllTextAsync(extractedPath / "README.md", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Pack_WhenProjectInSubdirectoryAndReadmeAbove_DoesNotIncludeReadme()
    {
        await using var project = CreateProject();

        project.AddFile("README.md", "sample");

        var result = await project
            .WithFilename("dir/Test.csproj")
            .WithOutputType(Val.Library)
            .AddSource("dir/Class1.cs", """
                namespace TestProject;

                /// <summary>Test class.</summary>
                public class Class1 { }
                """)
            .PackAsync(["dir", "--configuration", "Release"]);

        Assert.True(result.ExitCode == 0, $"Pack failed with exit code {result.ExitCode}: {result.Output}");

        var extractedPath = project.RootFolder / "extracted";
        var nupkg = NuGetPackage.FindSingle(project.RootFolder / "dir" / "bin" / "Release");
        await NuGetPackage.ExtractAsync(nupkg, extractedPath);

        Assert.False(File.Exists(extractedPath / "README.md"));
    }

    [Fact]
    public async Task Pack_WhenProjectNameIsNotANcpLua_DoesNotApplyPackageProperties()
    {
        await using var project = CreateProject();

        project.AddFile("LICENSE.txt", "dummy");

        var result = await project
            .WithFilename("sample.csproj")
            .AddSource("Program.cs", "Console.WriteLine();")
            .PackAsync();

        Assert.Equal(0, result.ExitCode);

        using var packageReader = new PackageArchiveReader(NuGetPackage.FindSingle(project.RootFolder));
        var nuspecReader = await packageReader.GetNuspecReaderAsync(TestContext.Current.CancellationToken);
        Assert.NotEqual(SdkBrandingConstants.Author, nuspecReader.GetAuthors());
        Assert.NotEqual("icon.png", nuspecReader.GetIcon());
        Assert.DoesNotContain("icon.png", packageReader.GetFiles());
    }

    [Fact]
    public async Task Pack_WhenProjectNameIsANcpLua_AppliesPackageProperties()
    {
        await using var project = CreateProject();

        project.AddFile("LICENSE.txt", "dummy");

        var result = await project
            .AddSource("Program.cs", "Console.WriteLine();")
            .PackAsync();

        Assert.Equal(0, result.ExitCode);

        using var packageReader = new PackageArchiveReader(NuGetPackage.FindSingle(project.RootFolder));
        var nuspecReader = await packageReader.GetNuspecReaderAsync(TestContext.Current.CancellationToken);
        Assert.Equal(SdkBrandingConstants.Author, nuspecReader.GetAuthors());
        Assert.Null(nuspecReader.GetIcon());
        Assert.DoesNotContain("icon.png", packageReader.GetFiles());
        Assert.Contains("LICENSE.txt", packageReader.GetFiles());
    }

    [Fact]
    public async Task Build_WhenProjectNameIsANcpLuaAnalyzer_Succeeds()
    {
        await using var project = CreateProject();

        var result = await project
            .WithFilename("ANcpLua.Analyzer.csproj")
            .WithOutputType(Val.Library)
            .WithTargetFramework(Tfm.NetStandard20)
            .AddSource("Sample.cs", """
                namespace Sample;

                /// <summary>Sample placeholder for analyzer-named project.</summary>
                public class Sample { }
                """)
            .BuildAsync();

        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task Build_WhenMtpTestProjectRunOnGitHubActions_AddsGitHubActionsTestLogger()
    {
        await using var project = CreateProject(SdkTestName);

        var result = await project
            .WithFilename("Sample.Tests.csproj")
            .AddSource("Program.cs", """
                namespace Sample.Tests;

                /// <summary>Sample test class.</summary>
                public class Tests
                {
                    /// <summary>Test method.</summary>
                    [Fact]
                    public void Test1() => true.Should().BeTrue();
                }
                """)
            .BuildAsync(environmentVariables: [.. project.GitHubEnvironmentVariables]);

        Assert.Equal(0, result.ExitCode);

        var isGitHubActions = result.GetRecordedProperty("_IsGitHubActions");
        Assert.True(isGitHubActions == "true",
            $"Expected _IsGitHubActions='true', but got '{isGitHubActions}'.");

        Assert.True(result.IsMsBuildTargetExecuted("_InjectGitHubActionsLogger"),
            "_InjectGitHubActionsLogger target did not execute.");

        var items = result.GetMsBuildItems("PackageReference");
        Assert.Contains(items, static i => i.Contains("GitHubActionsTestLogger", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Build_WhenMtpTestProjectRunOffCi_DoesNotAddGitHubActionsTestLogger()
    {
        await using var project = CreateProject(SdkTestName);

        var result = await project
            .WithFilename("Sample.Tests.csproj")
            .AddSource("Program.cs", """
                namespace Sample.Tests;

                /// <summary>Sample test class.</summary>
                public class Tests
                {
                    [Fact]
                    public void Test1() => true.Should().BeTrue();
                }
                """)
            .BuildAsync();

        Assert.Equal(0, result.ExitCode);

        Assert.DoesNotContain(result.GetMsBuildItems("PackageReference"),
            static i => i.Contains("GitHubActionsTestLogger", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Build_WhenCentralPackageManagementEnabled_Succeeds()
    {
        await using var project = CreateProject();

        project.AddFile("Directory.Packages.props", """
            <Project>
              <PropertyGroup>
                <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
                <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
              </PropertyGroup>
              <ItemGroup>
              </ItemGroup>
            </Project>
            """);

        var result = await project
            .WithSdkName(SdkTestName)
            .WithFilename("Sample.Tests.csproj")
            .AddSource("Program.cs", "Console.WriteLine();")
            .BuildAsync();

        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task Build_WhenVulnerablePackageAndNoSuppression_Fails()
    {
        await using var project = CreateProject();

        var result = await project
            .WithPackage("System.Net.Http", "4.3.3")
            .WithProperty("NOWARN", "$(NOWARN);NU1510")
            .AddSource("Program.cs", "Console.WriteLine();")
            .BuildAsync(["--configuration", "Release"]);

        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Build_WhenNuGetAuditSuppressed_Succeeds()
    {
        await using var project = CreateProject();

        var suppressItem = new XElement("ItemGroup",
            new XElement("NuGetAuditSuppress",
                new XAttribute("Include", "https://github.com/advisories/GHSA-7jgj-8wvc-jh57")));

        var result = await project
            .WithPackage("System.Net.Http", "4.3.3")
            .WithProperty("NOWARN", "$(NOWARN);NU1510")
            .WithAdditionalProjectElement(suppressItem)
            .AddSource("Program.cs", "Console.WriteLine();")
            .BuildAsync(["--configuration", "Release"]);

        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task Pack_WhenGitRepositoryInitializedAndCiBuild_IncludesRepositoryMetadata()
    {
        await using var project = CreateProject();

        await project.InitializeGitRepositoryAsync();

        var result = await project
            .WithFilename("ANcpLua.Sample.csproj")
            .WithOutputType(Val.Library)
            .AddSource("Class1.cs", """
                namespace ANcpLua.Sample;

                /// <summary>Sample class.</summary>
                public static class Class1
                {
                }
                """)
            .PackAsync(environmentVariables: [.. project.GitHubEnvironmentVariables]);

        Assert.True(result.ExitCode is 0, result.Output);

        using var packageReader = new PackageArchiveReader(NuGetPackage.FindSingle(project.RootFolder));
        var nuspecReader = await packageReader.GetNuspecReaderAsync(TestContext.Current.CancellationToken);
        Assert.Equal(SdkBrandingConstants.Author, nuspecReader.GetAuthors());
        Assert.Null(nuspecReader.GetIcon());
        var license = nuspecReader.GetLicenseMetadata()!;
        Assert.Equal(LicenseType.Expression, license.Type);
        Assert.Equal(LicenseExpressionType.License, license.LicenseExpression!.Type);
        Assert.Equal("MIT", ((NuGetLicense)license.LicenseExpression!).Identifier);
        Assert.Equal("git", nuspecReader.GetRepositoryMetadata().Type);
        Assert.Equal("https://github.com/ancplua/sample.git", nuspecReader.GetRepositoryMetadata().Url);
        Assert.Equal("refs/heads/main", nuspecReader.GetRepositoryMetadata().Branch);
        Assert.NotEmpty(nuspecReader.GetRepositoryMetadata().Commit);
    }

    [Theory]
    [InlineData(SdkName)]
    [InlineData(SdkTestName)]
    [InlineData(SdkWebName)]
    public async Task Build_WhenSdkNameDefined_StampsAssemblyWithSdkMetadata(string sdkName)
    {
        await using var project = CreateProject(sdkName);

        project.AddDirectoryBuildPropsFile("", sdkName: sdkName);

        var isTestSdk = sdkName == SdkTestName;
        var outputType = isTestSdk ? Val.Exe : Val.Library;
        var source = isTestSdk ? "Console.WriteLine();" : "namespace Sample.Tests; public class Foo { }";

        var result = await project
            .WithFilename("Sample.Tests.csproj")
            .WithOutputType(outputType)
            .AddSource("Program.cs", source)
            .BuildAsync();

        Assert.Equal(0, result.ExitCode);

        var dllPath = Directory
            .GetFiles(project.RootFolder / "bin" / "Debug", "Sample.Tests.dll", SearchOption.AllDirectories).Single();
        var metadata = PeImage.ReadAssemblyMetadata(dllPath);

        Assert.Equal(sdkName, metadata.GetValueOrDefault(SdkBrandingConstants.SdkMetadataKey));
    }

    [Fact]
    public async Task Build_WhenMultipleTargetFrameworksSet_BuildsBothOutputs()
    {
        await using var project = CreateProject()
            .WithFilename("Sample.csproj")
            .WithOutputType(Val.Library)
            .WithProperty("TargetFramework", "")
            .WithProperty("TargetFrameworks", "net10.0;netstandard2.0")
            .AddSource("Sample.cs", "namespace Sample; public class Foo { }");

        var result = await project.BuildAsync();

        Assert.True(result.ExitCode is 0, result.Output);
        Assert.Single(Directory.GetFiles(project.RootFolder / "bin" / "Debug" / "net10.0", "Sample.dll"));
        Assert.Single(Directory.GetFiles(project.RootFolder / "bin" / "Debug" / "netstandard2.0", "Sample.dll"));
    }

    [Theory]
    [InlineData("TargetFramework", "")]
    [InlineData("TargetFrameworks", "")]
    public async Task Build_WhenTargetFrameworkPropertySet_ProducesCorrectOutput(string propName, string version)
    {
        await using var project = CreateProject();

        var result = await project
            .WithFilename("Sample.csproj")
            .WithOutputType(Val.Library)
            .WithProperty(propName, version)
            .AddSource("Sample.cs", "namespace Sample; public class Foo { }")
            .BuildAsync();

        Assert.True(result.ExitCode is 0, result.Output);

        var dllPath = Directory
            .GetFiles(project.RootFolder / "bin" / "Debug", "Sample.dll", SearchOption.AllDirectories).Single();

        var expectedVersion = string.IsNullOrEmpty(version)
            ? dotnetSdkVersion switch
            {
                NetSdkVersion.Net100 => "net10.0",
                _ => throw new NotSupportedException()
            }
            : version;

        var frameworkName = PeImage.ReadTargetFrameworkName(dllPath);
        Assert.NotNull(frameworkName);
        Assert.Contains(expectedVersion.Replace("net", "v", StringComparison.Ordinal), frameworkName, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Build_WhenNpmPackageFileExists_InstallsNpmDependencies()
    {
        await using var project = CreateProject(SdkWebName)
            .WithProperty("EnableDefaultNpmPackageFile", "true");

        project.AddFile("package.json", NpmPackageJson);

        var result = await project
            .AddSource("Program.cs", "Console.WriteLine();")
            .BuildAsync();

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(project.RootFolder / "package-lock.json"));
        Assert.True(File.Exists(project.RootFolder / "node_modules" / ".npm-install-stamp"));
        var files = result.GetBinLogFiles();
        Assert.Contains(files, static f => f.EndsWith("package-lock.json", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Restore_WhenNpmPackageFileExists_InstallsNpmDependencies()
    {
        await using var project = CreateProject(SdkWebName)
            .WithProperty("EnableDefaultNpmPackageFile", "true");

        project.AddFile("package.json", NpmPackageJson);

        var result = await project
            .AddSource("Program.cs", "Console.WriteLine();")
            .RestoreAsync();

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(project.RootFolder / "package-lock.json"));
        Assert.True(File.Exists(project.RootFolder / "node_modules" / ".npm-install-stamp"));
    }

    [Fact]
    public async Task Restore_WhenNpmAutoDetectionDisabled_DoesNotInstallNpm()
    {
        await using var project = CreateProject(SdkWebName);

        project.AddFile("package.json", NpmPackageJson);

        var result = await project
            .AddSource("Program.cs", "Console.WriteLine();")
            .RestoreAsync();

        Assert.Equal(0, result.ExitCode);
        Assert.False(File.Exists(project.RootFolder / "package-lock.json"));
        Assert.False(File.Exists(project.RootFolder / "node_modules" / ".npm-install-stamp"));
    }

    [Fact]
    public async Task Build_WhenBuildingSolutionWithNpm_InstallsNpmAndBuilds()
    {
        await using var project = CreateProject(SdkWebName)
            .WithProperty("EnableDefaultNpmPackageFile", "true");

        var slnFile = project.AddFile("sample.slnx", """
            <Solution>
                <Project Path="sample.csproj" />
            </Solution>
            """);
        project.AddFile("package.json", NpmPackageJson);

        var result = await project
            .WithFilename("sample.csproj")
            .AddSource("Program.cs", "Console.WriteLine();")
            .BuildAsync([slnFile]);

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(project.RootFolder / "package-lock.json"));
        Assert.True(File.Exists(project.RootFolder / "node_modules" / ".npm-install-stamp"));
    }

    [Theory]
    [InlineData("build")]
    [InlineData("publish")]
    public async Task Build_WhenBuildingOrPublishingSolutionWithNpm_Succeeds(string command)
    {
        await using var project = CreateProject(SdkWebName)
            .WithProperty("EnableDefaultNpmPackageFile", "true");

        var slnFile = project.AddFile("sample.slnx", """
            <Solution>
                <Project Path="sample.csproj" />
            </Solution>
            """);
        project.AddFile("package.json", NpmPackageJson);
        project.WithFilename("sample.csproj").WithOutputType(Val.Exe).AddSource("Program.cs", "Console.WriteLine();");

        BuildResult result;
        if (command == "build")
        {
            result = await project.BuildAsync([slnFile]);
        }
        else
        {
            await project.BuildAsync([slnFile]);
            result = await project.ExecuteDotnetCommandAsync("publish", [slnFile]);
        }

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(project.RootFolder / "package-lock.json"));
        Assert.True(File.Exists(project.RootFolder / "node_modules" / ".npm-install-stamp"));
    }

    [Fact]
    public async Task Restore_WhenMultipleNpmPackageFilesExist_InstallsEachDirectory()
    {
        await using var project = CreateProject(SdkWebName);

        var npmItems = new XElement("ItemGroup",
            new XElement("NpmPackageFile", new XAttribute("Include", "a/package.json")),
            new XElement("NpmPackageFile", new XAttribute("Include", "b/package.json")));

        project.AddFile("a/package.json", NpmPackageJson);
        project.AddFile("b/package.json", NpmPackageJson);

        var result = await project
            .WithAdditionalProjectElement(npmItems)
            .AddSource("Program.cs", "Console.WriteLine();")
            .RestoreAsync();

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(project.RootFolder / "a" / "package-lock.json"));
        Assert.True(File.Exists(project.RootFolder / "a" / "node_modules" / ".npm-install-stamp"));
        Assert.True(File.Exists(project.RootFolder / "b" / "package-lock.json"));
        Assert.True(File.Exists(project.RootFolder / "b" / "node_modules" / ".npm-install-stamp"));
    }

    [Fact]
    public async Task Build_WhenNpmBuildWithRestoreLockedModeAndNoLockFile_Fails()
    {
        await using var project = CreateProject(SdkWebName)
            .WithProperty("EnableDefaultNpmPackageFile", "true");

        project.AddFile("package.json", NpmPackageJson);

        var result = await project
            .AddSource("Program.cs", "Console.WriteLine();")
            .BuildAsync(["/p:RestoreLockedMode=true"]);

        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [InlineData("/p:RestoreLockedMode=true")]
    [InlineData("/p:ContinuousIntegrationBuild=true")]
    public async Task Build_WhenNpmBuildWithCiCondition_Succeeds(string command)
    {
        await using var project = CreateProject(SdkWebName)
            .WithProperty("EnableDefaultNpmPackageFile", "true");

        project.AddFile("package.json", NpmPackageJson);
        project.AddFile("package-lock.json", """
            {
              "name": "sample",
              "version": "1.0.0",
              "lockfileVersion": 3,
              "requires": true,
              "packages": {
                "": {
                  "name": "sample",
                  "version": "1.0.0",
                  "devDependencies": {
                    "is-number": "7.0.0"
                  }
                },
                "node_modules/is-number": {
                  "version": "7.0.0",
                  "resolved": "https://registry.npmjs.org/is-number/-/is-number-7.0.0.tgz",
                  "integrity": "sha512-41Cifkg6e8TylSpdtTpeLVMqvSBEVzTttHvERD741+pnZ8ANv0004MRL43QKPDlK9cGvNp6NZWZUBlbGXYxxng==",
                  "dev": true,
                  "license": "MIT",
                  "engines": {
                    "node": ">=0.12.0"
                  }
                }
              }
            }

            """);

        var result = await project
            .AddSource("Program.cs", "Console.WriteLine();")
            .BuildAsync([command]);

        Assert.Equal(0, result.ExitCode);
    }

    private const string NpmPackageJson = """
        {
          "name": "sample",
          "version": "1.0.0",
          "private": true,
          "devDependencies": {
            "is-number": "7.0.0"
          }
        }
        """;

    private static void AssertDebugInfoExists(IReadOnlyCollection<string> outputFiles, bool isPackOutput = false)
    {
        var dllPath = outputFiles.Single(static f => f.EndsWith(".dll", StringComparison.OrdinalIgnoreCase));
        Assert.True(PeImage.HasCodeViewDebugEntry(dllPath));

        if (!isPackOutput)
            Assert.Contains(outputFiles, static f => f.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase));
    }
}
