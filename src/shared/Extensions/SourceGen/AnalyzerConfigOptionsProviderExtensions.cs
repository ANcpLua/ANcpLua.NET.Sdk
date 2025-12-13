// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Source: https://github.com/Sergio0694/PolySharp

#if ANCPLUA_SOURCEGEN_HELPERS

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ANcpLua.SourceGen;

/// <summary>
/// Extension methods for the <see cref="AnalyzerConfigOptionsProvider"/> type.
/// </summary>
internal static class AnalyzerConfigOptionsProviderExtensions
{
    /// <summary>
    /// Checks whether the input property has a valid <see cref="bool"/> value.
    /// </summary>
    /// <param name="options">The input <see cref="AnalyzerConfigOptionsProvider"/> instance.</param>
    /// <param name="propertyName">The MSBuild property name.</param>
    /// <param name="propertyValue">The resulting property value, if invalid.</param>
    /// <returns>Whether the target property is a valid <see cref="bool"/> value.</returns>
    public static bool IsValidMSBuildProperty(
        this AnalyzerConfigOptionsProvider options,
        string propertyName,
        [NotNullWhen(false)] out string? propertyValue)
    {
        return
            !options.GlobalOptions.TryGetValue($"build_property.{propertyName}", out propertyValue) ||
            string.Equals(propertyValue, string.Empty, StringComparison.Ordinal) ||
            string.Equals(propertyValue, bool.TrueString, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(propertyValue, bool.FalseString, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the value of a <see cref="bool"/> MSBuild property.
    /// </summary>
    /// <param name="options">The input <see cref="AnalyzerConfigOptionsProvider"/> instance.</param>
    /// <param name="propertyName">The MSBuild property name.</param>
    /// <returns>The value of the specified MSBuild property.</returns>
    public static bool GetBoolMSBuildProperty(this AnalyzerConfigOptionsProvider options, string propertyName)
    {
        return
            options.GlobalOptions.TryGetValue($"build_property.{propertyName}", out string? propertyValue) &&
            string.Equals(propertyValue, bool.TrueString, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the value of an MSBuild property representing a semicolon-separated list of strings.
    /// </summary>
    /// <param name="options">The input <see cref="AnalyzerConfigOptionsProvider"/> instance.</param>
    /// <param name="propertyName">The MSBuild property name.</param>
    /// <returns>The value of the specified MSBuild property.</returns>
    public static ImmutableArray<string> GetStringArrayMSBuildProperty(
        this AnalyzerConfigOptionsProvider options,
        string propertyName)
    {
        if (options.GlobalOptions.TryGetValue($"build_property.{propertyName}", out string? propertyValue))
        {
            var builder = ImmutableArray.CreateBuilder<string>();

            foreach (string part in propertyValue.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries))
            {
                string trimmed = part.Trim();
                if (trimmed.Length > 0)
                {
                    builder.Add(trimmed);
                }
            }

            return builder.ToImmutable();
        }

        return [];
    }
}

#endif
