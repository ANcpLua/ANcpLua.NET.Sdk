using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace ANcpLua.Sdk.Tests.Infrastructure;

internal static class PeImage
{
    public static IReadOnlyDictionary<string, string> ReadAssemblyMetadata(string dllPath)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        ForEachCustomAttribute(dllPath, "AssemblyMetadataAttribute", reader =>
        {
            if (reader.ReadSerializedString() is { } key)
                result[key] = reader.ReadSerializedString() ?? "";
        });
        return result;
    }

    public static string? ReadTargetFrameworkName(string dllPath)
    {
        string? name = null;
        ForEachCustomAttribute(dllPath, "TargetFrameworkAttribute", reader => name ??= reader.ReadSerializedString());
        return name;
    }

    public static bool HasCodeViewDebugEntry(string dllPath)
    {
        using var stream = File.OpenRead(dllPath);
        using var peReader = new PEReader(stream);
        return peReader.ReadDebugDirectory().Any(static entry => entry.Type is DebugDirectoryEntryType.CodeView);
    }

    private static void ForEachCustomAttribute(string dllPath, string attributeTypeName, Action<BlobReader> readArguments)
    {
        using var stream = File.OpenRead(dllPath);
        using var peReader = new PEReader(stream);
        var metadata = peReader.GetMetadataReader();

        foreach (var handle in metadata.CustomAttributes)
        {
            var attribute = metadata.GetCustomAttribute(handle);
            if (AttributeTypeName(metadata, attribute) != attributeTypeName)
                continue;

            var blob = metadata.GetBlobReader(attribute.Value);
            blob.ReadUInt16();
            readArguments(blob);
        }
    }

    private static string? AttributeTypeName(MetadataReader metadata, CustomAttribute attribute)
    {
        switch (attribute.Constructor.Kind)
        {
            case HandleKind.MemberReference:
                var member = metadata.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
                return member.Parent.Kind is HandleKind.TypeReference
                    ? metadata.GetString(metadata.GetTypeReference((TypeReferenceHandle)member.Parent).Name)
                    : null;
            case HandleKind.MethodDefinition:
                var method = metadata.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor);
                return metadata.GetString(metadata.GetTypeDefinition(method.GetDeclaringType()).Name);
            default:
                return null;
        }
    }
}
