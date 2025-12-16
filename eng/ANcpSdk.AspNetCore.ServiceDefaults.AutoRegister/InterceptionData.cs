using Microsoft.CodeAnalysis.CSharp;

namespace ANcpSdk.AspNetCore.ServiceDefaults.AutoRegister;

internal sealed class InterceptionData : IEquatable<InterceptionData?>
{
    public required string OrderKey { get; set; }
    public required InterceptionMethodKind Kind { get; set; }
    public required InterceptableLocation InterceptableLocation { get; set; }

    public bool Equals(InterceptionData? other)
    {
        return other is not null && Kind == other.Kind &&
               EqualityComparer<InterceptableLocation>.Default.Equals(InterceptableLocation,
                   other.InterceptableLocation);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as InterceptionData);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + Kind.GetHashCode();
            hash = hash * 31 + (InterceptableLocation?.GetHashCode() ?? 0);
            return hash;
        }
    }
}