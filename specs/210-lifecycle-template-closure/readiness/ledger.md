# Readiness Ledger — Feature 210

## Evidence index

| file | what it proves | provenance |
|---|---|---|
| `epic-acceptance.md` | The single consolidated Epic Acceptance Record (US1) — published-package 3×4 matrix, byte-identical default, build spot-check, conclusion | **live** (`FS_GG_RUN_PUBLISHED_ACCEPTANCE=1`) |
| `gated-set-map.md` | Gated lifecycle set + byte-identical baseline reference (T004) | read from `template.json` |
| `early-smoke.md` | Early live smoke run confirming the published package behaves like the child-validated working tree (T005) | live spot-check |
| `closure-state.md` | Coordination board transition + cross-repo remainder attribution/dedupe (US3) | live `gh project` |
| `baseline.md` | No-regression baseline: 21/21 test projects green (T002) | live `dotnet test` |

## Feature 168 — gitignore allowlist proof

`specs/*/readiness/` is gitignored by default; this feature's readiness is allowlisted in `.gitignore`:

```
!specs/210-lifecycle-template-closure/readiness/
!specs/210-lifecycle-template-closure/readiness/**
specs/210-lifecycle-template-closure/readiness/**/nuget-cache/
```

`git check-ignore` on each evidence file returns **no match** (tracked-eligible) — verified
2026-06-28 for `epic-acceptance.md`, `gated-set-map.md`, `early-smoke.md`, `closure-state.md`,
`baseline.md`. No degraded/synthetic/substitute checks were summarized as green: the acceptance
record is `provenance: live` and the baseline is a real full test run.

## Post-merge package evidence

This merge touches one packable `FS.GG.UI.*` project file: `.template.package/README.md` (the US2
consumer guidance). Per the merge policy:

| field | value |
|---|---|
| package | `FS.GG.UI.Template` |
| bump | `0.1.51-preview.1` → **`0.1.52-preview.1`** (`.template.package/FS.GG.UI.Template.fsproj`) |
| change class | **doc-only** — adds the lifecycle decision tree / per-value table / standalone-`none` statement / migration note to the package README. **No `template.json`, source, or generated-output change** (FR-012). |
| local feed pack | `dotnet pack .template.package` → `~/.local/share/nuget-local/FS.GG.UI.Template.0.1.52-preview.1.nupkg` |
| sample pin alignment | **not applicable** — no sample references `FS.GG.UI.Template` as a `PackageReference`; the template is consumed via `dotnet new`, not as a package dependency. |

**Coherence caveat (disclosed):** the live Epic Acceptance Record stays pinned to and validated against
`0.1.51-preview.1` — the version whose *lifecycle behavior* was exercised live. `0.1.52-preview.1`
carries **identical lifecycle behavior** (no template/source change) plus the added consumer guidance,
so the 0.1.51 acceptance evidence remains valid for the behavior the epic closes on. Re-running the
live gate against 0.1.52 would reproduce the same verdict (guidance text is not part of the gated-set /
byte-identity / build assertions).
