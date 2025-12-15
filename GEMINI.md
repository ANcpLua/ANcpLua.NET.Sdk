---
agent: gemini_cli
tools: [ search, edit, changes, problems ]
description: Repo-specific operating instructions for Gemini CLI when working on ANcpLua.NET.Sdk (MSBuild SDK + injected shared code).
---

# GEMINI.md — How to work on ANcpLua.NET.Sdk

This file is **Gemini-specific** operational guidance (how to execute work here).
For repo-wide rules that apply to *any* agent, read `AGENTS.md` first.

> **Name authority:** use the identifiers from `tests/ANcpLua.Sdk.Tests/Infrastructure/TestInfrastructure.cs` when
> referencing test constants, markers, and helpers.

---

## 0) Default execution posture

- Avoid broad formatting sweeps.
- Don’t “refactor for fun”: refactor only to remove duplication you touched, or to fix the humans requests.ye

---

## 1) Fast repo navigation (what to search first)

When the request mentions “injection”, “polyfill”, or `eng/Shared`:

- Search for the **area README** and the **property name** it documents.
- Jump to:
    - `eng/Shared/<Area>/README.md` (consumer-facing docs)
    - `eng/Shared/<Area>/...` sources (what gets injected)
    - MSBuild props/targets that add `Compile Include=...` conditioned on that property

---

## 2) Test Infrastructure & Patterns

We use a consolidated infrastructure in `tests/ANcpLua.Sdk.Tests/Infrastructure/TestInfrastructure.cs`.

### Key Components:

| Type                                    | Purpose                                           |
|:----------------------------------------|:--------------------------------------------------|
| **`TargetFrameworks`**                  | TFM constants (`netstandard2.0`,, `net10.0`)      |
| **`MsBuildProperties`**                 | Property names via `nameof()`                     |
| **`MsBuildItems`**                      | Item names via `nameof()`                         |
| **`MsBuildAttributes`**                 | Attribute names via `nameof()`                    |
| **`MsBuildValues`**                     | Common values (`Enable`, `True`, `Library`, etc.) |
| **`RepositoryPaths`**                   | All polyfill file paths                           |
| **`InjectionPropertyNames`**            | MSBuild injection toggles                         |
| **`PolyfillTypeNames`**                 | Fully-qualified type names                        |
| **`PolyfillActivationCode`**            | C# activation snippets                            |
| **`PolyfillDefinition`**                | Record struct combining all metadata              |
| **`RepositoryRoot`**                    | Repo root locator with indexer                    |
| **`MsBuildPropertyBuilder`**            | Fluent property builder                           |
| **`XmlSnippetBuilder`**                 | XML snippet generators                            |
| **`PolyfillTestDataSource`**            | TheoryData matrices                               |
| **`IInjectedFile` / `IPolyfillMarker`** | Marker interfaces                                 |
| **13 marker classes**                   | `ThrowFile`, `LockFile`, etc.                     |

### Adding a new Polyfill Test:

1. Define a marker class implementing `IPolyfillMarker` in `TestInfrastructure.cs`.
2. Add a new `PolyfillCase<NewMarker>(...)` to `PolyfillActivationTests.cs` or `PolyfillInjectionTests.cs`.

---

## 3) How to validate MSBuild wiring (the “binlog mindset”)

For this repo, the strongest signal is: **did MSBuild include the right Compile items / run the right targets?**

Use the existing helpers:

- `ProjectBuilder` to create a temp test project and run `dotnet build /bl`
- `BuildResult.GetMSBuildItems("Compile")` to assert injected files were included
- `MsbuildShould.ShouldGenerate<TMarker>(...)` for concise assertions.

### Minimal pattern (pseudo-code)

```csharp
await using var project = new ProjectBuilder(...);

// Use the builder for properties
var props = MsBuildPropertyBuilder.Create()ee
    .WithTargetFramework(TargetFrameworks.NetStandard2_0)
    .Enable(InjectionPropertyNames.Throw);

project.AddCsprojFile(properties: props.ToPropertyArray());

// ... build and assert
```

---

## 4) When to update docs

If you change:

- an injected file
- a property name
- enable/disable semantics

…update the relevant `eng/Shared/<Area>/README.md` so consumers know how to turn it on.

---

## 5) Output format when you finish a task

When you respond with results, include:

- **What changed** (files + intent)
- **How to verify** (exact commands)
- **What you did not change** (important to reduce reviewer anxiety)
