using System.IO.Compression;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Xml.Linq;
using ANcpLua.Sdk.Tests.Helpers;
using ANcpLua.Sdk.Tests.Infrastructure;
using Meziantou.Framework;
using NuGet.Packaging;
using NuGet.Packaging.Licenses;
using static ANcpLua.Sdk.Tests.Helpers.PackageFixture;
using static ANcpLua.Sdk.Tests.Infrastructure.SdkBrandingConstants;
using Task = System.Threading.Tasks.Task;

namespace ANcpLua.Sdk.Tests;

public sealed class Sdk100RootTests(PackageFixture fixture, ITestOutputHelper testOutputHelper)
    : SdkTests(fixture, testOutputHelper, NetSdkVersion.Net100, SdkImportStyle.ProjectElement);

public sealed class Sdk100InnerTests(PackageFixture fixture, ITestOutputHelper testOutputHelper)
    : SdkTests(fixture, testOutputHelper, NetSdkVersion.Net100, SdkImportStyle.SdkElement);

public sealed class Sdk100DirectoryBuildPropsTests(PackageFixture fixture, ITestOutputHelper testOutputHelper)
    : SdkTests(fixture, testOutputHelper, NetSdkVersion.Net100, SdkImportStyle.SdkElementDirectoryBuildProps);

