using System.Xml.Linq;
using ANcpLua.Sdk.Tests.Helpers;
using ANcpLua.Sdk.Tests.Infrastructure;

namespace ANcpLua.Sdk.Tests;

public class PolyfillInjectionTests(PackageFixture fixture, ITestOutputHelper testOutputHelper)
{
    private ProjectBuilder CreateProjectBuilder()
    {
        return new ProjectBuilder(fixture, testOutputHelper, SdkImportStyle.SdkElement, PackageFixture.SdkName);
    }

    [Theory]
    [MemberData(nameof(PolyfillTestDataSource.InjectionMatrix), MemberType = typeof(PolyfillTestDataSource))]
    public async Task Polyfill_Injection_Status_Is_Correct(PolyfillDefinition polyfill, string tfm,
        bool shouldBePresent)
    {
        await using var project = CreateProjectBuilder();

        // Enable the polyfill property
        var properties = new[]
        {
            (polyfill.InjectionProperty, MsBuildValues.True),
            (MsBuildProperties.TargetFramework, tfm),
            (MsBuildProperties.OutputType, MsBuildValues.Library)
        };

        // Inject a custom target to dump Compile items
        var dumpTarget = new XElement("Target",
            new XAttribute("Name", "DumpCompile"),
            new XAttribute("AfterTargets", "PrepareForBuild"),
            new XElement("Message",
                new XAttribute("Importance", "High"),
                new XAttribute("Text", "COMPILE_ITEM: %(Compile.Identity)") // Simple prefix for easy searching
            )
        );

        project.AddCsprojFile(
            properties,
            additionalProjectElements: [dumpTarget]
        );

        project.AddFile("Class1.cs", "public class Class1 {}");

        // Run the build (which triggers PrepareForBuild and our target)
        var result = await project.BuildAndGetOutput();

        // Verify compilation success (sanity check)
        Assert.True(result.ExitCode is 0,
            $"Build failed for {polyfill.InjectionProperty} on {tfm}. Output: {result.ProcessOutput}");

        // Check if the expected file is in the output
        var expectedFileName = Path.GetFileName(polyfill.RepositoryPath);
        var isPresent = result.ProcessOutput.Any(line =>
            line.Text.Contains("COMPILE_ITEM:") && line.Text.Contains(expectedFileName));

        if (shouldBePresent)
            Assert.True(isPresent,
                $"Expected {expectedFileName} to be injected for {polyfill.InjectionProperty} on {tfm}, but it was NOT found.");
        else
            Assert.False(isPresent,
                $"Expected {expectedFileName} NOT to be injected for {polyfill.InjectionProperty} on {tfm}, but it WAS found.");
    }
}