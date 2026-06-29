# T015 / T016 — Visibility flip (#26) — OPERATOR STEP, environment-limited

**Status (2026-06-29):** ⛔ **NOT performed in-session — requires an org admin.** Disclosed, not
faked (Principle V).

## Why this cannot be scripted

GitHub exposes **no REST/`gh api` endpoint** to *change* a package's visibility (research R3). The API
can only **read** it:
```
$ gh api orgs/FS-GG/packages/nuget/FS.GG.UI.Template --jq .visibility
private
```
Changing visibility is an **admin-only GitHub UI action**. The in-session token (`EHotwagner`) holds
`read:packages` only — it cannot flip visibility even if an endpoint existed.

## Operator action required (one of)

1. **Preferred — flip `private → internal`** (org-wide read, matches the public `FS.GG.*.Cli` norm):
   - Go to `https://github.com/orgs/FS-GG/packages/nuget/package/FS.GG.UI.Template/settings`
   - **Change visibility → Internal**
2. **Fallback (FR-003 equal)** — grant `FS-GG/FS.GG.Templates` repo **Read** on the same settings page
   (Manage Actions access).

## Machine-checkable acceptance (run after the operator acts)

```bash
gh api orgs/FS-GG/packages/nuget/FS.GG.UI.Template --jq .visibility   # expect: internal
```
- **Pass** when this returns `internal` (or the Templates Read grant is confirmed). (INV-8)

Until then, an ordinary org-consumer token (`packages: read`, no special grant) still gets **exit 103**
on `dotnet new install FS.GG.UI.Template@0.1.53-preview.1`, so the FR-004 combined gate (US1) cannot
pass. This is the **single remaining blocker** after the publish lands.

## Current observed state

```
visibility: private   (unchanged — awaiting operator)
```
