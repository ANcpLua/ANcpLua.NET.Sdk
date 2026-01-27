# OTelConventions

OpenTelemetry Semantic Conventions as C# constants.

## Features

- **Zero dependencies** - BCL only, works everywhere
- **AOT compatible** - Fully trimmable
- **Generated** - From `@opentelemetry/semantic-conventions` NPM package
- **Complete** - All stable and experimental conventions

## Installation

```bash
dotnet add package OTelConventions
```

## Usage

```csharp
using OTelConventions;

// GenAI attributes
activity.SetTag(GenAiAttributes.ProviderName, GenAiSystemValues.Anthropic);
activity.SetTag(GenAiAttributes.RequestModel, "claude-sonnet-4-20250514");
activity.SetTag(GenAiUsageAttributes.InputTokens, 150);
activity.SetTag(GenAiUsageAttributes.OutputTokens, 42);

// HTTP attributes
activity.SetTag(HttpAttributes.RequestMethod, "GET");
activity.SetTag(HttpAttributes.ResponseStatusCode, 200);

// Database attributes
activity.SetTag(DbAttributes.System, DbSystemValues.Postgresql);
activity.SetTag(DbAttributes.Name, "mydb");

// And many more: Cloud, Container, K8s, Messaging, RPC, Network, etc.
```

## Semantic Convention Version

This package is generated from `@opentelemetry/semantic-conventions` v1.39.0.

## License

MIT
