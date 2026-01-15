// Copyright (c) ANcpLua. All rights reserved.

using ANcpLua.Sdk.Tests.Helpers;
using ANcpLua.Sdk.Tests.Infrastructure;

namespace ANcpLua.Sdk.Tests;

/// <summary>
///     Consolidated polyfill tests - activation (positive/negative) and combination scenarios.
///     Uses PolyfillDefinition directly - no custom serializer needed.
/// </summary>
public class PolyfillTests(PackageFixture fixture, ITestOutputHelper testOutputHelper)
{
    private readonly PackageFixture _fixture = fixture;
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

    private ProjectBuilder CreateProjectBuilder() => new(_fixture, _testOutputHelper, SdkImportStyle.SdkElement, PackageFixture.SdkName);

    #region Individual Polyfill Activation Tests

    public static TheoryData<PolyfillDefinition> AllPolyfills => PolyfillTestDataSource.ActivationMatrix();

    [Theory]
    [MemberData(nameof(AllPolyfills))]
    public async Task Polyfill_Activates_When_Enabled(PolyfillDefinition polyfill)
    {
        await using var project = CreateProjectBuilder();

        var properties = new List<(string, string)>
        {
            (polyfill.InjectionProperty, Val.True),
            (Prop.TargetFramework, polyfill.MinimumTargetFramework),
            (Prop.OutputType, Val.Library)
        };

        if (polyfill.RequiresLangVersionLatest)
            properties.Add((Prop.LangVersion, Val.Latest));

        project.AddCsprojFile(properties.ToArray());
        project.AddFile("Smoke.cs", $$"""
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
                                      """);

        var result = await project.BuildAndGetOutput();
        result.ShouldSucceed($"Build failed for {polyfill.InjectionProperty} on {polyfill.MinimumTargetFramework}");
    }

    [Theory]
    [MemberData(nameof(AllPolyfills))]
    public async Task Polyfill_Fails_When_Disabled(PolyfillDefinition polyfill)
    {
        if (!polyfill.HasNegativeTest)
            return;

        await using var project = CreateProjectBuilder();

        var properties = new List<(string, string)>
        {
            (Prop.TargetFramework, polyfill.MinimumTargetFramework),
            (Prop.OutputType, Val.Library)
        };

        if (polyfill.RequiresLangVersionLatest)
            properties.Add((Prop.LangVersion, Val.Latest));

        // InjectSharedThrow defaults to true, which auto-enables CallerAttributes and NullabilityAttributes.
        // For negative tests of these polyfills, we must explicitly disable InjectSharedThrow.
        if (polyfill.DisablesSharedThrowForNegative)
            properties.Add((Prop.InjectSharedThrow, Val.False));

        project.AddCsprojFile(properties.ToArray());
        project.AddFile("Smoke.cs", $$"""
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
                                      """);

        var result = await project.BuildAndGetOutput();
        result.ShouldFail($"Build succeeded for {polyfill.InjectionProperty} when expected to fail without the flag");

        Assert.True(
            result.OutputContains("CS0246") || // Type or namespace not found
            result.OutputContains("CS0103") || // Name does not exist
            result.OutputContains("CS0234") || // Type or namespace does not exist in namespace
            result.OutputContains("CS0518") || // Predefined type not defined
            result.OutputContains("CS1513") || // } expected
            result.OutputContains("CS1022"), // Type or namespace definition expected
            $"Expected compilation error for missing type {polyfill.ExpectedType}. Output: {result.ProcessOutput}");
    }

    #endregion

    #region Polyfill Combination Tests

    public static TheoryData<PolyfillScenario> CombinationScenarios =>
    [
        new PolyfillScenario(
            "Language Features (required, init, Index)",
            [
                Prop.InjectRequiredMemberOnLegacy, Prop.InjectCompilerFeatureRequiredOnLegacy,
                Prop.InjectIsExternalInitOnLegacy, Prop.InjectIndexRangeOnLegacy
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
            [Prop.InjectSharedThrow, Prop.InjectTimeProviderPolyfill],
            """
            var tp = Microsoft.Shared.Diagnostics.Throw.IfNull(TimeProvider.System);
            var now = tp.GetUtcNow();
            _ = now;
            """),

        new PolyfillScenario(
            "Realistic Service (Throw + Nullable + Caller + Init + UnreachableException)",
            [
                Prop.InjectSharedThrow, Prop.InjectNullabilityAttributesOnLegacy,
                Prop.InjectCallerAttributesOnLegacy, Prop.InjectIsExternalInitOnLegacy,
                Prop.InjectUnreachableExceptionOnLegacy
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
            [Prop.InjectNullabilityAttributesOnLegacy, Prop.InjectCallerAttributesOnLegacy],
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
        await using var project = CreateProjectBuilder();

        var properties = scenario.PolyfillsToEnable
            .Select(p => (p, Val.True))
            .Prepend((Prop.LangVersion, Val.Latest))
            .Prepend((Prop.OutputType, Val.Library))
            .Prepend((Prop.TargetFramework, Tfm.NetStandard20))
            .ToArray();

        project.AddCsprojFile(properties);

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

        project.AddFile("Smoke.cs", code);

        var result = await project.BuildAndGetOutput();
        result.ShouldSucceed($"Combination scenario '{scenario.Name}' failed");
    }

    #endregion
}

/// <summary>
///     Defines a polyfill combination test scenario.
///     xUnit v3 handles records natively - no serializer needed.
/// </summary>
public sealed record PolyfillScenario(
    string Name,
    string[] PolyfillsToEnable,
    string TestCode,
    string AdditionalCode = "")
{
    public override string ToString() => Name;
}
