# qyl.dashboard

@../../.claude/instructions/architecture.md

## Scope

React frontend for viewing traces, sessions, and metrics. Uses generated TypeScript types from Kiota.

## Tech Stack

- Vite
- React 19
- TypeScript 5.7
- TailwindCSS 4
- shadcn/ui
- Vitest

## Type Source

<generated_types>
Types are in `src/types/generated/` from Kiota. Do NOT edit these files.

```typescript
// CORRECT - import from generated
import type { SpanData, SessionSummary } from '@/types/generated/models';

// WRONG - manual type definition (you will be penalized)
interface SpanData {
  traceId: string;
  // ...
}
```

If types are missing:
1. Update TypeSpec in `core/specs/`
2. Run `nuke GenerateTypeScript`
3. Import from generated
</generated_types>

## SSE Pattern

<sse_hook>
```typescript
export function useSse<T>(endpoint: string) {
  const [data, setData] = useState<T[]>([]);
  const [status, setStatus] = useState<'connecting' | 'open' | 'error'>('connecting');

  useEffect(() => {
    const es = new EventSource(`${API_URL}${endpoint}`);
    es.onopen = () => setStatus('open');
    es.onerror = () => setStatus('error');
    es.onmessage = (e) => {
      setData(prev => [JSON.parse(e.data) as T, ...prev].slice(0, 100));
    };
    return () => es.close();
  }, [endpoint]);

  return { data, status };
}
```
</sse_hook>

## Forbidden Actions

- Do NOT edit files in `src/types/generated/`
- Do NOT define types that exist in generated
- Do NOT use `any` type
- Do NOT import from .NET projects

## Commands

```bash
npm run dev      # Start dev server
npm run build    # Production build
npm run test     # Run Vitest
npm run lint     # ESLint
```

## Validation

Before commit:
- [ ] No manual types duplicating generated
- [ ] No edits to generated files
- [ ] `npm run build` succeeds
- [ ] `npm run test` passes
