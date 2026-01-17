using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using Meziantou.Framework;
using YamlDotNet.RepresentationModel;

namespace SchemaGenerator;

/// <summary>
///     Generates C# types and DuckDB schema from TypeSpec-compiled OpenAPI.
///     Uses YamlDotNet for direct YAML parsing (supports OpenAPI 3.1.0).
/// </summary>
public static class SchemaGeneratorTool
{
    public static void Generate(FullPath openApiPath, FullPath protocolDir, FullPath collectorDir, GenerationGuard guard)
    {
        var schema = OpenApiSchema.Load(openApiPath);
        Console.WriteLine($"Loaded schema: {schema.Title} v{schema.Version} ({schema.Schemas.Length} definitions)");

        var sourcePath = openApiPath.Value;

        // C# Scalars (Qyl.Common namespace)
        var scalars = schema.Schemas.Where(static s => s.IsScalar).ToList();
        if (scalars.Count > 0)
            guard.WriteFile(protocolDir / "Primitives" / "Scalars.g.cs", GenerateScalars(scalars, sourcePath));

        // C# Enums (Qyl.Enums namespace)
        var enums = schema.Schemas.Where(static s => s.IsEnum).ToList();
        if (enums.Count > 0)
            guard.WriteFile(protocolDir / "Enums" / "Enums.g.cs", GenerateEnums(enums, sourcePath));

        // C# Models (grouped by namespace)
        var models = schema.Schemas.Where(static s => s is { IsScalar: false, IsEnum: false, Type: "object" }).ToList();
        foreach (var group in models.GroupBy(static m => GetCSharpNamespace(m.Name)))
        {
            var fileName = GetFileNameFromNamespace(group.Key);
            guard.WriteFile(protocolDir / "Models" / $"{fileName}.g.cs", GenerateModels(group.Key, group.ToList(), sourcePath));
        }

        // DuckDB Schema
        var tables = schema.Schemas.Where(static s => s.Extensions.ContainsKey("x-duckdb-table")).ToList();
        if (tables.Count > 0)
            guard.WriteFile(collectorDir / "Storage" / "DuckDbSchema.g.cs", GenerateDuckDb(tables, schema, sourcePath));
    }

    // ════════════════════════════════════════════════════════════════════════════
    // C# SCALARS
    // ════════════════════════════════════════════════════════════════════════════

