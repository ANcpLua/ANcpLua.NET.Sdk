using Microsoft.CodeAnalysis.CSharp;

namespace ANcpSdk.AspNetCore.ServiceDefaults.AutoRegister;

internal sealed class InterceptionData : IEquatable<InterceptionData?>
{
    public required string OrderKey { get; init; }
    public required InterceptionMethodKind Kind { get; init; }
    public required InterceptableLocation InterceptableLocation { get; init; }

    public bool Equals(InterceptionData? other) =>
        other is not null && Kind == other.Kind &&
        EqualityComparer<InterceptableLocation>.Default.Equals(InterceptableLocation,
            other.InterceptableLocation);

    public override bool Equals(object? obj) => Equals(obj as InterceptionData);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + Kind.GetHashCode();
            hash = hash * 31 + InterceptableLocation.GetHashCode();
            return hash;
        }
    }
}
