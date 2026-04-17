# Version.props Cross-Repo Sync

`src/Build/Common/Version.props` is the canonical version manifest. On every push that touches it, `.github/workflows/sync-versions.yml` opens an auto-merging PR against `ANcpLua/ANcpLua.Analyzers` to keep that repo's root `Version.props` identical.

## History

- **2026-01-09** — sync introduced (commit `3e054fe`), auth via classic PAT stored as `CROSS_REPO_TOKEN`.
- **2026-04-02** — PAT expired. Every sync run since that day failed silently at `actions/checkout` with `Bad credentials`. Took 2 weeks to notice.
- **2026-04-17** — workflow rewritten to mint a per-run installation token from a dedicated GitHub App (commit `7c112d7`). App tokens don't expire.

## Setup (one-time, for the repo owner)

1. Create a GitHub App at <https://github.com/settings/apps/new>:
   - **Repository permissions**: `Contents` Read/Write, `Pull requests` Read/Write
   - No webhook, no user-level access, install only on this account
2. Generate a private key (downloads a `.pem` file)
3. Install the App on **both** `ANcpLua/ANcpLua.NET.Sdk` and `ANcpLua/ANcpLua.Analyzers`
4. Add two secrets to `ANcpLua/ANcpLua.NET.Sdk`:
   - `SYNC_APP_ID` — numeric App ID from the App settings page
   - `SYNC_APP_PRIVATE_KEY` — full `.pem` contents including `BEGIN`/`END` lines
5. Delete the stale `CROSS_REPO_TOKEN` secret

## Rotation

App tokens don't expire so rotation isn't scheduled. If the App's private key is compromised, regenerate it in the App settings and update `SYNC_APP_PRIVATE_KEY`. No other action needed.

## Failure modes

| Symptom | Cause | Fix |
|---|---|---|
| `Error: Input required and not supplied: app-id` | `SYNC_APP_ID` secret missing | complete step 4 above |
| `Error: could not parse private key` | `SYNC_APP_PRIVATE_KEY` malformed | re-paste full `.pem` contents |
| `HttpError: Not Found` on checkout | App not installed on target repo | install on `ANcpLua.Analyzers` |
| `403 Resource not accessible by integration` | App permissions too narrow | add `Contents` + `Pull requests` write |