    static string GenerateScalars(IEnumerable<SchemaDefinition> scalars, string sourcePath)
    {
        var sb = new StringBuilder();
        AppendHeader(sb, sourcePath, "Strongly-typed scalar primitives");

        foreach (var group in scalars.GroupBy(static s => GetCSharpNamespace(s.Name)).OrderBy(static g => g.Key))
        {
            sb.AppendLine($"namespace {group.Key};");
            sb.AppendLine();

            foreach (var scalar in group.OrderBy(static s => s.GetTypeName()))
            {
                var typeName = EscapeKeyword(scalar.GetTypeName());
                var (underlying, jsonRead, jsonWrite) = GetScalarTypeInfo(scalar.Type, scalar.Format);
                var isHex = typeName is "TraceId" or "SpanId";
                var hexLen = typeName switch { "TraceId" => 32, "SpanId" => 16, _ => 0 };

                AppendXmlDoc(sb, scalar.Description, "");

                sb.AppendLine($"[System.Text.Json.Serialization.JsonConverter(typeof({typeName}JsonConverter))]");
                if (isHex)
                    sb.AppendLine($"public readonly partial record struct {typeName}({underlying} Value) : System.IParsable<{typeName}>, System.ISpanFormattable");
                else
                    sb.AppendLine($"public readonly partial record struct {typeName}({underlying} Value)");

                sb.AppendLine("{");

                // Implicit conversions
                sb.AppendLine($"    public static implicit operator {underlying}({typeName} v) => v.Value;");
                sb.AppendLine($"    public static implicit operator {typeName}({underlying} v) => new(v);");
                sb.AppendLine($"    public override string ToString() => {(underlying == "string" ? "Value ?? string.Empty" : "Value.ToString()")};");

                // Validation
                if (scalar.Pattern is not null)
                {
                    sb.AppendLine($"    private static readonly System.Text.RegularExpressions.Regex s_pattern = new(@\"{scalar.Pattern.Replace("\"", "\"\"")}\", System.Text.RegularExpressions.RegexOptions.Compiled);");
                    sb.AppendLine("    public bool IsValid => !string.IsNullOrEmpty(Value) && s_pattern.IsMatch(Value);");
                }
                else if (underlying == "string")
                    sb.AppendLine("    public bool IsValid => !string.IsNullOrEmpty(Value);");
                else
                    sb.AppendLine("    public bool IsValid => true;");

                if (isHex) AppendHexParsing(sb, typeName, hexLen);

                sb.AppendLine("}");
                sb.AppendLine();

                // JSON Converter
                sb.AppendLine($"file sealed class {typeName}JsonConverter : System.Text.Json.Serialization.JsonConverter<{typeName}>");
                sb.AppendLine("{");
                sb.AppendLine($"    public override {typeName} Read(ref System.Text.Json.Utf8JsonReader reader, System.Type typeToConvert, System.Text.Json.JsonSerializerOptions options) => new({jsonRead});");
                sb.AppendLine($"    public override void Write(System.Text.Json.Utf8JsonWriter writer, {typeName} value, System.Text.Json.JsonSerializerOptions options) => {jsonWrite};");
                sb.AppendLine("}");
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    static void AppendHexParsing(StringBuilder sb, string typeName, int len)
    {
        var zeros = new string('0', len);
        sb.AppendLine();
        sb.AppendLine($"    public static {typeName} Empty => new(\"{zeros}\");");
        sb.AppendLine($"    public bool IsEmpty => string.IsNullOrEmpty(Value) || Value == \"{zeros}\";");
        sb.AppendLine();

        sb.AppendLine($"    public static {typeName} Parse(string s, IFormatProvider? provider) => TryParse(s, provider, out var r) ? r : throw new FormatException($\"Invalid {typeName}: {{s}}\");");
        sb.AppendLine($"    public static bool TryParse(string? s, IFormatProvider? provider, out {typeName} result)");
        sb.AppendLine("    {");
        sb.AppendLine($"        if (s is {{ Length: {len} }} && IsValidHex(s.AsSpan())) {{ result = new(s); return true; }}");
        sb.AppendLine("        result = default; return false;");
        sb.AppendLine("    }");

        sb.AppendLine("    public bool TryFormat(Span<char> dest, out int written, ReadOnlySpan<char> format, IFormatProvider? provider)");
        sb.AppendLine("    {");
        sb.AppendLine($"        if (dest.Length < {len} || string.IsNullOrEmpty(Value)) {{ written = 0; return false; }}");
        sb.AppendLine($"        Value.AsSpan().CopyTo(dest); written = {len}; return true;");
        sb.AppendLine("    }");
        sb.AppendLine("    string IFormattable.ToString(string? format, IFormatProvider? provider) => Value ?? string.Empty;");
        sb.AppendLine("    static bool IsValidHex(ReadOnlySpan<char> s) { foreach (var c in s) if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'))) return false; return true; }");
    }

    // ════════════════════════════════════════════════════════════════════════════
    // C# ENUMS
    // ════════════════════════════════════════════════════════════════════════════

    static string GenerateEnums(IEnumerable<SchemaDefinition> enums, string sourcePath)
    {
        var sb = new StringBuilder();
        AppendHeader(sb, sourcePath, "Enumeration types");

        sb.AppendLine("namespace Qyl.Enums");
        sb.AppendLine("{");

        var deduped = enums
            .GroupBy(static e => e.GetTypeName())
            .Select(static g => g.FirstOrDefault(e => e.Name.StartsWith("Enums.", StringComparison.Ordinal)) ?? g.First())
            .OrderBy(static e => e.GetTypeName());

        foreach (var enumDef in deduped)
        {
            var typeName = EscapeKeyword(enumDef.GetTypeName());
            var isIntegerEnum = enumDef.Type == "integer";
            var enumVarNames = enumDef.GetEnumVarNames();

            AppendXmlDoc(sb, enumDef.Description, "    ");

            if (isIntegerEnum)
                sb.AppendLine($"    [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonNumberEnumConverter<{typeName}>))]");
            else
                sb.AppendLine($"    [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter<{typeName}>))]");

            sb.AppendLine($"    public enum {typeName}");
            sb.AppendLine("    {");

            for (var i = 0; i < enumDef.EnumValues.Length; i++)
            {
                var rawValue = enumDef.EnumValues[i];
                string memberName = i < enumVarNames.Length
                    ? enumVarNames[i]
                    : isIntegerEnum
                        ? InferEnumMemberName(typeName, rawValue)
                        : ToPascalCase(rawValue);
                memberName = EscapeKeyword(memberName);

                if (isIntegerEnum)
                    sb.AppendLine($"        {memberName} = {rawValue},");
                else
                {
                    sb.AppendLine($"        [System.Runtime.Serialization.EnumMember(Value = \"{rawValue}\")]");
                    sb.AppendLine($"        {memberName} = {i},");
                }
            }

            sb.AppendLine("    }");
            sb.AppendLine();
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    static string InferEnumMemberName(string enumTypeName, string value) => enumTypeName switch
    {
        "SpanKind" => value switch
        {
            "0" => "Unspecified", "1" => "Internal", "2" => "Server",
            "3" => "Client", "4" => "Producer", "5" => "Consumer",
            _ => $"Value{value}"
        },
        "StatusCode" => value switch
        {
            "0" => "Unset", "1" => "Ok", "2" => "Error",
            _ => $"Value{value}"
        },
        "SeverityNumber" => value switch
        {
            "0" => "Unspecified", "1" => "Trace", "5" => "Debug",
            "9" => "Info", "13" => "Warn", "17" => "Error", "21" => "Fatal",
            _ => $"Value{value}"
        },
        _ => $"Value{value}"
    };

    // ════════════════════════════════════════════════════════════════════════════
    // C# MODELS
    // ════════════════════════════════════════════════════════════════════════════

    static string GenerateModels(string ns, IEnumerable<SchemaDefinition> models, string sourcePath)
    {
        var sb = new StringBuilder();
        AppendHeader(sb, sourcePath, $"Models for {ns}");

        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Text.Json.Serialization;");
        sb.AppendLine();
        sb.AppendLine($"namespace {ns};");
        sb.AppendLine();

        foreach (var model in models.OrderBy(static m => m.GetTypeName()))
        {
            var typeName = EscapeKeyword(model.GetTypeName());
            AppendXmlDoc(sb, model.Description, "");
            sb.AppendLine($"public sealed record {typeName}");
            sb.AppendLine("{");

            foreach (var prop in model.Properties)
            {
                var propName = EscapeKeyword(ToPascalCase(prop.Name));
                var propType = ResolveCSharpType(prop);

                AppendXmlDoc(sb, prop.Description, "    ");
                sb.AppendLine($"    [JsonPropertyName(\"{prop.Name}\")]");

                if (prop.IsRequired)
                    sb.AppendLine($"    public required {propType} {propName} {{ get; init; }}");
                else
                    sb.AppendLine($"    public {propType}? {propName} {{ get; init; }}");

                sb.AppendLine();
            }

            sb.AppendLine("}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    static string ResolveCSharpType(SchemaProperty prop)
    {
        if (prop.Extensions.TryGetValue("x-csharp-type", out var csType)) return csType;

        if (prop.RefPath is not null && prop.GetRefTypeName() is { } refTypeName)
        {
            if (refTypeName.StartsWith("Primitives.", StringComparison.Ordinal)) return $"global::Qyl.Common.{refTypeName[11..]}";
            if (refTypeName.StartsWith("Enums.", StringComparison.Ordinal)) return $"global::Qyl.Enums.{refTypeName[6..]}";
            return refTypeName.StartsWith("Models.", StringComparison.Ordinal) ? $"global::Qyl.Models.{refTypeName[7..]}" : refTypeName;
        }

        if (prop.Type == "array")
        {
            var itemType = prop.ItemsRef is not null
                ? ResolveCSharpType(new SchemaProperty(prop.Name, null, null, null, prop.ItemsRef, null, null, true, ImmutableDictionary<string, string>.Empty))
                : MapOpenApiType(prop.ItemsType, null);
            return $"IReadOnlyList<{itemType}>";
        }

        return MapOpenApiType(prop.Type, prop.Format);
    }

    static string MapOpenApiType(string? type, string? format) => (type, format) switch
    {
        ("string", "date-time") => "DateTimeOffset",
        ("string", "date") => "DateOnly",
        ("string", "time") => "TimeOnly",
        ("string", "uuid") => "Guid",
        ("string", "byte") => "ReadOnlyMemory<byte>",
        ("string", _) => "string",
        ("integer", "int32") => "int",
        ("integer", "int64") => "long",
        ("integer", _) => "int",
        ("number", "float") => "float",
        ("number", "double") => "double",
        ("number", _) => "double",
        ("boolean", _) => "bool",
        _ => "object"
    };

    // ════════════════════════════════════════════════════════════════════════════
    // DUCKDB SCHEMA
    // ════════════════════════════════════════════════════════════════════════════

    static string GenerateDuckDb(IEnumerable<SchemaDefinition> tables, OpenApiSchema schema, string sourcePath)
    {
        var sb = new StringBuilder();
        AppendHeader(sb, sourcePath, "DuckDB schema definitions");

        sb.AppendLine("namespace Qyl.Collector.Storage;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>DuckDB schema from OpenAPI.</summary>");
        sb.AppendLine("public static partial class DuckDbSchema");
        sb.AppendLine("{");

        foreach (var table in tables.OrderBy(static t => t.Extensions["x-duckdb-table"]))
        {
            var tableName = table.Extensions["x-duckdb-table"];
            var constName = ToPascalCase(tableName) + "Ddl";

            sb.AppendLine($"    public const string {constName} = \"\"\"");
            sb.AppendLine($"        CREATE TABLE IF NOT EXISTS {tableName} (");

            var columns = new List<string>();
            string? primaryKey = null;

            foreach (var prop in table.Properties)
            {
                var columnName = prop.Extensions.TryGetValue("x-duckdb-column", out var col) ? col : ToSnakeCase(prop.Name);
                var columnType = ResolveDuckDbType(prop, schema);

                var columnDef = $"            {columnName} {columnType}";
                if (prop.IsRequired && !columnType.Contains("DEFAULT", StringComparison.OrdinalIgnoreCase))
                    columnDef += " NOT NULL";

                columns.Add(columnDef);

                if (prop.Extensions.ContainsKey("x-duckdb-primary-key"))
                    primaryKey = columnName;
            }

            if (!table.Properties.Any(static p => p.Name == "createdAt"))
                columns.Add("            created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP");

            sb.AppendLine(string.Join(",\n", columns));

            if (primaryKey is not null)
                sb.AppendLine($"            PRIMARY KEY ({primaryKey})");

            sb.AppendLine("        );");
            sb.AppendLine("        \"\"\";");
            sb.AppendLine();
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    static string ResolveDuckDbType(SchemaProperty prop, OpenApiSchema schema)
    {
        if (prop.Extensions.TryGetValue("x-duckdb-type", out var duckType)) return duckType;

        if (prop.RefPath is not null)
        {
            var refTypeName = prop.GetRefTypeName();
            var refSchema = schema.Schemas.FirstOrDefault(s => s.Name == refTypeName);
            if (refSchema is not null && refSchema.Extensions.TryGetValue("x-duckdb-type", out var refDuckType))
                return refDuckType;
        }

        return MapOpenApiTypeToDuckDb(prop.Type, prop.Format);
    }

    static string MapOpenApiTypeToDuckDb(string? type, string? format) => (type, format) switch
    {
        ("string", "date-time") => "TIMESTAMP",
        ("string", "date") => "DATE",
        ("string", _) => "VARCHAR",
        ("integer", "int32") => "INTEGER",
        ("integer", "int64") => "BIGINT",
        ("integer", _) => "INTEGER",
        ("number", "float") => "FLOAT",
        ("number", "double") => "DOUBLE",
        ("number", _) => "DOUBLE",
        ("boolean", _) => "BOOLEAN",
        _ => "VARCHAR"
    };

    // ════════════════════════════════════════════════════════════════════════════
    // HELPERS
    // ════════════════════════════════════════════════════════════════════════════

    static void AppendHeader(StringBuilder sb, string sourcePath, string description)
    {
        sb.AppendLine("// =============================================================================");
        sb.AppendLine("// AUTO-GENERATED FILE - DO NOT EDIT");
        sb.AppendLine("// =============================================================================");
        sb.AppendLine($"//     Source:    {sourcePath}");
        sb.AppendLine($"//     Generated: {TimeProvider.System.GetUtcNow():O}");
        sb.AppendLine($"//     {description}");
        sb.AppendLine("// =============================================================================");
        sb.AppendLine();
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
    }

    static void AppendXmlDoc(StringBuilder sb, string? description, string indent)
    {
        if (string.IsNullOrWhiteSpace(description)) return;
        var escaped = description.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        sb.AppendLine($"{indent}/// <summary>{escaped}</summary>");
    }

    static string GetCSharpNamespace(string schemaName)
    {
        if (schemaName.StartsWith("Primitives.", StringComparison.Ordinal)) return "Qyl.Common";
        if (schemaName.StartsWith("Enums.", StringComparison.Ordinal)) return "Qyl.Enums";
        if (schemaName.StartsWith("Models.", StringComparison.Ordinal)) return "Qyl.Models";
        return schemaName.StartsWith("Api.", StringComparison.Ordinal) ? "Qyl.Api" : "Qyl";
    }

    static string GetFileNameFromNamespace(string ns) => ns switch
    {
        "Qyl.Common" => "Scalars",
        "Qyl.Enums" => "Enums",
        "Qyl.Models" => "Models",
        "Qyl.Api" => "Api",
        _ => "Types"
    };

    static (string CSharpType, string JsonRead, string JsonWrite) GetScalarTypeInfo(string? type, string? format) =>
        (type, format) switch
        {
            ("string", _) => ("string", "reader.GetString() ?? string.Empty", "writer.WriteStringValue(value.Value)"),
            ("integer", "int64") => ("long", "reader.GetInt64()", "writer.WriteNumberValue(value.Value)"),
            ("integer", _) => ("int", "reader.GetInt32()", "writer.WriteNumberValue(value.Value)"),
            ("number", "double") => ("double", "reader.GetDouble()", "writer.WriteNumberValue(value.Value)"),
            ("number", "float") => ("float", "reader.GetSingle()", "writer.WriteNumberValue(value.Value)"),
            ("number", _) => ("double", "reader.GetDouble()", "writer.WriteNumberValue(value.Value)"),
            ("boolean", _) => ("bool", "reader.GetBoolean()", "writer.WriteBooleanValue(value.Value)"),
            _ => ("string", "reader.GetString() ?? string.Empty", "writer.WriteStringValue(value.Value)")
        };

    static string ToPascalCase(string value)
    {
        if (string.IsNullOrEmpty(value)) return "Unknown";

        var sb = new StringBuilder();
        var capitalizeNext = true;

        foreach (var c in value)
        {
            if (c is '_' or '-' or ' ' or '.')
                capitalizeNext = true;
            else if (capitalizeNext)
            {
                sb.Append(char.ToUpperInvariant(c));
                capitalizeNext = false;
            }
            else
                sb.Append(c);
        }

        if (sb.Length == 0) return "Unknown";
        var result = sb.ToString();
        return char.IsDigit(result[0]) ? $"_{result}" : result;
    }

    static string ToSnakeCase(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;

        var sb = new StringBuilder();
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (char.IsUpper(c))
            {
                if (i > 0) sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            else
                sb.Append(c);
        }

        return sb.ToString();
    }

    static string EscapeKeyword(string name) => name switch
    {
        "abstract" or "as" or "base" or "bool" or "break" or "byte" or "case" or "catch" or "char" or "checked"
            or "class" or "const" or "continue" or "decimal" or "default" or "delegate" or "do" or "double"
            or "else" or "enum" or "event" or "explicit" or "extern" or "false" or "finally" or "fixed"
            or "float" or "for" or "foreach" or "goto" or "if" or "implicit" or "in" or "int" or "interface"
            or "internal" or "is" or "lock" or "long" or "namespace" or "new" or "null" or "object" or "operator"
            or "out" or "override" or "params" or "private" or "protected" or "public" or "readonly" or "ref"
            or "return" or "sbyte" or "sealed" or "short" or "sizeof" or "stackalloc" or "static" or "string"
            or "struct" or "switch" or "this" or "throw" or "true" or "try" or "typeof" or "uint" or "ulong"
            or "unchecked" or "unsafe" or "ushort" or "using" or "virtual" or "void" or "volatile" or "while"
            => $"@{name}",
        _ => name
    };
}

// ════════════════════════════════════════════════════════════════════════════════
// OPENAPI SCHEMA PARSER (using YamlDotNet - supports OpenAPI 3.1.0)
// ════════════════════════════════════════════════════════════════════════════════

public sealed record OpenApiSchema(string Title, string Version, ImmutableArray<SchemaDefinition> Schemas)
{
    public static OpenApiSchema Load(FullPath path)
    {
        using var reader = new StreamReader(path);
        var yaml = new YamlStream();
        yaml.Load(reader);

        var root = (YamlMappingNode)yaml.Documents[0].RootNode;
        var info = GetMapping(root, "info") ?? throw new InvalidOperationException("Missing 'info' section in OpenAPI schema");
        var title = GetString(info, "title") ?? "API";
        var version = GetString(info, "version") ?? "0.0.0";

        var schemas = ImmutableArray.CreateBuilder<SchemaDefinition>();
        var components = GetMapping(root, "components");
        var schemasNode = components is not null ? GetMapping(components, "schemas") : null;

        if (schemasNode is not null)
            foreach (var (keyNode, valueNode) in schemasNode.Children)
            {
                if (keyNode is YamlScalarNode { Value: { } name } && valueNode is YamlMappingNode schemaNode)
                    schemas.Add(ParseSchema(name, schemaNode));
            }

        return new OpenApiSchema(title, version, schemas.ToImmutable());
    }

    static SchemaDefinition ParseSchema(string name, YamlMappingNode node)
    {
        var type = GetString(node, "type");
        var description = GetString(node, "description");
        var format = GetString(node, "format");
        var pattern = GetString(node, "pattern");
        var enumValues = GetStringArray(node, "enum");
        var extensions = ParseExtensions(node);

        var isScalar = extensions.ContainsKey("x-csharp-struct") ||
                       type is "string" or "integer" or "number" && enumValues.Length == 0 &&
                       GetMapping(node, "properties")?.Children.Any() != true;

        var isEnum = enumValues.Length > 0;

        var properties = ImmutableArray<SchemaProperty>.Empty;
        var propsNode = GetMapping(node, "properties");
        var required = GetStringArray(node, "required").ToHashSet();

        if (propsNode is not null)
        {
            var propsBuilder = ImmutableArray.CreateBuilder<SchemaProperty>();
            foreach (var (keyNode, valueNode) in propsNode.Children)
            {
                if (keyNode is YamlScalarNode { Value: { } propName } && valueNode is YamlMappingNode propNode)
                    propsBuilder.Add(ParseProperty(propName, propNode, required.Contains(propName)));
            }

            properties = propsBuilder.ToImmutable();
        }

        return new SchemaDefinition(name, type, description, format, pattern, enumValues, properties, extensions, isScalar, isEnum);
    }

    static SchemaProperty ParseProperty(string name, YamlMappingNode node, bool isRequired)
    {
        var type = GetString(node, "type");
        var format = GetString(node, "format");
        var description = GetString(node, "description");

        var allOf = node.Children.TryGetValue("allOf", out var allOfNode) && allOfNode is YamlSequenceNode seq
            ? seq.Children.OfType<YamlMappingNode>().FirstOrDefault()
            : null;

        var refPath = GetRef(node) ?? (allOf is not null ? GetRef(allOf) : null);

        string? itemsRef = null;
        string? itemsType = null;
        var items = GetMapping(node, "items");
        if (items is not null)
        {
            itemsRef = GetRef(items);
            itemsType = GetString(items, "type");
        }

        return new SchemaProperty(name, type, format, description, refPath, itemsRef, itemsType, isRequired, ParseExtensions(node));
    }

    static ImmutableDictionary<string, string> ParseExtensions(YamlMappingNode node)
    {
        var builder = ImmutableDictionary.CreateBuilder<string, string>();
        foreach (var (keyNode, valueNode) in node.Children)
        {
            var key = ((YamlScalarNode)keyNode).Value ?? "";
            if (key.StartsWith("x-", StringComparison.Ordinal))
                builder[key] = valueNode switch
                {
                    YamlScalarNode scalar => scalar.Value ?? "",
                    YamlSequenceNode seq => string.Join(",", seq.Children.OfType<YamlScalarNode>().Select(static s => s.Value)),
                    _ => ""
                };
        }

        return builder.ToImmutable();
    }

    static YamlMappingNode? GetMapping(YamlMappingNode parent, string key) =>
        parent.Children.TryGetValue(key, out var node) && node is YamlMappingNode m ? m : null;

    static string? GetString(YamlMappingNode parent, string key) =>
        parent.Children.TryGetValue(key, out var node) && node is YamlScalarNode s ? s.Value : null;

    static string? GetRef(YamlMappingNode node) =>
        node.Children.TryGetValue("$ref", out var refNode) && refNode is YamlScalarNode s ? s.Value : null;

    static ImmutableArray<string> GetStringArray(YamlMappingNode parent, string key)
    {
        if (parent.Children.TryGetValue(key, out var node) && node is YamlSequenceNode seq)
            return [..seq.Children.OfType<YamlScalarNode>().Select(static s => s.Value ?? "").Where(static s => s.Length > 0)];
        return [];
    }
}

public sealed record SchemaDefinition(
    string Name,
    string? Type,
    string? Description,
    string? Format,
    string? Pattern,
    ImmutableArray<string> EnumValues,
    ImmutableArray<SchemaProperty> Properties,
    ImmutableDictionary<string, string> Extensions,
    bool IsScalar,
    bool IsEnum)
{
    public string GetTypeName() => Name[(Name.LastIndexOf('.') + 1)..];

    public ImmutableArray<string> GetEnumVarNames()
    {
        if (Extensions.TryGetValue("x-enum-varnames", out var varnames) && !string.IsNullOrEmpty(varnames))
            return [..varnames.Split(',').Select(static s => s.Trim())];
        return [];
    }
}

public sealed record SchemaProperty(
    string Name,
    string? Type,
    string? Format,
    string? Description,
    string? RefPath,
    string? ItemsRef,
    string? ItemsType,
    bool IsRequired,
    ImmutableDictionary<string, string> Extensions)
{
    const string RefPrefix = "#/components/schemas/";

    public string? GetRefTypeName() => RefPath?.StartsWith(RefPrefix, StringComparison.Ordinal) == true
        ? RefPath[RefPrefix.Length..]
        : RefPath;
}

// ════════════════════════════════════════════════════════════════════════════════
// GENERATION GUARD
// ════════════════════════════════════════════════════════════════════════════════

public enum GuardMode
{
    Force,
    DryRun,
    SkipExisting
}

public partial class GenerationGuard(GuardMode mode)
{
    private readonly GuardMode _mode = mode;
    public const GuardMode DefaultMode = GuardMode.SkipExisting;

    [GeneratedRegex(@"^(//|--)\s+Generated:\s+\d{4}-\d{2}-\d{2}T.*$", RegexOptions.Multiline)]
    private static partial Regex GeneratedTimestampRegex();

    public void WriteFile(FullPath path, string content)
    {
        var normalizedNew = Normalize(content);
        if (File.Exists(path))
        {
            var existing = File.ReadAllText(path);
            if (Normalize(existing) == normalizedNew)
            {
                Console.WriteLine($"Unchanged: {path}");
                return;
            }

            if (_mode == GuardMode.SkipExisting)
            {
                Console.WriteLine($"Skipping existing: {path}");
                return;
            }
        }

        if (_mode == GuardMode.DryRun)
        {
            Console.WriteLine($"Dry run: Would write to {path}");
            return;
        }

        path.CreateParentDirectory();
        File.WriteAllText(path, content);
        Console.WriteLine($"Written: {path}");
    }

    static string Normalize(string content)
    {
        var normalized = content.ReplaceLineEndings("\n");
        return GeneratedTimestampRegex().Replace(normalized, "$1 Generated: <STABLE>");
    }
}