public abstract class SdkTests(
    PackageFixture fixture,
    ITestOutputHelper testOutputHelper,
    NetSdkVersion dotnetSdkVersion,
    SdkImportStyle sdkImportStyle)
{
    // note: don't simplify names as they are used in the Renovate regex
    // All test projects use MTP (Microsoft Testing Platform) - VSTest is deprecated on .NET 10+
    private static readonly NuGetReference[] XUnitMtpReferences =
        [new("xunit.v3.mtp-v2", "3.2.1")];

    private readonly NetSdkVersion _dotnetSdkVersion = dotnetSdkVersion;
    private readonly PackageFixture _fixture = fixture;
    private readonly SdkImportStyle _sdkImportStyle = sdkImportStyle;
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

    private ProjectBuilder CreateProjectBuilder(string defaultSdkName = SdkName)
    {
        var builder = new ProjectBuilder(_fixture, _testOutputHelper, _sdkImportStyle, defaultSdkName);
        builder.SetDotnetSdkVersion(_dotnetSdkVersion);
        return builder;
    }

    [Fact]
    public void PackageReferenceAreValid()
    {
        // Verify SDK-injected packages have Version attribute for CPM compatibility
        var root = RepositoryRoot.Locate()["src"];
        var files = Directory.GetFiles(root, "*", SearchOption.AllDirectories).Select(FullPath.FromPath);
        foreach (var file in files)
            if (file.Extension is ".props" or ".targets")
            {
                var doc = XDocument.Load(file);
                // Skip PackageReferences inside ItemDefinitionGroup (they set defaults, not actual references)
                var nodes = doc.Descendants("PackageReference")
                    .Where(static n => n.Parent?.Name.LocalName != "ItemDefinitionGroup");
                foreach (var node in nodes)
                {
                    var versionAttr = node.Attribute("Version");
                    if (versionAttr is null)
                        Assert.Fail("Missing Version attribute on " + node);
                }
            }
    }

    [Fact]
    public async Task ValidateDefaultProperties()
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile([("OutputType", "Library")]);
        project.AddFile("sample.cs", """
                                     namespace TestProject;

                                     /// <summary>Sample class for SDK validation.</summary>
                                     public class Sample { }
                                     """);
        var data = await project.BuildAndGetOutput();
        data.AssertMsBuildPropertyValue("LangVersion", "latest");
        data.AssertMsBuildPropertyValue("PublishRepositoryUrl", "true");
        // DebugType=portable to support snupkg symbol packages
        data.AssertMsBuildPropertyValue("DebugType", "portable");
        data.AssertMsBuildPropertyValue("EmbedAllSources", "true");
        data.AssertMsBuildPropertyValue("EnableNETAnalyzers", "true");
        data.AssertMsBuildPropertyValue("AnalysisLevel", "latest-all");
        data.AssertMsBuildPropertyValue("EnablePackageValidation", "true");
        data.AssertMsBuildPropertyValue("RollForward", "LatestMajor");
    }

    [Fact]
    public async Task ValidateDefaultProperties_Test()
    {
        await using var project = CreateProjectBuilder(SdkTestName);
        project.AddCsprojFile();
        var data = await project.BuildAndGetOutput();
        data.AssertMsBuildPropertyValue("RollForward", null);
    }

    [Fact]
    public async Task CanOverrideLangVersion()
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile([("LangVersion", "preview")]);
        project.AddFile("sample.cs", "Console.WriteLine();");
        var data = await project.BuildAndGetOutput();
        data.AssertMsBuildPropertyValue("LangVersion", "preview");
    }

    [Fact]
    public async Task GenerateSbom_IsSetWhenContinuousIntegrationBuildIsSet()
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile([("ContinuousIntegrationBuild", "true")]);
        project.AddFile("Program.cs", "Console.WriteLine();");
        var data = await project.PackAndGetOutput();
        data.AssertMsBuildPropertyValue("GenerateSBOM", "true");

        var nupkgFile = Directory.GetFiles(project.RootFolder, "*.nupkg", SearchOption.AllDirectories).Single();
        await using var archive = await ZipFile.OpenReadAsync(nupkgFile, TestContext.Current.CancellationToken);
        Assert.Contains(archive.Entries,
            e => e.FullName.EndsWith("manifest.spdx.json", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GenerateSbom_IsNotSetWhenContinuousIntegrationBuildIsNotSet()
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile();
        project.AddFile("Program.cs", "Console.WriteLine();");
        var data = await project.PackAndGetOutput();

        Assert.False(
            data.OutputContains("SbomFilePath=") ||
            data.OutputContains("manifest.spdx.json"),
            data.Output.ToString());
    }

    [Fact]
    public async Task CanOverrideRollForward()
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile([("RollForward", "Minor")]);
        project.AddFile("sample.cs", "Console.WriteLine();");
        var data = await project.BuildAndGetOutput();
        data.AssertMsBuildPropertyValue("RollForward", "Minor");
    }

    [Fact]
    public async Task RollForwardIsCompatibleWithClassLibraries()
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile([("OutputType", "Library")]);
        var data = await project.BuildAndGetOutput();
        data.AssertMsBuildPropertyValue("RollForward", "LatestMajor");
    }

    [Fact]
    public async Task CanOverrideLangVersionInDirectoryBuildProps()
    {
        if (_sdkImportStyle is SdkImportStyle.SdkElement)
            Assert.Skip("Directory.Build.props is not supported with SdkImportStyle.SdkElement");

        await using var project = CreateProjectBuilder();
        project.AddCsprojFile();
        project.AddDirectoryBuildPropsFile("""
                                           <PropertyGroup>
                                               <LangVersion>preview</LangVersion>
                                           </PropertyGroup>
                                           """);
        project.AddFile("sample.cs", "Console.WriteLine();");
        var data = await project.BuildAndGetOutput();
        data.AssertMsBuildPropertyValue("LangVersion", "preview");
    }

    [Fact]
    public async Task AllowUnsafeBlock()
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile();
        project.AddFile("sample.cs", """
                                     unsafe
                                     {
                                         int* p = null;
                                     }
                                     """);

        var data = await project.BuildAndGetOutput();
        Assert.Equal(0, data.ExitCode);
    }

    [Fact]
    public async Task StrictModeEnabled()
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile();
        project.AddFile("sample.cs", """
                                     var o = new object();
                                     if (o is Math) // Error CS7023 The second operand of an 'is' or 'as' operator may not be static type 'Math'
                                     {
                                     }
                                     """);

        var data = await project.BuildAndGetOutput();
        Assert.True(data.HasWarning("CS7023"));
    }

    [Fact]
    public async Task BannedSymbolsAreReported()
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile();
        project.AddFile("sample.cs", "_ = System.DateTime.Now;");
        var data = await project.BuildAndGetOutput();
        Assert.True(data.HasWarning("RS0030"));

        var files = data.GetBinLogFiles();
        Assert.Contains(files, static f => f.EndsWith("BannedSymbols.txt", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task BannedSymbols_NewtonsoftJson_AreReported()
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile(nuGetPackages: [new NuGetReference("Newtonsoft.Json", "13.0.4")]);
        project.AddFile("sample.cs", """_ = Newtonsoft.Json.JsonConvert.SerializeObject("test");""");
        var data = await project.BuildAndGetOutput();
        Assert.True(data.HasWarning("RS0030"));
    }

    [Fact]
    public async Task BannedSymbols_NewtonsoftJson_Disabled_AreNotReported()
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile([("BannedNewtonsoftJsonSymbols", "false")],
            [new NuGetReference("Newtonsoft.Json", "13.0.4")]);
        project.AddFile("sample.cs", """_ = Newtonsoft.Json.JsonConvert.SerializeObject("test");""");
        var data = await project.BuildAndGetOutput();
        Assert.False(data.HasWarning("RS0030"));
    }

    [Fact]
    public async Task EditorConfigsAreInBinlog()
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile();
        project.AddFile("sample.cs", "_ = System.DateTime.Now;");
        var localFile = project.AddFile(".editorconfig", "");
        TestContext.Current.TestOutputHelper?.WriteLine("Local editorconfig path: " + localFile);

        var data = await project.BuildAndGetOutput();

        var files = data.GetBinLogFiles();
        foreach (var file in files) TestContext.Current.TestOutputHelper?.WriteLine("Binlog file: " + file);

        Assert.Contains(files, static f => f.EndsWith(".editorconfig", StringComparison.Ordinal));
        Assert.Contains(files, f => f == localFile || f == "/private" + localFile); // macos may prefix it with /private
    }

    [Fact]
    public async Task WarningsAsErrorOnGitHubActions()
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile();
        project.AddFile("sample.cs", "_ = System.DateTime.Now;");
        var data = await project.BuildAndGetOutput(environmentVariables: [.. project.GitHubEnvironmentVariables]);
        Assert.True(data.HasError("RS0030"));
    }

    [Fact]
    public async Task Override_WarningsAsErrors()
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile([("TreatWarningsAsErrors", "false")]);
        project.AddFile("sample.cs", """
                                     _ = "";

                                     class Sample
                                     {
                                         private readonly int field;

                                         public Sample(int a) => field = a;

                                         public int A() => field;
                                     }
                                     """);
        var data = await project.BuildAndGetOutput(["--configuration", "Release"]);
        Assert.True(data.HasWarning("IDE1006"));
    }

    [Fact]
    public async Task NamingConvention_Invalid()
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile();
        project.AddFile("sample.cs", """
                                     _ = "";

                                     class Sample
                                     {
                                         private readonly int field;

                                         public Sample(int a) => field = a;

                                         public int A() => field;
                                     }
                                     """);
        var data = await project.BuildAndGetOutput(["--configuration", "Release"]);
        Assert.True(data.HasError("IDE1006"));
    }

    [Fact]
    public async Task NamingConvention_Valid()
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile();
        project.AddFile("sample.cs", """
                                     _ = "";

                                     class Sample
                                     {
                                         private int _field;
                                     }
                                     """);
        var data = await project.BuildAndGetOutput(["--configuration", "Release"]);
        Assert.False(data.HasError("IDE1006"));
        Assert.False(data.HasWarning("IDE1006"));
    }

    [Fact]
    public async Task CodingStyle_UseExpression()
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile();
        project.AddFile("Program.cs", """
                                      A();

                                      static void A()
                                      {
                                          System.Console.WriteLine();
                                      }
                                      """);
        var data = await project.BuildAndGetOutput(["--configuration", "Release"]);
        Assert.False(data.HasWarning());
        Assert.False(data.HasError());
    }

    [Fact]
    public async Task CodingStyle_ExpressionIsNeverUsed()
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile();
        project.AddFile("Program.cs", """
                                      var sb = new System.Text.StringBuilder();
                                      sb.AppendLine();

                                      """);
        var data = await project.BuildAndGetOutput(["--configuration", "Release"]);
        Assert.False(data.HasWarning());
        Assert.False(data.HasError());
    }

    [Fact]
    public async Task LocalEditorConfigCanOverrideSettings()
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile();
        project.AddFile("Program.cs", """
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

                                      """);
        project.AddFile(".editorconfig", """
                                         [*.cs]
                                         csharp_style_expression_bodied_local_functions = true:warning
                                         """);

        var data = await project.BuildAndGetOutput(["--configuration", "Debug"]);
        Assert.True(data.HasWarning());
        Assert.False(data.HasError());
    }

    [Fact]
    public async Task NuGetAuditIsReportedAsErrorOnGitHubActions()
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile(nuGetPackages: [new NuGetReference("System.Net.Http", "4.3.3")]);
        project.AddFile("Program.cs", "System.Console.WriteLine();");
        var data = await project.BuildAndGetOutput(environmentVariables: [.. project.GitHubEnvironmentVariables]);
        Assert.True(data.OutputContains("error NU1903"));
        Assert.Equal(1, data.ExitCode);
    }

    [Fact]
    public async Task NuGetAuditIsReportedAsWarning()
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile(nuGetPackages: [new NuGetReference("System.Net.Http", "4.3.3")]);
        project.AddFile("Program.cs", "System.Console.WriteLine();");
        var data = await project.BuildAndGetOutput();
        Assert.True(data.OutputContains("warning NU1903"));
        Assert.True(data.OutputDoesNotContain("error NU1903"));
        Assert.Equal(0, data.ExitCode);
    }

    [Fact]
    public async Task MsBuildWarningsAsError()
    {
        await using var project = CreateProjectBuilder();
        project.AddFile("Program.cs", """
                                      System.Console.WriteLine();

                                      """);
        project.AddCsprojFile(additionalProjectElements:
        [
            new XElement("Target", new XAttribute("Name", "Custom"), new XAttribute("BeforeTargets", "Build"),
                new XElement("Warning", new XAttribute("Text", "CustomWarning")))
        ]);
        var data = await project.BuildAndGetOutput(["--configuration", "Release"]);

        Assert.True(data.OutputContains("error : CustomWarning"));
    }

    [Fact]
    public async Task MSBuildWarningsAsError_NotEnableOnDebug()
    {
        await using var project = CreateProjectBuilder();
        project.AddFile("Program.cs", "System.Console.WriteLine();");
        project.AddCsprojFile(additionalProjectElements:
        [
            new XElement("Target", new XAttribute("Name", "Custom"), new XAttribute("BeforeTargets", "Build"),
                new XElement("Warning", new XAttribute("Text", "CustomWarning")))
        ]);
        var data = await project.BuildAndGetOutput(["--configuration", "Debug"]);

        Assert.True(data.OutputContains("warning : CustomWarning"));
    }

    [Fact]
    public async Task CA1708_NotReportedForFileLocalTypes()
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile();
        project.AddFile("Sample1.cs", """
                                      System.Console.WriteLine();

                                      class A {}

                                      file class Sample
                                      {
                                      }
                                      """);
        project.AddFile("Sample2.cs", """
                                      class B {}

                                      file class sample
                                      {
                                      }
                                      """);
        var data = await project.BuildAndGetOutput(["--configuration", "Release"]);
        Assert.False(data.HasError("CA1708"));
        Assert.False(data.HasWarning("CA1708"));
    }

    [Fact]
    public async Task PdbShouldBeEmbedded_Dotnet_Build()
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile();
        project.AddFile("Program.cs", "Console.WriteLine();");

        var data = await project.BuildAndGetOutput(["--configuration", "Release"]);

        var outputFiles = Directory.GetFiles(project.RootFolder / "bin", "*", SearchOption.AllDirectories);
        await AssertDebugInfoExists(outputFiles);
    }

    [Fact]
    public async Task Dotnet_Pack_ClassLibrary()
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile([("OutputType", "Library")]);
        var data = await project.PackAndGetOutput(["--configuration", "Release"]);

        var extractedPath = project.RootFolder / "extracted";
        var files = Directory.GetFiles(project.RootFolder / "bin" / "Release");
        // Filter to only .nupkg files (excluding .snupkg symbol packages)
        var nupkgFiles = files.Where(f => f.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase)).ToArray();
        Assert.Single(nupkgFiles);
        var nupkg = nupkgFiles.Single();
        await ZipFile.ExtractToDirectoryAsync(nupkg, extractedPath, TestContext.Current.CancellationToken);

        var outputFiles = Directory.GetFiles(extractedPath, "*", SearchOption.AllDirectories);
        await AssertDebugInfoExists(outputFiles, true);
        Assert.Contains(outputFiles, f => f.EndsWith(".xml", StringComparison.Ordinal));
    }

    [Fact]
    public async Task PdbShouldBeEmbedded_Dotnet_Pack()
    {
        await using var project = CreateProjectBuilder();
        // Use Library OutputType for packable project (exe projects don't produce nupkg by default)
        project.AddCsprojFile([("OutputType", "Library")]);
        project.AddFile("Class1.cs", """
                                     namespace TestProject;

                                     /// <summary>Test class.</summary>
                                     public class Class1 { }
                                     """);

        var data = await project.PackAndGetOutput(["--configuration", "Release"]);
        Assert.True(data.ExitCode == 0, $"Pack failed with exit code {data.ExitCode}: {data.Output}");

        var extractedPath = project.RootFolder / "extracted";
        var files = Directory.GetFiles(project.RootFolder / "bin" / "Release");
        // Filter to only .nupkg files (excluding .snupkg symbol packages)
        var nupkgFiles = files.Where(f => f.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase)).ToArray();
        Assert.Single(nupkgFiles);
        var nupkg = nupkgFiles.Single();
        await ZipFile.ExtractToDirectoryAsync(nupkg, extractedPath, TestContext.Current.CancellationToken);

        var outputFiles = Directory.GetFiles(extractedPath, "*", SearchOption.AllDirectories);
        await AssertDebugInfoExists(outputFiles, true);
        Assert.Contains(outputFiles, f => f.EndsWith(".xml", StringComparison.Ordinal));
    }

    [Fact]
    public async Task PackageShouldContainsXmlDocumentation()
    {
        await using var project = CreateProjectBuilder();
        // Use Library OutputType for packable project (exe projects don't produce nupkg by default)
        project.AddCsprojFile([("OutputType", "Library")]);
        project.AddFile("Class1.cs", """
                                     namespace TestProject;

                                     /// <summary>Test class.</summary>
                                     public class Class1 { }
                                     """);

        var data = await project.PackAndGetOutput();
        Assert.True(data.ExitCode == 0, $"Pack failed with exit code {data.ExitCode}: {data.Output}");

        var extractedPath = project.RootFolder / "extracted";
        var files = Directory.GetFiles(project.RootFolder / "bin" / "Release");
        // Filter to only .nupkg files (excluding .snupkg symbol packages)
        var nupkgFiles = files.Where(f => f.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase)).ToArray();
        Assert.Single(nupkgFiles);
        var nupkg = nupkgFiles.Single();
        await ZipFile.ExtractToDirectoryAsync(nupkg, extractedPath, TestContext.Current.CancellationToken);

        var outputFiles = Directory.GetFiles(extractedPath, "*", SearchOption.AllDirectories);
        Assert.Contains(outputFiles, f => f.EndsWith(".xml", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("readme.md")]
    [InlineData("Readme.md")]
    [InlineData("ReadMe.md")]
    [InlineData("README.md")]
    public async Task Pack_ReadmeFromCurrentFolder(string readmeFileName)
    {
        await using var project = CreateProjectBuilder();
        // Use Library OutputType for packable project (exe projects don't produce nupkg by default)
        project.AddCsprojFile([("OutputType", "Library")]);
        project.AddFile("Class1.cs", """
                                     namespace TestProject;

                                     /// <summary>Test class.</summary>
                                     public class Class1 { }
                                     """);
        project.AddFile(readmeFileName, "sample");

        var data = await project.PackAndGetOutput(["--configuration", "Release"]);
        Assert.True(data.ExitCode == 0, $"Pack failed with exit code {data.ExitCode}: {data.Output}");

        var extractedPath = project.RootFolder / "extracted";
        var files = Directory.GetFiles(project.RootFolder / "bin" / "Release");
        // Filter to only .nupkg files (excluding .snupkg symbol packages)
        var nupkgFiles = files.Where(f => f.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase)).ToArray();
        Assert.Single(nupkgFiles);
        var nupkg = nupkgFiles.Single();
        await ZipFile.ExtractToDirectoryAsync(nupkg, extractedPath, TestContext.Current.CancellationToken);
        var allFiles = Directory.GetFiles(extractedPath);
        Assert.Contains("README.md", allFiles.Select(Path.GetFileName));
        Assert.Equal("sample",
            await File.ReadAllTextAsync(extractedPath / "README.md", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Pack_ReadmeFromAboveCurrentFolder_SearchReadmeFileAbove_True()
    {
        // Skip for SdkElement style - SourceLink has import order issues with nested <Sdk> elements
        if (_sdkImportStyle is SdkImportStyle.SdkElement)
            Assert.Skip("SourceLink fails to locate git repo with SdkImportStyle.SdkElement in subdirectory projects");

        await using var project = CreateProjectBuilder();
        // Use Library OutputType for packable project (exe projects don't produce nupkg by default)
        project.AddCsprojFile(
            filename: "dir/Test.csproj",
            properties: [("SearchReadmeFileAbove", "true"), ("OutputType", "Library")]);
        project.AddFile("dir/Class1.cs", """
                                         namespace TestProject;

                                         /// <summary>Test class.</summary>
                                         public class Class1 { }
                                         """);
        project.AddFile("README.md", "sample");

        // Initialize git repository (required for SourceLink/Microsoft.Build.Tasks.Git)
        await project.ExecuteGitCommand("init");
        await project.ExecuteGitCommand("add", ".");
        await project.ExecuteGitCommand("commit", "-m", "sample");

        var data = await project.PackAndGetOutput(["dir", "--configuration", "Release"]);
        Assert.True(data.ExitCode == 0, $"Pack failed with exit code {data.ExitCode}: {data.Output}");

        var extractedPath = project.RootFolder / "extracted";
        var files = Directory.GetFiles(project.RootFolder / "dir" / "bin" / "Release");
        // Filter to only .nupkg files (excluding .snupkg symbol packages)
        var nupkgFiles = files.Where(f => f.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase)).ToArray();
        Assert.Single(nupkgFiles);
        var nupkg = nupkgFiles.Single();
        await ZipFile.ExtractToDirectoryAsync(nupkg, extractedPath, TestContext.Current.CancellationToken);

        Assert.Equal("sample",
            await File.ReadAllTextAsync(extractedPath / "README.md", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Pack_ReadmeFromAboveCurrentFolder_SearchReadmeFileAbove_False()
    {
        await using var project = CreateProjectBuilder();
        // Use Library OutputType for packable project (exe projects don't produce nupkg by default)
        project.AddCsprojFile(filename: "dir/Test.csproj", properties: [("OutputType", "Library")]);
        project.AddFile("dir/Class1.cs", """
                                         namespace TestProject;

                                         /// <summary>Test class.</summary>
                                         public class Class1 { }
                                         """);
        project.AddFile("README.md", "sample");

        var data = await project.PackAndGetOutput(["dir", "--configuration", "Release"]);
        Assert.True(data.ExitCode == 0, $"Pack failed with exit code {data.ExitCode}: {data.Output}");

        var extractedPath = project.RootFolder / "extracted";
        var files = Directory.GetFiles(project.RootFolder / "dir" / "bin" / "Release");
        // Filter to only .nupkg files (excluding .snupkg symbol packages)
        var nupkgFiles = files.Where(f => f.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase)).ToArray();
        Assert.Single(nupkgFiles);
        var nupkg = nupkgFiles.Single();
        await ZipFile.ExtractToDirectoryAsync(nupkg, extractedPath, TestContext.Current.CancellationToken);

        Assert.False(File.Exists(extractedPath / "README.md"));
    }

    // Note: DotnetTestSkipAnalyzers tests removed - they require actual test runs
    // but MTP/VSTest detection issues on .NET 10 prevent reliable testing.
    // The DisableAnalyzerWhenRunningTests target only fires during test execution.

    [Fact]
    public async Task NonANcpLuaCsproj_DoesNotIncludePackageProperties()
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile(filename: "sample.csproj");
        project.AddFile("Program.cs", "Console.WriteLine();");
        project.AddFile("LICENSE.txt", "dummy");
        var data = await project.PackAndGetOutput();
        Assert.Equal(0, data.ExitCode);

        var package = Directory.GetFiles(project.RootFolder, "*.nupkg", SearchOption.AllDirectories).Single();
        using var packageReader = new PackageArchiveReader(package);
        var nuspecReader = await packageReader.GetNuspecReaderAsync(TestContext.Current.CancellationToken);
        Assert.NotEqual(Author, nuspecReader.GetAuthors());
        Assert.NotEqual("icon.png", nuspecReader.GetIcon());
        Assert.DoesNotContain("icon.png", packageReader.GetFiles());
    }

    [Fact]
    public async Task ANcpLuaCsproj_DoesIncludePackageProperties()
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile();
        project.AddFile("Program.cs", "Console.WriteLine();");
        project.AddFile("LICENSE.txt", "dummy");
        var data = await project.PackAndGetOutput();
        Assert.Equal(0, data.ExitCode);

        var package = Directory.GetFiles(project.RootFolder, "*.nupkg", SearchOption.AllDirectories).Single();
        using var packageReader = new PackageArchiveReader(package);
        var nuspecReader = await packageReader.GetNuspecReaderAsync(TestContext.Current.CancellationToken);
        Assert.Equal(Author, nuspecReader.GetAuthors());
        Assert.Null(nuspecReader.GetIcon());
        Assert.DoesNotContain("icon.png", packageReader.GetFiles());
        Assert.Contains("LICENSE.txt", packageReader.GetFiles());
    }

    [Fact]
    public async Task ANcpLuaAnalyzerCsproj()
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile(filename: "ANcpLua.Analyzer.csproj");
        project.AddFile("Program.cs", "Console.WriteLine();");
        var data = await project.BuildAndGetOutput();
        Assert.Equal(0, data.ExitCode);
    }

    [Fact]
    public async Task MTP_OnGitHubActionsShouldAddCustomLogger()
    {
        // Test SDK (SdkTestName) already injects xunit.v3.mtp-v2 via Testing.props
        await using var project = CreateProjectBuilder(SdkTestName);
        project.AddCsprojFile(filename: "Sample.Tests.csproj");

        project.AddFile("Program.cs", """
                                      public class Tests
                                      {
                                          [Fact]
                                          public void Test1()
                                          {
                                              Assert.True(true);
                                          }
                                      }
                                      """);

        // Build only to verify GitHubActionsTestLogger is injected
        var data = await project.BuildAndGetOutput(environmentVariables: [.. project.GitHubEnvironmentVariables]);

        Assert.Equal(0, data.ExitCode);
        // Verify GitHubActionsTestLogger is injected on GitHub Actions
        var items = data.GetMsBuildItems("PackageReference");
        Assert.Contains(items, i => i.Contains("GitHubActionsTestLogger", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task MTP_OnUnknownContextShouldNotAddCustomLogger()
    {
        // Test SDK (SdkTestName) already injects xunit.v3.mtp-v2 via Testing.props
        await using var project = CreateProjectBuilder(SdkTestName);
        project.AddCsprojFile(filename: "Sample.Tests.csproj");

        project.AddFile("Program.cs", """
                                      public class Tests
                                      {
                                          [Fact]
                                          public void Test1()
                                          {
                                              Assert.True(true);
                                          }
                                      }
                                      """);

        // Build only - not on GitHub Actions, so no logger should be injected
        var data = await project.BuildAndGetOutput();

        Assert.Equal(0, data.ExitCode);
        // Verify GitHubActionsTestLogger is NOT injected when not on GitHub Actions
        var items = data.GetMsBuildItems("PackageReference");
        Assert.DoesNotContain(items, i => i.Contains("GitHubActionsTestLogger", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CentralPackageManagement()
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile(
            sdk: SdkTestName,
            filename: "Sample.Tests.csproj"
        );

        project.AddFile("Program.cs", "Console.WriteLine();");

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

        var data = await project.BuildAndGetOutput();
        Assert.Equal(0, data.ExitCode);
    }

    [Fact]
    public async Task SuppressNuGetAudit_NoSuppression_Fails()
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile(
            nuGetPackages: [new NuGetReference("System.Net.Http", "4.3.3")],
            properties: [("NOWARN", "$(NOWARN);NU1510")]);

        project.AddFile("Program.cs", "Console.WriteLine();");

        var data = await project.BuildAndGetOutput(["--configuration", "Release"]);
        Assert.Equal(1, data.ExitCode);
    }

    [Fact]
    public async Task SuppressNuGetAudit_Suppressed()
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile(
            nuGetPackages: [new NuGetReference("System.Net.Http", "4.3.3")],
            additionalProjectElements:
            [
                new XElement("ItemGroup",
                    new XElement("NuGetAuditSuppress",
                        new XAttribute("Include", "https://github.com/advisories/GHSA-7jgj-8wvc-jh57")))
            ],
            properties: [("NOWARN", "$(NOWARN);NU1510")]);

        project.AddFile("Program.cs", "Console.WriteLine();");

        var data = await project.BuildAndGetOutput(["--configuration", "Release"]);
        Assert.Equal(0, data.ExitCode);
    }

    [Fact]
    public async Task Pack_ContainsMetadata()
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile(
            sdk: SdkName,
            filename: "ANcpLua.Sample.csproj",
            properties: [("OutputType", "library")]
        );

        project.AddFile("Class1.cs", """
                                     namespace ANcpLua.Sample;
                                     public static class Class1
                                     {
                                     }
                                     """);

        await project.ExecuteGitCommand("init");
        await project.ExecuteGitCommand("add", ".");
        await project.ExecuteGitCommand("commit", "-m", "sample");
        await project.ExecuteGitCommand("remote", "add", "origin", "https://github.com/ancplua/sample.git");

        var data = await project.PackAndGetOutput(environmentVariables: [.. project.GitHubEnvironmentVariables]);
        Assert.True(data.ExitCode is 0, data.Output.ToString());

        // Validate nupkg
        var package = Directory.GetFiles(project.RootFolder, "*.nupkg", SearchOption.AllDirectories).Single();
        using var packageReader = new PackageArchiveReader(package);
        var nuspecReader = await packageReader.GetNuspecReaderAsync(TestContext.Current.CancellationToken);
        Assert.Equal(Author, nuspecReader.GetAuthors());
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
        await using var project = CreateProjectBuilder(SdkWebName);
        project.AddCsprojFile(rootSdk: "Microsoft.NET.Sdk.Web");

        project.AddFile("Program.cs", """
                                      using ANcpSdk.AspNetCore.ServiceDefaults;

                                      var builder = WebApplication.CreateBuilder();
                                      builder.UseANcpSdkConventions();
                                      """);

        var data = await project.BuildAndGetOutput(environmentVariables: [.. project.GitHubEnvironmentVariables]);
        Assert.Equal(0, data.ExitCode);
    }

    [Fact]
    public async Task Web_ServiceDefaultsIsRegisteredAutomatically()
    {
        await using var project = CreateProjectBuilder(SdkWebName);
        project.AddCsprojFile(rootSdk: "Microsoft.NET.Sdk.Web");

        project.AddFile("Program.cs", """
                                      using ANcpSdk.AspNetCore.ServiceDefaults;

                                      var builder = WebApplication.CreateBuilder();
                                      var app = builder.Build();
                                      return app.Services.GetService<ANcpSdkServiceDefaultsOptions>() is not null ? 0 : 1;
                                      """);

        var data = await project.RunAndGetOutput();
        Assert.Equal(0, data.ExitCode);
    }

    [Fact]
    public async Task Web_InterceptsAreGeneratedForBuildInvocation()
    {
        await using var project = CreateProjectBuilder(SdkWebName);
        project.AddCsprojFile(
            rootSdk: "Microsoft.NET.Sdk.Web",
            properties:
            [
                ("EmitCompilerGeneratedFiles", "true"),
                ("CompilerGeneratedFilesOutputPath", "obj/Generated")
            ]);

        project.AddFile("Program.cs", """
                                      using ANcpSdk.AspNetCore.ServiceDefaults;

                                      var builder = WebApplication.CreateBuilder();
                                      var app = builder.Build();
                                      return app.Services.GetService<ANcpSdkServiceDefaultsOptions>() is not null ? 0 : 1;
                                      """);

        var data = await project.BuildAndGetOutput(environmentVariables: [.. project.GitHubEnvironmentVariables]);
        Assert.Equal(0, data.ExitCode);

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
        await using var project = CreateProjectBuilder(SdkWebName);
        project.AddCsprojFile(
            rootSdk: "Microsoft.NET.Sdk.Web",
            properties: [("AutoRegisterServiceDefaults", "false")]);

        project.AddFile("Program.cs", """
                                      using ANcpSdk.AspNetCore.ServiceDefaults;

                                      var builder = WebApplication.CreateBuilder();
                                      var app = builder.Build();
                                      return app.Services.GetService<ANcpSdkServiceDefaultsOptions>() is not null ? 0 : 1;
                                      """);

        var data = await project.RunAndGetOutput();
        Assert.NotEqual(0, data.ExitCode);
    }

    [Theory]
    [InlineData(SdkName)]
    [InlineData(SdkTestName)]
    [InlineData(SdkWebName)]
    public async Task AssemblyContainsMetadataAttributeWithSdkName(string sdkName)
    {
        await using var project = CreateProjectBuilder(sdkName);
        project.AddCsprojFile(filename: "Sample.Tests.csproj");

        project.AddDirectoryBuildPropsFile("", sdkName: sdkName);

        project.AddFile("Program.cs", "Console.WriteLine();");

        var data = await project.BuildAndGetOutput();
        Assert.Equal(0, data.ExitCode);
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

                Assert.Equal(SdkMetadataKey, key);
                Assert.Equal(sdkName, value);
                return;
            }
        }

        Assert.Fail("Attribute not found");
    }

    [Theory]
    [InlineData("TargetFramework", "")]
    [InlineData("TargetFrameworks", "")]
    [InlineData("TargetFramework", "net8.0")]
    [InlineData("TargetFrameworks", "net8.0")]
    public async Task SetTargetFramework(string propName, string version)
    {
        await using var project = CreateProjectBuilder();
        project.AddCsprojFile(
            filename: "Sample.Tests.csproj",
            properties: [(propName, version)]);

        project.AddFile("Program.cs", "Console.WriteLine();");

        var data = await project.BuildAndGetOutput();
        Assert.True(data.ExitCode is 0, data.Output.ToString());
        var dllPath = Directory
            .GetFiles(project.RootFolder / "bin" / "Debug", "Sample.Tests.dll", SearchOption.AllDirectories).Single();

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
        await using var project = CreateProjectBuilder(SdkWebName);
        project.AddCsprojFile();

        project.AddFile("Program.cs", "Console.WriteLine();");
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

        var data = await project.BuildAndGetOutput();
        Assert.Equal(0, data.ExitCode);
        Assert.True(File.Exists(project.RootFolder / "package-lock.json"));
        Assert.True(File.Exists(project.RootFolder / "node_modules" / ".npm-install-stamp"));
        var files = data.GetBinLogFiles();
        Assert.Contains(files, f => f.EndsWith("package-lock.json", StringComparison.Ordinal));
    }

    [Fact]
    public async Task NpmRestore()
    {
        await using var project = CreateProjectBuilder(SdkWebName);
        project.AddCsprojFile();

        project.AddFile("Program.cs", "Console.WriteLine();");
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

        var data = await project.RestoreAndGetOutput();
        Assert.Equal(0, data.ExitCode);
        Assert.True(File.Exists(project.RootFolder / "package-lock.json"));
        Assert.True(File.Exists(project.RootFolder / "node_modules" / ".npm-install-stamp"));
    }

    [Fact]
    public async Task Npm_Dotnet_Build_sln()
    {
        await using var project = CreateProjectBuilder(SdkWebName);
        project.AddCsprojFile(filename: "sample.csproj");

        project.AddFile("Program.cs", "Console.WriteLine();");
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

        var data = await project.BuildAndGetOutput([slnFile]);
        Assert.Equal(0, data.ExitCode);
        Assert.True(File.Exists(project.RootFolder / "package-lock.json"));
        Assert.True(File.Exists(project.RootFolder / "node_modules" / ".npm-install-stamp"));
    }

    [Theory]
    [InlineData("build")]
    [InlineData("publish")]
    public async Task Npm_Dotnet_sln(string command)
    {
        await using var project = CreateProjectBuilder(SdkWebName);
        project.AddCsprojFile(filename: "sample.csproj");

        project.AddFile("Program.cs", "Console.WriteLine();");
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

        var data = await project.ExecuteDotnetCommandAndGetOutput(command, [slnFile]);
        Assert.Equal(0, data.ExitCode);

        Assert.True(File.Exists(project.RootFolder / "package-lock.json"));
        Assert.True(File.Exists(project.RootFolder / "node_modules" / ".npm-install-stamp"));
    }

    [Fact]
    public async Task NpmRestore_MultipleFiles()
    {
        await using var project = CreateProjectBuilder(SdkWebName);
        project.AddCsprojFile(
            additionalProjectElements:
            [
                new XElement("ItemGroup",
                    new XElement("NpmPackageFile", new XAttribute("Include", "a/package.json")),
                    new XElement("NpmPackageFile", new XAttribute("Include", "b/package.json")))
            ]);

        project.AddFile("Program.cs", "Console.WriteLine();");
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

        var data = await project.RestoreAndGetOutput();
        Assert.Equal(0, data.ExitCode);
        Assert.True(File.Exists(project.RootFolder / "a" / "package-lock.json"));
        Assert.True(File.Exists(project.RootFolder / "a" / "node_modules" / ".npm-install-stamp"));
        Assert.True(File.Exists(project.RootFolder / "b" / "package-lock.json"));
        Assert.True(File.Exists(project.RootFolder / "b" / "node_modules" / ".npm-install-stamp"));
    }

    [Fact]
    public async Task Npm_Dotnet_Build_RestoreLockedMode_Fail()
    {
        await using var project = CreateProjectBuilder(SdkWebName);
        project.AddCsprojFile();

        project.AddFile("Program.cs", "Console.WriteLine();");
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

        var data = await project.BuildAndGetOutput(["/p:RestoreLockedMode=true"]);
        Assert.Equal(1, data.ExitCode);
    }

    [Theory]
    [InlineData("/p:RestoreLockedMode=true")]
    [InlineData("/p:ContinuousIntegrationBuild=true")]
    public async Task Npm_Dotnet_Build_Ci_Success(string command)
    {
        await using var project = CreateProjectBuilder(SdkWebName);
        project.AddCsprojFile();

        project.AddFile("Program.cs", "Console.WriteLine();");
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

        var data = await project.BuildAndGetOutput([command]);
        Assert.Equal(0, data.ExitCode);
    }

    /// <summary>
    ///     Verifies debug info exists. SDK uses DebugType=portable with snupkg symbol packages.
    ///     For build output: portable PDB file should exist alongside DLL.
    ///     For pack output (nupkg extraction): PDB is in snupkg, not in main package.
    /// </summary>
    private static async Task AssertDebugInfoExists(string[] outputFiles, bool isPackOutput = false)
    {
        var dllPath = outputFiles.Single(f => f.EndsWith(".dll", StringComparison.OrdinalIgnoreCase));
        await using var stream = File.OpenRead(dllPath);
        var peReader = new PEReader(stream);
        var debug = peReader.ReadDebugDirectory();

        if (isPackOutput)
            // For pack output, PDB is in .snupkg (not in main .nupkg)
            // Just verify the DLL has debug directory entries pointing to portable PDB
            Assert.Contains(debug, entry => entry.Type == DebugDirectoryEntryType.CodeView);
        else
        {
            // For build output, portable PDB file should exist
            Assert.Contains(outputFiles, f => f.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(debug, entry => entry.Type == DebugDirectoryEntryType.CodeView);
        }
    }
}