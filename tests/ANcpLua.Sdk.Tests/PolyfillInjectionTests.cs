using System.Xml.Linq;
using ANcpLua.Sdk.Tests.Helpers;
using ANcpLua.Sdk.Tests.Infrastructure;

namespace ANcpLua.Sdk.Tests;

public class PolyfillInjectionTests(PackageFixture fixture, ITestOutputHelper testOutputHelper)
{
    private readonly PackageFixture _fixture = fixture;
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

    private ProjectBuilder CreateProjectBuilder() => new(_fixture, _testOutputHelper, SdkImportStyle.SdkElement, PackageFixture.SdkName);

    [Theory]
    [MemberData(nameof(PolyfillTestDataSource.InjectionMatrix), MemberType = typeof(PolyfillTestDataSource))]
    public async Task Polyfill_Injection_Status_Is_Correct(PolyfillDefinition polyfill, string tfm,
        bool shouldBePresent)
    {
        await using var project = CreateProjectBuilder();

        var properties = new[]
        {
            (polyfill.InjectionProperty, Val.True), (Prop.TargetFramework, tfm), (Prop.OutputType, Val.Library)
        };

        var dumpTarget = new XElement("Target",
            new XAttribute("Name", "DumpCompile"),
            new XAttribute("AfterTargets", "PrepareForBuild"),
            new XElement("Message",
                new XAttribute("Importance", "High"),
                new XAttribute("Text", "COMPILE_ITEM: %(Compile.Identity)")
            )
        );

        project.AddCsprojFile(
            properties,
            additionalProjectElements: (XElement[]?)[dumpTarget]
        );

        project.AddFile("Class1.cs", "public class Class1 {}");

        var result = await project.BuildAndGetOutput();

        Assert.True(result.ExitCode is 0,
            $"Build failed for {polyfill.InjectionProperty} on {tfm}. Output: {result.ProcessOutput}");

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
