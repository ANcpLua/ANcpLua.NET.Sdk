using System.Text.Json.Serialization;

namespace ANcpLua.Sdk.Tests.Helpers;

internal sealed class SarifFileRun
{
    [JsonPropertyName("results")]
    public SarifFileRunResult[] Results { get; set; }
}
