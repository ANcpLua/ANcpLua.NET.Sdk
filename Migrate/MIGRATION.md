# ANcpLua.NET.Sdk v1.4.0 - Polyfills Consolidation

## Breaking Changes

None. All old property names are aliased to the new system.

## What Changed

### Before (v1.3.x)
```
common/
├── LegacySupport.props    ← Polyfill properties (partial)
├── LegacySupport.targets  ← Polyfill injection (broken paths!)
├── Shared.props           ← More polyfill properties (duplicate!)
├── Shared.targets         ← More polyfill injection (also broken!)
└── ...

Problem: Two systems, inconsistent paths, agent loops
```

### After (v1.4.0)
```
common/
├── Polyfills.props        ← ALL polyfill properties (single source of truth)
├── Polyfills.targets      ← ALL polyfill injection (correct paths)
├── Shared.props           ← Non-polyfill utilities only
├── Shared.targets         ← Non-polyfill injection only
├── LegacySupport.props    ← DEPRECATED: forwards to Polyfills.props
├── LegacySupport.targets  ← DEPRECATED: forwards to Polyfills.targets
└── ...
```

## New API

### Master Switches
```xml
<!-- Enable all polyfills (default: true) -->
<InjectPolyfills>true</InjectPolyfills>

<!-- Disable all polyfills -->
<InjectPolyfills>false</InjectPolyfills>
```

### Bundle Switches
```xml
<!-- Language features: IsExternalInit, Index/Range, CallerArgumentExpression, etc. -->
<InjectLanguagePolyfills>true</InjectLanguagePolyfills>

<!-- BCL extensions: Lock, TimeProvider, UnreachableException -->
<InjectBclPolyfills>true</InjectBclPolyfills>

<!-- Analyzer compliance: StringExtensions (CA1307), Nullable attrs, Trim attrs -->
<InjectAnalyzerPolyfills>true</InjectAnalyzerPolyfills>
```

### Individual Switches
```xml
<!-- All default to their bundle value, auto-disabled when TFM has native support -->
<InjectIsExternalInitPolyfill>true</InjectIsExternalInitPolyfill>
<InjectIndexRangePolyfill>true</InjectIndexRangePolyfill>
<InjectCallerArgumentExpressionPolyfill>true</InjectCallerArgumentExpressionPolyfill>
<InjectRequiredMemberPolyfill>true</InjectRequiredMemberPolyfill>
<InjectParamCollectionPolyfill>true</InjectParamCollectionPolyfill>
<InjectLockPolyfill>true</InjectLockPolyfill>
<InjectTimeProviderPolyfill>true</InjectTimeProviderPolyfill>
<InjectUnreachableExceptionPolyfill>true</InjectUnreachableExceptionPolyfill>
<InjectStringExtensionsPolyfill>true</InjectStringExtensionsPolyfill>
<InjectNullabilityAttributesPolyfill>true</InjectNullabilityAttributesPolyfill>
<InjectStackTraceHiddenPolyfill>true</InjectStackTraceHiddenPolyfill>
<InjectTrimAttributesPolyfill>true</InjectTrimAttributesPolyfill>
<InjectExperimentalAttributePolyfill>true</InjectExperimentalAttributePolyfill>
```

### Diagnostics
```xml
<!-- Enable verbose output during build -->
<ANcpLuaDiagnostics>true</ANcpLuaDiagnostics>
```

## Property Name Migration

| Old Name (v1.3.x)                        | New Name (v1.4.0)                      |
|------------------------------------------|----------------------------------------|
| `InjectIsExternalInitOnLegacy`           | `InjectIsExternalInitPolyfill`         |
| `InjectRequiredMemberOnLegacy`           | `InjectRequiredMemberPolyfill`         |
| `InjectCompilerFeatureRequiredOnLegacy`  | `InjectRequiredMemberPolyfill`         |
| `InjectTrimAttributesOnLegacy`           | `InjectTrimAttributesPolyfill`         |
| `InjectNullableAttributesOnLegacy`       | `InjectNullabilityAttributesPolyfill`  |
| `InjectUnreachableExceptionOnLegacy`     | `InjectUnreachableExceptionPolyfill`   |
| `InjectExperimentalAttributeOnLegacy`    | `InjectExperimentalAttributePolyfill`  |
| `InjectRecordPolyfill`                   | `InjectIsExternalInitPolyfill`         |
| `InjectAllPolyfillsOnLegacy`             | `InjectPolyfills`                      |
| `InjectANcpLuaLanguageFeaturesPolyfills` | `InjectLanguagePolyfills`              |
| `InjectANcpLuaNullabilityPolyfills`      | `InjectNullabilityAttributesPolyfill`  |
| `InjectANcpLuaIndexRangePolyfills`       | `InjectIndexRangePolyfill`             |
| `InjectANcpLuaDiagnosticsPolyfills`      | `InjectStackTraceHiddenPolyfill`       |

All old names continue to work (aliased in Polyfills.props).

## Smart Defaults

Polyfills are now **TFM-aware**. If your target framework already has the feature, the polyfill is automatically disabled:

| Feature                  | Native Support |
|--------------------------|----------------|
| IsExternalInit           | .NET 5+        |
| Index/Range              | .NET Core 3.0+ |
| CallerArgumentExpression | .NET 6+        |
| RequiredMember           | .NET 7+        |
| UnreachableException     | .NET 7+        |
| TimeProvider             | .NET 8+        |
| ExperimentalAttribute    | .NET 8+        |
| Lock                     | .NET 9+        |
| StringExtensions         | .NET 5+        |
| ParamCollection          | .NET 9+        |

## Files in Package

```
ANcpLua.NET.Sdk.nupkg
├── Sdk/
│   ├── Sdk.props
│   └── Sdk.targets
├── common/
│   ├── Polyfills.props     ← NEW
│   ├── Polyfills.targets   ← NEW
│   ├── Shared.props        ← Simplified
│   ├── Shared.targets      ← Simplified
│   ├── LegacySupport.props ← DEPRECATED (forwards)
│   ├── LegacySupport.targets ← DEPRECATED (forwards)
│   └── ...
├── shared/
│   ├── Polyfills/
│   │   ├── LanguageFeatures/
│   │   │   ├── IsExternalInit.cs
│   │   │   ├── CallerArgumentExpressionAttribute.cs
│   │   │   ├── CompilerFeatureRequiredAttribute.cs
│   │   │   ├── RequiredMemberAttribute.cs
│   │   │   └── ParamCollectionAttribute.cs
│   │   ├── IndexRange/
│   │   │   ├── Index.cs
│   │   │   └── Range.cs
│   │   ├── TimeProvider/TimeProvider.cs
│   │   ├── Exceptions/UnreachableException.cs
│   │   ├── StringExtensions/StringExtensions.cs
│   │   ├── DiagnosticAttributes/NullableAttributes.cs
│   │   ├── NullabilityAttributes/MemberNotNullAttributes.cs
│   │   ├── Diagnostics/StackTraceHiddenAttribute.cs
│   │   ├── Experimental/ExperimentalAttribute.cs
│   │   ├── TrimAttributes/*.cs
│   │   └── Lock.cs
│   ├── Throw/Throw.cs
│   └── Extensions/...
└── ...
```
