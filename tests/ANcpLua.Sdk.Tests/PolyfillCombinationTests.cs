// Copyright (c) ANcpLua. All rights reserved.

using ANcpLua.Sdk.Tests.Helpers;
using ANcpLua.Sdk.Tests.Infrastructure;

namespace ANcpLua.Sdk.Tests;

/// <summary>
///     Tests that multiple polyfills work together in combination.
///     Uses the SDK test infrastructure to build actual projects.
/// </summary>
public class PolyfillCombinationTests(PackageFixture fixture, ITestOutputHelper testOutputHelper)
{
    /// <summary>
    ///     Tests that all language feature polyfills work together (required, init, Index).
    ///     Note: Range slicing (arr[1..3]) requires GetSubArray which isn't available on netstandard2.0
    /// </summary>
    [Fact]
    public async Task LanguageFeaturePolyfills_AllTogether_BuildsSuccessfully()
    {
        await using var project =
            new ProjectBuilder(fixture, testOutputHelper, SdkImportStyle.SdkElement, PackageFixture.SdkName);

        project.AddCsprojFile([
            (MsBuildProperties.TargetFramework, TargetFrameworks.NetStandard20),
            (MsBuildProperties.OutputType, MsBuildValues.Library),
            (MsBuildProperties.LangVersion, MsBuildValues.Latest),
            ("InjectRequiredMemberOnLegacy", MsBuildValues.True),
            ("InjectCompilerFeatureRequiredOnLegacy", MsBuildValues.True),
            ("InjectIsExternalInitOnLegacy", MsBuildValues.True),
            ("InjectIndexRangeOnLegacy", MsBuildValues.True)
        ]);

        project.AddFile("ModernFeatures.cs", """
                                             #nullable enable
                                             using System;

                                             namespace Consumer;

                                             internal class Person
                                             {
                                                 public required string Name { get; init; }
                                                 public int Age { get; init; }
                                             }

                                             internal class ArrayProcessor
                                             {
                                                 public void Process()
                                                 {
                                                     var arr = new[] { 1, 2, 3, 4, 5 };
                                                     var last = arr[^1];           // Index from end
                                                     var first = arr[0];           // Normal index
                                                     
                                                     // Range slicing not supported on netstandard2.0 (needs GetSubArray)
                                                     // Use Span slicing instead in real code
                                                     
                                                     var person = new Person { Name = "Test", Age = 25 };
                                                     _ = person.Name;
                                                     _ = last + first;
                                                 }
                                             }
                                             """);

        var result = await project.BuildAndGetOutput();
        Assert.True(result.ExitCode is 0,
            $"Build failed when combining language feature polyfills. Output: {result.ProcessOutput}");
    }

    /// <summary>
    ///     Tests that Throw helper + TimeProvider polyfills work together.
    /// </summary>
    [Fact]
    public async Task ThrowAndTimeProvider_Together_BuildsSuccessfully()
    {
        await using var project =
            new ProjectBuilder(fixture, testOutputHelper, SdkImportStyle.SdkElement, PackageFixture.SdkName);

        project.AddCsprojFile([
            (MsBuildProperties.TargetFramework, TargetFrameworks.NetStandard20),
            (MsBuildProperties.OutputType, MsBuildValues.Library),
            (MsBuildProperties.LangVersion, MsBuildValues.Latest),
            ("InjectSharedThrow", MsBuildValues.True),
            ("InjectTimeProviderPolyfill", MsBuildValues.True)
        ]);

        project.AddFile("ServiceClass.cs", """
                                           #nullable enable
                                           using System;
                                           using Microsoft.Shared.Diagnostics;

                                           namespace Consumer;

                                           internal class ServiceClass
                                           {
                                               private readonly TimeProvider _timeProvider;

                                               public ServiceClass(TimeProvider timeProvider)
                                               {
                                                   _timeProvider = Throw.IfNull(timeProvider);
                                               }

                                               public DateTimeOffset GetCurrentTime()
                                               {
                                                   return _timeProvider.GetUtcNow();
                                               }
                                           }
                                           """);

        var result = await project.BuildAndGetOutput();
        Assert.True(result.ExitCode is 0,
            $"Build failed when combining Throw + TimeProvider polyfills. Output: {result.ProcessOutput}");
    }

