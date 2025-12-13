using System.Text.Json.Serialization;

namespace ANcpLua.Sdk.Tests.Helpers;

internal sealed class SarifFileRunResultMessage
{
    [JsonPropertyName("text")]
    public string Text { get; set; }

    public override string ToString()
    {
        return Text;
    }
}