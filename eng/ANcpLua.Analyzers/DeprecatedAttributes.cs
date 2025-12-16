// Licensed under MIT.

using System;
using System.Collections.Generic;

namespace ANcpLua.Analyzers;

/// <summary>
/// Contains mappings of deprecated OpenTelemetry semantic convention attributes
/// to their modern replacements.
/// </summary>
public static class DeprecatedAttributes
{
    /// <summary>
    /// Dictionary of deprecated attribute names to their replacements.
    /// Key: deprecated name, Value: (replacement name, deprecation version)
    /// </summary>
    public static readonly IReadOnlyDictionary<string, (string Replacement, string Version)> Renames =
        new Dictionary<string, (string, string)>
        {
            // GenAI attributes (OTel 1.38)
            ["gen_ai.system"] = ("gen_ai.provider.name", "1.38.0"),
            ["gen_ai.usage.prompt_tokens"] = ("gen_ai.usage.input_tokens", "1.38.0"),
            ["gen_ai.usage.completion_tokens"] = ("gen_ai.usage.output_tokens", "1.38.0"),
            ["gen_ai.request.max_tokens"] = ("gen_ai.request.max_output_tokens", "1.38.0"),

            // HTTP attributes (v1.21.0)
            ["http.method"] = ("http.request.method", "1.21.0"),
            ["http.status_code"] = ("http.response.status_code", "1.21.0"),
            ["http.url"] = ("url.full", "1.21.0"),
            ["http.scheme"] = ("url.scheme", "1.21.0"),
            ["http.target"] = ("url.path", "1.21.0"),
            ["http.host"] = ("server.address", "1.21.0"),
            ["http.server_name"] = ("server.address", "1.21.0"),
            ["http.client_ip"] = ("client.address", "1.21.0"),
            ["http.user_agent"] = ("user_agent.original", "1.21.0"),
            ["http.request_content_length"] = ("http.request.body.size", "1.21.0"),
            ["http.response_content_length"] = ("http.response.body.size", "1.21.0"),

            // Network attributes (v1.21.0)
            ["net.host.name"] = ("server.address", "1.21.0"),
            ["net.host.port"] = ("server.port", "1.21.0"),
            ["net.peer.name"] = ("server.address", "1.21.0"),
            ["net.peer.port"] = ("server.port", "1.21.0"),
            ["net.sock.peer.addr"] = ("network.peer.address", "1.21.0"),
            ["net.sock.peer.port"] = ("network.peer.port", "1.21.0"),
            ["net.sock.host.addr"] = ("server.socket.address", "1.21.0"),
            ["net.sock.host.port"] = ("server.socket.port", "1.21.0"),
            ["net.protocol.name"] = ("network.protocol.name", "1.21.0"),
            ["net.protocol.version"] = ("network.protocol.version", "1.21.0"),

            // Database attributes (v1.24.0)
            ["db.statement"] = ("db.query.text", "1.24.0"),
            ["db.operation"] = ("db.operation.name", "1.24.0"),
            ["db.name"] = ("db.namespace", "1.24.0"),
            ["db.user"] = ("db.client.user", "1.24.0"),
            ["db.connection_string"] = ("db.connection.string", "1.24.0"),

            // Code attributes (v1.25.0)
            ["code.filepath"] = ("code.file.path", "1.25.0"),
            ["code.function"] = ("code.function.name", "1.25.0"),
            ["code.lineno"] = ("code.line.number", "1.25.0"),
            ["code.column"] = ("code.column.number", "1.25.0"),

            // FaaS attributes (v1.19.0)
            ["faas.execution"] = ("faas.invocation_id", "1.19.0"),
            ["faas.id"] = ("cloud.resource_id", "1.19.0"),

            // Messaging attributes (v1.21.0)
            ["messaging.kafka.client_id"] = ("messaging.client_id", "1.21.0"),
            ["messaging.rocketmq.client_id"] = ("messaging.client_id", "1.21.0"),
        };

    /// <summary>
    /// Known attribute key patterns used in OpenTelemetry APIs.
    /// </summary>
    public static readonly HashSet<string> AttributeKeyPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        "SetAttribute",
        "AddTag",
        "SetTag",
        "attributes",
        "Tags",
    };
}
