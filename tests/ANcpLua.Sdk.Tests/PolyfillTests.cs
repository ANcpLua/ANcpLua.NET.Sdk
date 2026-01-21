using Xunit.Sdk;

namespace ANcpLua.Sdk.Tests;

/// <summary>
///     Consolidated polyfill tests - activation (positive/negative) and combination scenarios.
///     Uses PolyfillDefinition directly - no custom serializer needed.
/// </summary>
public class PolyfillTests(PackageFixture fixture)
{
    private readonly PackageFixture _fixture = fixture;

    #region Individual Polyfill Activation Tests

    public static TheoryData<PolyfillDefinition> AllPolyfills => PolyfillTestDataSource.ActivationMatrix();

    [Theory]
    [MemberData(nameof(AllPolyfills))]
    public async Task Polyfill_Activates_When_Enabled(PolyfillDefinition polyfill)
    {
        await using var project = SdkProjectBuilder.Create(_fixture);

        // Use Library since polyfills target netstandard2.0 which is library-only
        project
            .WithOutputType(Val.Library)
            .WithProperty(polyfill.InjectionProperty, Val.True)
            .WithTargetFramework(polyfill.MinimumTargetFramework);

        if (polyfill.RequiresLangVersionLatest)
            project.WithLangVersion(Val.Latest);

        var result = await project
            .AddSource("Smoke.cs", $$"""
                #nullable enable
                using System;
                namespace Consumer;
                internal class Smoke
                {
                    public void Run()
                    {
                        {{polyfill.ActivationCode}}
                    }
                }
                """)
            .BuildAsync();

        result.ShouldSucceed($"Build failed for {polyfill.InjectionProperty} on {polyfill.MinimumTargetFramework}");
    }

    [Theory]
    [MemberData(nameof(AllPolyfills))]
    public async Task Polyfill_Fails_When_Disabled(PolyfillDefinition polyfill)
    {
        if (!polyfill.HasNegativeTest)
            return;

        await using var project = SdkProjectBuilder.Create(_fixture);

        // Use Library since polyfills target netstandard2.0 which is library-only
        project
            .WithOutputType(Val.Library)
            .WithTargetFramework(polyfill.MinimumTargetFramework);

        if (polyfill.RequiresLangVersionLatest)
            project.WithLangVersion(Val.Latest);

        if (polyfill.DisablesSharedThrowForNegative)
            project.WithProperty(SdkProp.InjectSharedThrow, Val.False);

        var result = await project
            .AddSource("Smoke.cs", $$"""
                #nullable enable
                using System;
                namespace Consumer;
                internal class Smoke
                {
                    public void Run()
                    {
                        {{polyfill.ActivationCode}}
                    }
                }
                """)
            .BuildAsync();

        result.ShouldFail($"Build succeeded for {polyfill.InjectionProperty} when expected to fail without the flag");

        Assert.True(
            result.OutputContains("CS0246") ||
            result.OutputContains("CS0103") ||
            result.OutputContains("CS0234") ||
            result.OutputContains("CS0518") ||
            result.OutputContains("CS1513") ||
            result.OutputContains("CS1022"),
            $"Expected compilation error for missing type {polyfill.ExpectedType}. Output: {result.ProcessOutput}");
    }

    #endregion

    #region Polyfill Combination Tests

