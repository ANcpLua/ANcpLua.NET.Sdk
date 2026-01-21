using System.Xml.Linq;

namespace ANcpLua.Sdk.Tests;

public class PolyfillInjectionTests(PackageFixture fixture)
{
    private readonly PackageFixture _fixture = fixture;

    [Theory]
    [MemberData(nameof(PolyfillTestDataSource.InjectionMatrix), MemberType = typeof(PolyfillTestDataSource))]
    public async Task Polyfill_Injection_Status_Is_Correct(PolyfillDefinition polyfill, string tfm,
        bool shouldBePresent)
    {
        await using var project = SdkProjectBuilder.Create(_fixture);

        var dumpTarget = new XElement("Target",
            new XAttribute("Name", "DumpCompile"),
            new XAttribute("AfterTargets", "PrepareForBuild"),
            new XElement("Message",
                new XAttribute("Importance", "High"),
                new XAttribute("Text", "COMPILE_ITEM: %(Compile.Identity)")
            )
        );

        var result = await project
            .WithProperty(polyfill.InjectionProperty, Val.True)
            .WithTargetFramework(tfm)
            .WithOutputType(Val.Library) // Use Library - we just need build to succeed, not run
            .WithAdditionalProjectElement(dumpTarget)
            .AddSource("Class1.cs", "public class Class1 {}")
            .BuildAsync();

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
