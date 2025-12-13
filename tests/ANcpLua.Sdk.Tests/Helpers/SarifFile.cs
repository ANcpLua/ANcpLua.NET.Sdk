using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace ANcpLua.Sdk.Tests.Helpers;

internal sealed class SarifFile
{
    [JsonPropertyName("runs")]
    public SarifFileRun[] Runs { get; set; }

    public IEnumerable<SarifFileRunResult> AllResults() => Runs.SelectMany(r => r.Results);
}