    public static TheoryData<PolyfillScenario> CombinationScenarios =>
    [
        new PolyfillScenario(
            "Language Features (required, init, Index)",
            [
                SdkProp.InjectRequiredMemberOnLegacy, SdkProp.InjectCompilerFeatureRequiredOnLegacy,
                SdkProp.InjectIsExternalInitOnLegacy, SdkProp.InjectIndexRangeOnLegacy
            ],
            """
            var arr = new[] { 1, 2, 3, 4, 5 };
            var last = arr[^1];
            var first = arr[0];
            var person = new Person { Name = "Test", Age = 25 };
            _ = person.Name;
            _ = last + first;
            """,
            """
            internal class Person
            {
                public required string Name { get; init; }
                public int Age { get; init; }
            }
            """),

        new PolyfillScenario(
            "Throw + TimeProvider",
            [SdkProp.InjectSharedThrow, SdkProp.InjectTimeProviderPolyfill],
            """
            var tp = Microsoft.Shared.Diagnostics.Throw.IfNull(TimeProvider.System);
            var now = tp.GetUtcNow();
            _ = now;
            """),

        new PolyfillScenario(
            "Realistic Service (Throw + Nullable + Caller + Init + UnreachableException)",
            [
                SdkProp.InjectSharedThrow, SdkProp.InjectNullabilityAttributesOnLegacy,
                SdkProp.InjectCallerAttributesOnLegacy, SdkProp.InjectIsExternalInitOnLegacy,
                SdkProp.InjectUnreachableExceptionOnLegacy
            ],
            """
            var options = new ServiceOptions { ConnectionString = "test", Timeout = 30 };
            var service = new RealisticService(options);
            _ = service.GetConnection();
            """,
            """
            internal record ServiceOptions
            {
                public string ConnectionString { get; init; } = "";
                public int Timeout { get; init; } = 30;
            }

            internal class RealisticService
            {
                private readonly ServiceOptions _options;

                public RealisticService(ServiceOptions options)
                {
                    _options = Microsoft.Shared.Diagnostics.Throw.IfNull(options);
                    Microsoft.Shared.Diagnostics.Throw.IfNullOrWhitespace(options.ConnectionString);
                    Microsoft.Shared.Diagnostics.Throw.IfLessThan(options.Timeout, 1);
                }

                [return: System.Diagnostics.CodeAnalysis.NotNull]
                public string GetConnection()
                {
                    return _options.ConnectionString ?? throw new System.Diagnostics.UnreachableException();
                }
            }
            """),

        new PolyfillScenario(
            "Nullable + CallerExpression",
            [SdkProp.InjectNullabilityAttributesOnLegacy, SdkProp.InjectCallerAttributesOnLegacy],
            """
            string? input = "test";
            Validator.ThrowIfNull(input);
            System.Console.WriteLine(input.Length);
            """,
            """
            internal static class Validator
            {
                public static void ThrowIfNull(
                    [System.Diagnostics.CodeAnalysis.NotNull] object? value,
                    [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(value))] string? paramName = null)
                {
                    if (value is null)
                        throw new System.ArgumentNullException(paramName);
                }
            }
            """)
    ];

    [Theory]
    [MemberData(nameof(CombinationScenarios))]
    public async Task Polyfill_Combinations_Build_Successfully(PolyfillScenario scenario)
    {
        await using var project = SdkProjectBuilder.Create(_fixture);

        // Use Library since polyfills target netstandard2.0 which is library-only
        project
            .WithOutputType(Val.Library)
            .WithTargetFramework(Tfm.NetStandard20)
            .WithLangVersion(Val.Latest);

        foreach (var polyfillProp in scenario.PolyfillsToEnable)
            project.WithProperty(polyfillProp, Val.True);

        var code = $$"""
            #nullable enable
            using System;
            namespace Consumer;

            {{scenario.AdditionalCode}}

            internal class Smoke
            {
                public void Run()
                {
                    {{scenario.TestCode}}
                }
            }
            """;

        var result = await project
            .AddSource("Smoke.cs", code)
            .BuildAsync();

        result.ShouldSucceed($"Combination scenario '{scenario.Name}' failed");
    }

    #endregion
}

/// <summary>
///     Defines a polyfill combination test scenario.
///     Implements IXunitSerializable for Test Explorer enumeration.
/// </summary>
public sealed class PolyfillScenario : IXunitSerializable
{
    public string Name { get; private set; } = "";
    public string[] PolyfillsToEnable { get; private set; } = [];
    public string TestCode { get; private set; } = "";
    public string AdditionalCode { get; private set; } = "";

    /// <summary>Required for xUnit deserialization.</summary>
    public PolyfillScenario() { }

    public PolyfillScenario(string name, string[] polyfillsToEnable, string testCode, string additionalCode = "")
    {
        Name = name;
        PolyfillsToEnable = polyfillsToEnable;
        TestCode = testCode;
        AdditionalCode = additionalCode;
    }

    void IXunitSerializable.Deserialize(IXunitSerializationInfo info)
    {
        Name = (string)info.GetValue(nameof(Name))!;
        PolyfillsToEnable = (string[])info.GetValue(nameof(PolyfillsToEnable))!;
        TestCode = (string)info.GetValue(nameof(TestCode))!;
        AdditionalCode = (string)info.GetValue(nameof(AdditionalCode))!;
    }

    void IXunitSerializable.Serialize(IXunitSerializationInfo info)
    {
        info.AddValue(nameof(Name), Name, typeof(string));
        info.AddValue(nameof(PolyfillsToEnable), PolyfillsToEnable, typeof(string[]));
        info.AddValue(nameof(TestCode), TestCode, typeof(string));
        info.AddValue(nameof(AdditionalCode), AdditionalCode, typeof(string));
    }

    public override string ToString() => Name;
}
