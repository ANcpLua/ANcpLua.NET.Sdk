# ThrowPolyfills - C# 14 Extension Members

Modern throw helpers using **C# 14 Extension Members** that polyfill .NET 6/7/8/9+ APIs for older targets.

## Features

Uses the new `extension(Type)` syntax to add static methods to exception types:

| API                                                 | Available in | Polyfilled for |
|-----------------------------------------------------|--------------|----------------|
| `ArgumentNullException.ThrowIfNull`                 | .NET 6+      | < .NET 6       |
| `ArgumentException.ThrowIfNullOrEmpty`              | .NET 7+      | < .NET 7       |
| `ArgumentException.ThrowIfNullOrWhiteSpace`         | .NET 7+      | < .NET 7       |
| `ArgumentOutOfRangeException.ThrowIfNegative`       | .NET 8+      | < .NET 8       |
| `ArgumentOutOfRangeException.ThrowIfZero`           | .NET 8+      | < .NET 8       |
| `ArgumentOutOfRangeException.ThrowIfNegativeOrZero` | .NET 8+      | < .NET 8       |
| `ArgumentOutOfRangeException.ThrowIfGreaterThan`    | .NET 8+      | < .NET 8       |
| `ArgumentOutOfRangeException.ThrowIfLessThan`       | .NET 8+      | < .NET 8       |
| `ObjectDisposedException.ThrowIf`                   | .NET 9+      | < .NET 9       |

## Usage

```csharp
public void ProcessData(string data, int count)
{
    // .NET 6+ style - works on all targets!
    ArgumentNullException.ThrowIfNull(data);
    ArgumentException.ThrowIfNullOrWhiteSpace(data);
    ArgumentOutOfRangeException.ThrowIfNegative(count);

    // Process...
}
```

## Requirements

- **C# 14** (LangVersion 14 or preview)
- **.NET 10 SDK** (for C# 14 support)
- Polyfills for `CallerArgumentExpression`, `NotNull`, `DoesNotReturn` attributes

## How it works

C# 14 introduces `extension(Type)` blocks that allow adding static methods to existing types:

```csharp
extension(ArgumentNullException)
{
    public static void ThrowIfNull(object? argument, ...) { ... }
}

// Now you can call:
ArgumentNullException.ThrowIfNull(myArg);  // Looks like native .NET 6+ API!
```

The `#if !NET6_0_OR_GREATER` directives ensure polyfills are only compiled when needed.
