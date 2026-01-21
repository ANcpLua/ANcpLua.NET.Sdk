using System.Diagnostics;
using System.IO.Compression;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Xml.Linq;
using Meziantou.Framework;
using NuGet.Packaging;
using NuGet.Packaging.Licenses;
using static ANcpLua.Sdk.Tests.Helpers.PackageFixture;

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
    SdkImportStyle sdkImportStyle)
{
    private readonly NetSdkVersion _dotnetSdkVersion = dotnetSdkVersion;
    private readonly PackageFixture _fixture = fixture;
    private readonly SdkImportStyle _sdkImportStyle = sdkImportStyle;

    private SdkProjectBuilder CreateProject(string? sdkName = null) =>
        SdkProjectBuilder.Create(_fixture, _sdkImportStyle, sdkName ?? SdkName)
            .WithDotnetSdkVersion(_dotnetSdkVersion);

    [Fact]
    public void PackageReferenceAreValid()
    {
        var root = RepositoryRoot.Locate()["src"];
        var files = Directory.GetFiles(root, "*", SearchOption.AllDirectories).Select(FullPath.FromPath);
        foreach (var file in files)
            if (file.Extension is ".props" or ".targets")
            {
                var doc = XDocument.Load(file);

                // Exclude: Update elements (metadata-only), ItemDefinitionGroup, CPM ItemGroups
                var nodes = doc.Descendants("PackageReference")
                    .Where(static n => n.Attribute("Update") == null)
                    .Where(static n => n.Parent?.Name.LocalName != "ItemDefinitionGroup")
                    .Where(static n => !IsCpmItemGroup(n.Parent));
                foreach (var node in nodes)
                {
                    var versionAttr = node.Attribute("Version");
                    if (versionAttr is null)
                        Assert.Fail("Missing Version attribute on " + node);
                }
            }

        static bool IsCpmItemGroup(XElement? parent)
        {
            if (parent?.Name.LocalName != "ItemGroup")
                return false;
            var condition = parent.Attribute("Condition")?.Value;
            return condition?.Contains("ManagePackageVersionsCentrally") == true;
        }
    }

    [Fact]
    public async Task ValidateDefaultProperties()
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

        result.ShouldHavePropertyValue("LangVersion", "latest");
        result.ShouldHavePropertyValue("PublishRepositoryUrl", "true");
        result.ShouldHavePropertyValue("DebugType", "portable");
        result.ShouldHavePropertyValue("EmbedAllSources", "true");
        result.ShouldHavePropertyValue("EnableNETAnalyzers", "true");
        result.ShouldHavePropertyValue("AnalysisLevel", "latest-all");
        result.ShouldHavePropertyValue("EnablePackageValidation", "true");
        result.ShouldHavePropertyValue("RollForward", "LatestMajor");
    }

    [Fact]
    public async Task ValidateDefaultProperties_Test()
    {
        await using var project = CreateProject(SdkTestName);

        var result = await project.BuildAsync();

        result.ShouldHavePropertyValue("RollForward", null);
    }

    [Fact]
    public async Task CanOverrideLangVersion()
    {
        await using var project = CreateProject();

        var result = await project
            .WithLangVersion("preview")
            .AddSource("sample.cs", "Console.WriteLine();")
            .BuildAsync();

        result.ShouldHavePropertyValue("LangVersion", "preview");
    }

    [Fact]
    public async Task GenerateSbom_IsSetWhenContinuousIntegrationBuildIsSet()
    {
        await using var project = CreateProject();

        var result = await project
            .WithProperty("ContinuousIntegrationBuild", "true")
            .AddSource("Program.cs", "Console.WriteLine();")
            .PackAsync();

        result.ShouldHavePropertyValue("GenerateSBOM", "true");

        var nupkgFile = Directory.GetFiles(project.RootFolder, "*.nupkg", SearchOption.AllDirectories).Single();
        await using var archive = await ZipFile.OpenReadAsync(nupkgFile, TestContext.Current.CancellationToken);
        Assert.Contains(archive.Entries,
            static e => e.FullName.EndsWith("manifest.spdx.json", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GenerateSbom_IsNotSetWhenContinuousIntegrationBuildIsNotSet()
    {
        await using var project = CreateProject();

        var result = await project
            .AddSource("Program.cs", "Console.WriteLine();")
            .PackAsync();

        Assert.False(
            result.OutputContains("SbomFilePath=") ||
            result.OutputContains("manifest.spdx.json"),
            result.Output.ToString());
    }

    [Fact]
    public async Task CanOverrideRollForward()
    {
        await using var project = CreateProject();

        var result = await project
            .WithProperty("RollForward", "Minor")
            .AddSource("sample.cs", "Console.WriteLine();")
            .BuildAsync();

        result.ShouldHavePropertyValue("RollForward", "Minor");
    }

    [Fact]
    public async Task RollForwardIsCompatibleWithClassLibraries()
    {
        await using var project = CreateProject();

        var result = await project
            .WithOutputType(Val.Library)
            .BuildAsync();

        result.ShouldHavePropertyValue("RollForward", "LatestMajor");
    }

    [Fact]
    public async Task AllowUnsafeBlock()
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
    public async Task StrictModeEnabled()
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
    public async Task BannedSymbolsAreReported()
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
    public async Task BannedSymbols_NewtonsoftJson_AreReported()
    {
        await using var project = CreateProject();

        var result = await project
            .WithPackage("Newtonsoft.Json", "13.0.4")
            .AddSource("sample.cs", """_ = Newtonsoft.Json.JsonConvert.SerializeObject("test");""")
            .BuildAsync();

        Assert.True(result.HasWarning("RS0030"));
    }

    [Fact]
    public async Task BannedSymbols_NewtonsoftJson_Disabled_AreNotReported()
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
    public async Task EditorConfigsAreInBinlog()
    {
        await using var project = CreateProject();

        var localFile = project.AddFile(".editorconfig", "");
        TestContext.Current.TestOutputHelper?.WriteLine("Local editorconfig path: " + localFile);

        var result = await project
            .AddSource("sample.cs", "_ = System.DateTime.Now;")
            .BuildAsync();

        var files = result.GetBinLogFiles();
        foreach (var file in files) TestContext.Current.TestOutputHelper?.WriteLine("Binlog file: " + file);

        Assert.Contains(files, static f => f.EndsWith(".editorconfig", StringComparison.Ordinal));
        Assert.Contains(files, f => f == localFile || f == "/private" + localFile);
    }

    [Fact]
    public async Task WarningsAsErrorOnGitHubActions()
    {
        await using var project = CreateProject();

        var result = await project
            .AddSource("sample.cs", "_ = System.DateTime.Now;")
            .BuildAsync(environmentVariables: [.. project.GitHubEnvironmentVariables]);

        Assert.True(result.HasError("RS0030"));
    }

    [Fact]
    public async Task Override_WarningsAsErrors()
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
    public async Task NamingConvention_Invalid()
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
    public async Task NamingConvention_Valid()
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
    public async Task CodingStyle_UseExpression()
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
    public async Task CodingStyle_ExpressionIsNeverUsed()
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
    public async Task SdkGlobalEditorConfigTakesPrecedenceOverLocalConfig()
    {
        // SDK's global editorconfigs (is_global=true, global_level=0) take precedence over local .editorconfig files.
        // This is by design - SDK provides baseline standards. Users should override via:
        // - .globalconfig files (higher precedence than SDK globals)
        // - MSBuild properties (<TreatWarningsAsErrors>, etc.)
        // - NOT via local .editorconfig files (which cannot override SDK globals)

        await using var project = CreateProject();

        project.AddFile(".editorconfig", """
            root = true

            [*.cs]
            # The option:severity syntax (e.g., = true:warning) is not respected at build time.
            # Use dotnet_diagnostic syntax for build-time enforcement per Microsoft docs.
            csharp_style_expression_bodied_local_functions = true
            dotnet_diagnostic.IDE0061.severity = warning
            """);

        var result = await project
            .AddSource("Program.cs", """
                _ = "";

                class Sample
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

        // SDK global editorconfig settings take precedence - local .editorconfig cannot override
        Assert.False(result.HasWarning());
        Assert.False(result.HasError());
    }

    [Fact]
    public async Task NuGetAuditIsReportedAsErrorOnGitHubActions()
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
    public async Task NuGetAuditIsReportedAsWarning()
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
    public async Task MsBuildWarningsAsError()
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
    public async Task MSBuildWarningsAsError_NotEnableOnDebug()
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
    public async Task CA1708_NotReportedForFileLocalTypes()
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
    public async Task PdbShouldBeEmbedded_Dotnet_Build()
    {
        await using var project = CreateProject();

        var result = await project
            .AddSource("Program.cs", "Console.WriteLine();")
            .BuildAsync(["--configuration", "Release"]);

        result.ShouldSucceed();

        var outputFiles = Directory.GetFiles(project.RootFolder / "bin", "*", SearchOption.AllDirectories);
        await AssertDebugInfoExists(outputFiles);
    }

    [Fact]
    public async Task Dotnet_Pack_ClassLibrary()
    {
        await using var project = CreateProject();

        await project
            .WithOutputType(Val.Library)
            .PackAsync(["--configuration", "Release"]);

        var extractedPath = project.RootFolder / "extracted";
        var files = Directory.GetFiles(project.RootFolder / "bin" / "Release");

        var nupkgFiles = files.Where(static f => f.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase)).ToArray();
        Assert.Single(nupkgFiles);
        var nupkg = nupkgFiles.Single();
        await ZipFile.ExtractToDirectoryAsync(nupkg, extractedPath, TestContext.Current.CancellationToken);

        var outputFiles = Directory.GetFiles(extractedPath, "*", SearchOption.AllDirectories);
        await AssertDebugInfoExists(outputFiles, true);
        Assert.Contains(outputFiles, static f => f.EndsWith(".xml", StringComparison.Ordinal));
    }

    [Fact]
    public async Task PdbShouldBeEmbedded_Dotnet_Pack()
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

        var extractedPath = project.RootFolder / "extracted";
        var files = Directory.GetFiles(project.RootFolder / "bin" / "Release");

        var nupkgFiles = files.Where(static f => f.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase)).ToArray();
        Assert.Single(nupkgFiles);
        var nupkg = nupkgFiles.Single();
        await ZipFile.ExtractToDirectoryAsync(nupkg, extractedPath, TestContext.Current.CancellationToken);

        var outputFiles = Directory.GetFiles(extractedPath, "*", SearchOption.AllDirectories);
        await AssertDebugInfoExists(outputFiles, true);
        Assert.Contains(outputFiles, static f => f.EndsWith(".xml", StringComparison.Ordinal));
    }

    [Fact]
    public async Task PackageShouldContainsXmlDocumentation()
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

        var extractedPath = project.RootFolder / "extracted";
        var files = Directory.GetFiles(project.RootFolder / "bin" / "Release");

        var nupkgFiles = files.Where(static f => f.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase)).ToArray();
        Assert.Single(nupkgFiles);
        var nupkg = nupkgFiles.Single();
        await ZipFile.ExtractToDirectoryAsync(nupkg, extractedPath, TestContext.Current.CancellationToken);

        var outputFiles = Directory.GetFiles(extractedPath, "*", SearchOption.AllDirectories);
        Assert.Contains(outputFiles, static f => f.EndsWith(".xml", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("readme.md")]
    [InlineData("Readme.md")]
    [InlineData("ReadMe.md")]
    [InlineData("README.md")]
    public async Task Pack_ReadmeFromCurrentFolder(string readmeFileName)
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
        var files = Directory.GetFiles(project.RootFolder / "bin" / "Release");

        var nupkgFiles = files.Where(static f => f.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase)).ToArray();
        Assert.Single(nupkgFiles);
        var nupkg = nupkgFiles.Single();
        await ZipFile.ExtractToDirectoryAsync(nupkg, extractedPath, TestContext.Current.CancellationToken);
        var allFiles = Directory.GetFiles(extractedPath);
        Assert.Contains("README.md", allFiles.Select(Path.GetFileName));
        Assert.Equal("sample",
            await File.ReadAllTextAsync(extractedPath / "README.md", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Pack_ReadmeFromAboveCurrentFolder_SearchReadmeFileAbove_False()
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
        var files = Directory.GetFiles(project.RootFolder / "dir" / "bin" / "Release");

        var nupkgFiles = files.Where(static f => f.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase)).ToArray();
        Assert.Single(nupkgFiles);
        var nupkg = nupkgFiles.Single();
        await ZipFile.ExtractToDirectoryAsync(nupkg, extractedPath, TestContext.Current.CancellationToken);

        Assert.False(File.Exists(extractedPath / "README.md"));
    }

    [Fact]
    public async Task NonANcpLuaCsproj_DoesNotIncludePackageProperties()
    {
        await using var project = CreateProject();

        project.AddFile("LICENSE.txt", "dummy");

        var result = await project
            .WithFilename("sample.csproj")
            .AddSource("Program.cs", "Console.WriteLine();")
            .PackAsync();

        Assert.Equal(0, result.ExitCode);

        var package = Directory.GetFiles(project.RootFolder, "*.nupkg", SearchOption.AllDirectories).Single();
        using var packageReader = new PackageArchiveReader(package);
        var nuspecReader = await packageReader.GetNuspecReaderAsync(TestContext.Current.CancellationToken);
        Assert.NotEqual(SdkBrandingConstants.Author, nuspecReader.GetAuthors());
        Assert.NotEqual("icon.png", nuspecReader.GetIcon());
        Assert.DoesNotContain("icon.png", packageReader.GetFiles());
    }

    [Fact]
    public async Task ANcpLuaCsproj_DoesIncludePackageProperties()
    {
        await using var project = CreateProject();

        project.AddFile("LICENSE.txt", "dummy");

        var result = await project
            .AddSource("Program.cs", "Console.WriteLine();")
            .PackAsync();

        Assert.Equal(0, result.ExitCode);

        var package = Directory.GetFiles(project.RootFolder, "*.nupkg", SearchOption.AllDirectories).Single();
        using var packageReader = new PackageArchiveReader(package);
        var nuspecReader = await packageReader.GetNuspecReaderAsync(TestContext.Current.CancellationToken);
        Assert.Equal(SdkBrandingConstants.Author, nuspecReader.GetAuthors());
        Assert.Null(nuspecReader.GetIcon());
        Assert.DoesNotContain("icon.png", packageReader.GetFiles());
        Assert.Contains("LICENSE.txt", packageReader.GetFiles());
    }

    [Fact]
    public async Task ANcpLuaAnalyzerCsproj()
    {
        await using var project = CreateProject();

        var result = await project
            .WithFilename("ANcpLua.Analyzer.csproj")
            .AddSource("Program.cs", "Console.WriteLine();")
            .BuildAsync();

        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task MTP_OnGitHubActionsShouldAddCustomLogger()
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
            .BuildAsync(environmentVariables: [.. project.GitHubEnvironmentVariables]);

        Assert.Equal(0, result.ExitCode);

        var items = result.GetMsBuildItems("PackageReference");
        Assert.Contains(items, static i => i.Contains("GitHubActionsTestLogger", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task MTP_OnUnknownContextShouldNotAddCustomLogger()
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

        var items = result.GetMsBuildItems("PackageReference");
        Assert.DoesNotContain(items,
            static i => i.Contains("GitHubActionsTestLogger", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CentralPackageManagement()
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
    public async Task SuppressNuGetAudit_NoSuppression_Fails()
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
    public async Task SuppressNuGetAudit_Suppressed()
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
    public async Task Pack_ContainsMetadata()
    {
        await using var project = CreateProject();

        await project.ExecuteGitCommand("init");
        await project.ExecuteGitCommand("add", ".");
        await project.ExecuteGitCommand("commit", "-m", "sample");
        await project.ExecuteGitCommand("remote", "add", "origin", "https://github.com/ancplua/sample.git");

        var result = await project
            .WithFilename("ANcpLua.Sample.csproj")
            .WithOutputType(Val.Library)
            .AddSource("Class1.cs", """
                namespace ANcpLua.Sample;
                public static class Class1
                {
                }
                """)
            .PackAsync(environmentVariables: [.. project.GitHubEnvironmentVariables]);

        Assert.True(result.ExitCode is 0, result.Output.ToString());

        var package = Directory.GetFiles(project.RootFolder, "*.nupkg", SearchOption.AllDirectories).Single();
        using var packageReader = new PackageArchiveReader(package);
        var nuspecReader = await packageReader.GetNuspecReaderAsync(TestContext.Current.CancellationToken);
        Assert.Equal(SdkBrandingConstants.Author, nuspecReader.GetAuthors());
        Assert.Null(nuspecReader.GetIcon());
        Assert.Equal(LicenseType.Expression, nuspecReader.GetLicenseMetadata().Type);
        Assert.Equal(LicenseExpressionType.License, nuspecReader.GetLicenseMetadata().LicenseExpression.Type);
        Assert.Equal("MIT", ((NuGetLicense)nuspecReader.GetLicenseMetadata().LicenseExpression).Identifier);
        Assert.Equal("git", nuspecReader.GetRepositoryMetadata().Type);
        Assert.Equal("https://github.com/ancplua/sample.git", nuspecReader.GetRepositoryMetadata().Url);
        Assert.Equal("refs/heads/main", nuspecReader.GetRepositoryMetadata().Branch);
        Assert.NotEmpty(nuspecReader.GetRepositoryMetadata().Commit);
    }

    [Fact]
    public async Task Web_HasServiceDefaults()
    {
        await using var project = CreateProject(SdkWebName);

        var result = await project
            .WithRootSdk("Microsoft.NET.Sdk.Web")
            .AddSource("Program.cs", """
                using ANcpSdk.AspNetCore.ServiceDefaults;

                var builder = WebApplication.CreateBuilder();
                builder.UseANcpSdkConventions();
                """)
            .BuildAsync(environmentVariables: [.. project.GitHubEnvironmentVariables]);

        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task Web_ServiceDefaultsIsRegisteredAutomatically()
    {
        // Build first, then run the DLL directly to bypass `dotnet run`'s project resolution
        // which fails with "not a valid project file" in test environment with custom SDKs
        await using var project = CreateProject(SdkWebName);

        var buildResult = await project
            .WithRootSdk("Microsoft.NET.Sdk.Web")
            .WithOutputType(Val.Exe)
            .AddSource("Program.cs", """
                using ANcpSdk.AspNetCore.ServiceDefaults;

                var builder = WebApplication.CreateBuilder();
                var app = builder.Build();
                return app.Services.GetService<ANcpSdkServiceDefaultsOptions>() is not null ? 0 : 1;
                """)
            .BuildAsync();

        buildResult.ShouldSucceed();

        // Run the built DLL directly via dotnet (bypass dotnet run's project resolution)
        var dllPath = Directory.GetFiles(project.RootFolder / "bin", "ANcpLua.TestProject.dll", SearchOption.AllDirectories).Single();
        var psi = new ProcessStartInfo(await DotNetSdkHelpers.Get(_dotnetSdkVersion), dllPath)
        {
            WorkingDirectory = project.RootFolder,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        using var process = Process.Start(psi)!;
        await process.WaitForExitAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, process.ExitCode);
    }

    [Fact]
    public async Task Web_InterceptsAreGeneratedForBuildInvocation()
    {
        await using var project = CreateProject(SdkWebName);

        var result = await project
            .WithRootSdk("Microsoft.NET.Sdk.Web")
            .WithProperty("EmitCompilerGeneratedFiles", "true")
            .WithProperty("CompilerGeneratedFilesOutputPath", "obj/Generated")
            .AddSource("Program.cs", """
                using ANcpSdk.AspNetCore.ServiceDefaults;

                var builder = WebApplication.CreateBuilder();
                var app = builder.Build();
                return app.Services.GetService<ANcpSdkServiceDefaultsOptions>() is not null ? 0 : 1;
                """)
            .BuildAsync(environmentVariables: [.. project.GitHubEnvironmentVariables]);

        Assert.Equal(0, result.ExitCode);

        var generatedFiles = Directory.GetFiles(
            project.RootFolder,
            "Intercepts.g.cs",
            SearchOption.AllDirectories);
        Assert.NotEmpty(generatedFiles);

        var generatedSource = await File.ReadAllTextAsync(
            generatedFiles[0],
            TestContext.Current.CancellationToken);
        Assert.Contains("Intercept_Build", generatedSource);
        Assert.Contains("TryUseANcpSdkConventions", generatedSource);
    }

    [Fact]
    public async Task Web_ServiceDefaultsIsRegisteredAutomatically_Disabled()
    {
        // Build first, then run the DLL directly to bypass `dotnet run`'s project resolution
        await using var project = CreateProject(SdkWebName);

        var buildResult = await project
            .WithRootSdk("Microsoft.NET.Sdk.Web")
            .WithOutputType(Val.Exe)
            .WithProperty("AutoRegisterServiceDefaults", "false")
            .AddSource("Program.cs", """
                using ANcpSdk.AspNetCore.ServiceDefaults;

                var builder = WebApplication.CreateBuilder();
                var app = builder.Build();
                return app.Services.GetService<ANcpSdkServiceDefaultsOptions>() is not null ? 0 : 1;
                """)
            .BuildAsync();

        buildResult.ShouldSucceed();

        // Run the built DLL directly via dotnet (bypass dotnet run's project resolution)
        var dllPath = Directory.GetFiles(project.RootFolder / "bin", "ANcpLua.TestProject.dll", SearchOption.AllDirectories).Single();
        var psi = new ProcessStartInfo(await DotNetSdkHelpers.Get(_dotnetSdkVersion), dllPath)
        {
            WorkingDirectory = project.RootFolder,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        using var process = Process.Start(psi)!;
        await process.WaitForExitAsync(TestContext.Current.CancellationToken);

        Assert.NotEqual(0, process.ExitCode);
    }

    [Theory]
    [InlineData(SdkName)]
    [InlineData(SdkTestName)]
    [InlineData(SdkWebName)]
    public async Task AssemblyContainsMetadataAttributeWithSdkName(string sdkName)
    {
        await using var project = CreateProject(sdkName);

        project.AddDirectoryBuildPropsFile("", sdkName: sdkName);

        // Test SDK requires OutputType=Exe (for xunit.v3 MTP), use top-level statements
        // Web SDK defaults to Exe, use Library to override
        // Base SDK uses Library by default
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

        await using var assembly = File.OpenRead(dllPath);
        using var reader = new PEReader(assembly);
        var metadata = reader.GetMetadataReader();
        foreach (var attrHandle in metadata.CustomAttributes)
        {
            var customAttribute = metadata.GetCustomAttribute(attrHandle);
            var attributeType = customAttribute.Constructor;
            var typeName = metadata.GetString(metadata
                .GetTypeReference(
                    (TypeReferenceHandle)metadata.GetMemberReference((MemberReferenceHandle)attributeType).Parent)
                .Name);
            if (typeName is "AssemblyMetadataAttribute")
            {
                var blobReader = metadata.GetBlobReader(customAttribute.Value);
                _ = blobReader.ReadSerializedString();
                var key = blobReader.ReadSerializedString();
                var value = blobReader.ReadSerializedString();

                Assert.Equal(SdkBrandingConstants.SdkMetadataKey, key);
                Assert.Equal(sdkName, value);
                return;
            }
        }

        Assert.Fail("Attribute not found");
    }

    [Theory]
    [InlineData("TargetFramework", "")]
    [InlineData("TargetFrameworks", "")]
    public async Task SetTargetFramework(string propName, string version)
    {
        await using var project = CreateProject();

        // Use Library to avoid entry point requirement - this test validates TargetFramework, not exe behavior
        var result = await project
            .WithFilename("Sample.csproj")
            .WithOutputType(Val.Library)
            .WithProperty(propName, version)
            .AddSource("Sample.cs", "namespace Sample; public class Foo { }")
            .BuildAsync();

        Assert.True(result.ExitCode is 0, result.Output.ToString());
        var dllPath = Directory
            .GetFiles(project.RootFolder / "bin" / "Debug", "Sample.dll", SearchOption.AllDirectories).Single();

        var expectedVersion = version;
        if (string.IsNullOrEmpty(expectedVersion))
            expectedVersion = _dotnetSdkVersion switch
            {
                NetSdkVersion.Net100 => "net10.0",
                _ => throw new NotSupportedException()
            };

        await using var assembly = File.OpenRead(dllPath);
        using var reader = new PEReader(assembly);
        var metadata = reader.GetMetadataReader();
        foreach (var attrHandle in metadata.CustomAttributes)
        {
            var customAttribute = metadata.GetCustomAttribute(attrHandle);
            var attributeType = customAttribute.Constructor;
            var typeName = metadata.GetString(metadata
                .GetTypeReference(
                    (TypeReferenceHandle)metadata.GetMemberReference((MemberReferenceHandle)attributeType).Parent)
                .Name);
            if (typeName is "TargetFrameworkAttribute")
            {
                var blobReader = metadata.GetBlobReader(customAttribute.Value);
                _ = blobReader.ReadSerializedString();
                var key = blobReader.ReadSerializedString();

                Assert.Contains(expectedVersion.Replace("net", "v", StringComparison.Ordinal), key);
                return;
            }
        }

        Assert.Fail("Attribute not found");
    }

    [Fact]
    public async Task NpmInstall()
    {
        await using var project = CreateProject(SdkWebName);

        project.AddFile("package.json", """
            {
              "name": "sample",
              "version": "1.0.0",
              "private": true,
              "devDependencies": {
                "is-number": "7.0.0"
              }
            }
            """);

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
    public async Task NpmRestore()
    {
        await using var project = CreateProject(SdkWebName);

        project.AddFile("package.json", """
            {
              "name": "sample",
              "version": "1.0.0",
              "private": true,
              "devDependencies": {
                "is-number": "7.0.0"
              }
            }
            """);

        var result = await project
            .AddSource("Program.cs", "Console.WriteLine();")
            .RestoreAsync();

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(project.RootFolder / "package-lock.json"));
        Assert.True(File.Exists(project.RootFolder / "node_modules" / ".npm-install-stamp"));
    }

    [Fact]
    public async Task Npm_Dotnet_Build_sln()
    {
        await using var project = CreateProject(SdkWebName);

        var slnFile = project.AddFile("sample.slnx", """
            <Solution>
                <Project Path="sample.csproj" />
            </Solution>
            """);
        project.AddFile("package.json", """
            {
              "name": "sample",
              "version": "1.0.0",
              "private": true,
              "devDependencies": {
                "is-number": "7.0.0"
              }
            }
            """);

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
    public async Task Npm_Dotnet_sln(string command)
    {
        await using var project = CreateProject(SdkWebName);

        var slnFile = project.AddFile("sample.slnx", """
            <Solution>
                <Project Path="sample.csproj" />
            </Solution>
            """);
        project.AddFile("package.json", """
            {
              "name": "sample",
              "version": "1.0.0",
              "private": true,
              "devDependencies": {
                "is-number": "7.0.0"
              }
            }
            """);

        // BuildAsync generates csproj and source files, then runs 'dotnet build slnFile'
        // For publish, we first build (which generates csproj), then run publish
        // Need OutputType=Exe for top-level statements
        project.WithFilename("sample.csproj").WithOutputType(Val.Exe).AddSource("Program.cs", "Console.WriteLine();");

        BuildResult result;
        if (command == "build")
            result = await project.BuildAsync([slnFile]);
        else
        {
            // Build first to generate csproj, then publish
            await project.BuildAsync([slnFile]);
            result = await project.ExecuteDotnetCommandAsync("publish", [slnFile]);
        }

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(project.RootFolder / "package-lock.json"));
        Assert.True(File.Exists(project.RootFolder / "node_modules" / ".npm-install-stamp"));
    }

    [Fact]
    public async Task NpmRestore_MultipleFiles()
    {
        await using var project = CreateProject(SdkWebName);

        var npmItems = new XElement("ItemGroup",
            new XElement("NpmPackageFile", new XAttribute("Include", "a/package.json")),
            new XElement("NpmPackageFile", new XAttribute("Include", "b/package.json")));

        project.AddFile("a/package.json", """
            {
              "name": "sample",
              "version": "1.0.0",
              "private": true,
              "devDependencies": {
                "is-number": "7.0.0"
              }
            }
            """);
        project.AddFile("b/package.json", """
            {
              "name": "sample",
              "version": "1.0.0",
              "private": true,
              "devDependencies": {
                "is-number": "7.0.0"
              }
            }
            """);

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
    public async Task Npm_Dotnet_Build_RestoreLockedMode_Fail()
    {
        await using var project = CreateProject(SdkWebName);

        project.AddFile("package.json", """
            {
              "name": "sample",
              "version": "1.0.0",
              "private": true,
              "devDependencies": {
                "is-number": "7.0.0"
              }
            }
            """);

        var result = await project
            .AddSource("Program.cs", "Console.WriteLine();")
            .BuildAsync(["/p:RestoreLockedMode=true"]);

        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [InlineData("/p:RestoreLockedMode=true")]
    [InlineData("/p:ContinuousIntegrationBuild=true")]
    public async Task Npm_Dotnet_Build_Ci_Success(string command)
    {
        await using var project = CreateProject(SdkWebName);

        project.AddFile("package.json", """
            {
              "name": "sample",
              "version": "1.0.0",
              "private": true,
              "devDependencies": {
                "is-number": "7.0.0"
              }
            }
            """);
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

    /// <summary>
    ///     Verifies debug info exists. SDK uses DebugType=portable with snupkg symbol packages.
    ///     For build output: portable PDB file should exist alongside DLL.
    ///     For pack output (nupkg extraction): PDB is in snupkg, not in main package.
    /// </summary>
    private static async Task AssertDebugInfoExists(IReadOnlyCollection<string> outputFiles, bool isPackOutput = false)
    {
        var dllPath = outputFiles.Single(static f => f.EndsWith(".dll", StringComparison.OrdinalIgnoreCase));
        await using var stream = File.OpenRead(dllPath);
        var peReader = new PEReader(stream);
        var debug = peReader.ReadDebugDirectory();

        if (isPackOutput)
            Assert.Contains(debug, static entry => entry.Type == DebugDirectoryEntryType.CodeView);
        else
        {
            Assert.Contains(outputFiles, static f => f.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(debug, static entry => entry.Type == DebugDirectoryEntryType.CodeView);
        }
    }
}
