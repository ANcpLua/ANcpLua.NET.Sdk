using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using ANcpLua.Sdk.Tests;
using ANcpLua.Sdk.Tests.Infrastructure;
using Xunit.Sdk;

[assembly: RegisterXunitSerializer(typeof(PolyfillDefinitionSerializer), typeof(PolyfillDefinition))]
[assembly: RegisterXunitSerializer(typeof(PolyfillScenarioSerializer), typeof(PolyfillScenario))]

namespace ANcpLua.Sdk.Tests.Infrastructure;

/// <summary>
///     xUnit v3 serializer for <see cref="PolyfillDefinition" />.
///     Enables Test Explorer to enumerate individual theory data rows.
/// </summary>
public sealed class PolyfillDefinitionSerializer : IXunitSerializer
{
    public bool IsSerializable(Type type, object? value, [NotNullWhen(false)] out string? failureReason)
    {
        failureReason = null;
        return type == typeof(PolyfillDefinition);
    }

    public string Serialize(object value)
    {
        var p = (PolyfillDefinition)value;
        return JsonSerializer.Serialize(new
        {
            p.InjectionProperty,
            p.RepositoryPath,
            p.MinimumTargetFramework,
            p.ActivationCode,
            p.ExpectedType,
            p.HasNegativeTest,
            p.RequiresLangVersionLatest,
            p.DisablesSharedThrowForNegative
        });
    }

    public object Deserialize(Type type, string serializedValue)
    {
        using var doc = JsonDocument.Parse(serializedValue);
        var root = doc.RootElement;
        return new PolyfillDefinition(
            root.GetProperty("InjectionProperty").GetString() ?? string.Empty,
            root.GetProperty("RepositoryPath").GetString() ?? string.Empty,
            root.GetProperty("MinimumTargetFramework").GetString() ?? string.Empty,
            root.GetProperty("ActivationCode").GetString() ?? string.Empty,
            root.GetProperty("ExpectedType").GetString() ?? string.Empty,
            root.GetProperty("HasNegativeTest").GetBoolean(),
            root.GetProperty("RequiresLangVersionLatest").GetBoolean(),
            root.GetProperty("DisablesSharedThrowForNegative").GetBoolean());
    }
}

/// <summary>
///     xUnit v3 serializer for <see cref="PolyfillScenario" />.
///     Enables Test Explorer to enumerate individual theory data rows.
/// </summary>
public sealed class PolyfillScenarioSerializer : IXunitSerializer
{
    public bool IsSerializable(Type type, object? value, [NotNullWhen(false)] out string? failureReason)
    {
        failureReason = null;
        return type == typeof(PolyfillScenario);
    }

    public string Serialize(object value)
    {
        var s = (PolyfillScenario)value;
        return JsonSerializer.Serialize(new
        {
            s.Name,
            s.PolyfillsToEnable,
            s.TestCode,
            s.AdditionalCode
        });
    }

    public object Deserialize(Type type, string serializedValue)
    {
        using var doc = JsonDocument.Parse(serializedValue);
        var root = doc.RootElement;
        return new PolyfillScenario(
            root.GetProperty("Name").GetString() ?? string.Empty,
            root.GetProperty("PolyfillsToEnable").EnumerateArray()
                .Select(e => e.GetString() ?? string.Empty).ToArray(),
            root.GetProperty("TestCode").GetString() ?? string.Empty,
            root.GetProperty("AdditionalCode").GetString() ?? string.Empty);
    }
}