    /// <summary>
    ///     Tests comprehensive polyfill combination for a realistic service class.
    /// </summary>
    [Fact]
    public async Task AllCommonPolyfills_RealisticService_BuildsSuccessfully()
    {
        await using var project =
            new ProjectBuilder(fixture, testOutputHelper, SdkImportStyle.SdkElement, PackageFixture.SdkName);

        project.AddCsprojFile([
            (MsBuildProperties.TargetFramework, TargetFrameworks.NetStandard20),
            (MsBuildProperties.OutputType, MsBuildValues.Library),
            (MsBuildProperties.LangVersion, MsBuildValues.Latest),
            ("InjectSharedThrow", MsBuildValues.True),
            ("InjectNullabilityAttributesOnLegacy", MsBuildValues.True),
            ("InjectCallerAttributesOnLegacy", MsBuildValues.True),
            ("InjectIsExternalInitOnLegacy", MsBuildValues.True),
            ("InjectUnreachableExceptionOnLegacy", MsBuildValues.True)
        ]);

        project.AddFile("RealisticService.cs", """
                                               #nullable enable
                                               using System;
                                               using System.Diagnostics;
                                               using System.Diagnostics.CodeAnalysis;
                                               using Microsoft.Shared.Diagnostics;

                                               namespace Consumer;

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
                                                       _options = Throw.IfNull(options);
                                                       Throw.IfNullOrWhitespace(options.ConnectionString);
                                                       Throw.IfLessThan(options.Timeout, 1);
                                                   }

                                                   [return: NotNull]
                                                   public string GetConnection()
                                                   {
                                                       return _options.ConnectionString ?? throw new UnreachableException();
                                                   }
                                               }
                                               """);

        var result = await project.BuildAndGetOutput();
        Assert.True(result.ExitCode is 0,
            $"Build failed for realistic service with common polyfills. Output: {result.ProcessOutput}");
    }

    /// <summary>
    ///     Tests nullable + caller expression polyfills together.
    /// </summary>
    [Fact]
    public async Task NullableAndCallerExpression_Together_BuildsSuccessfully()
    {
        await using var project =
            new ProjectBuilder(fixture, testOutputHelper, SdkImportStyle.SdkElement, PackageFixture.SdkName);

        project.AddCsprojFile([
            (MsBuildProperties.TargetFramework, TargetFrameworks.NetStandard20),
            (MsBuildProperties.OutputType, MsBuildValues.Library),
            (MsBuildProperties.LangVersion, MsBuildValues.Latest),
            ("InjectNullabilityAttributesOnLegacy", MsBuildValues.True),
            ("InjectCallerAttributesOnLegacy", MsBuildValues.True)
        ]);

        project.AddFile("Validator.cs", """
                                        #nullable enable
                                        using System;
                                        using System.Diagnostics.CodeAnalysis;
                                        using System.Runtime.CompilerServices;

                                        namespace Consumer;

                                        internal static class Validator
                                        {
                                            public static void ThrowIfNull(
                                                [NotNull] object? value,
                                                [CallerArgumentExpression(nameof(value))] string? paramName = null)
                                            {
                                                if (value is null)
                                                    throw new ArgumentNullException(paramName);
                                            }
                                        }

                                        internal class Consumer
                                        {
                                            public void Use(string? input)
                                            {
                                                Validator.ThrowIfNull(input);
                                                Console.WriteLine(input.Length); // No warning - NotNull works
                                            }
                                        }
                                        """);

        var result = await project.BuildAndGetOutput();
        Assert.True(result.ExitCode is 0,
            $"Build failed when combining Nullable + CallerExpression polyfills. Output: {result.ProcessOutput}");
    }
}
