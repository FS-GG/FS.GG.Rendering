# T021 — Out-of-scope invariants held (FR-007 Tier-2 guarantee)

## Change surface (tracked, non-lock)

```
.template.config/template.json                       # T007: 9 .claude/skills/ conditions +spec-kit
scripts/validate-lifecycle-template.fsx              # T015: classifier + floors + report lines
tests/Package.Tests/Feature204LifecycleTemplateTests.fs   # T014: classifier + floors + report string + GV-4/5
tests/Package.Tests/Feature219EmitFrameworkSkillsTests.fs # T013: G-EMIT surface-specific
.gitignore                                           # readiness allowlist for specs/228 (Feature 168 rule)
specs/228-fix-scaffold-skill-leak/**                 # spec + readiness evidence
.specify/feature.json, CLAUDE.md                     # speckit workflow pointers (feature setup)
```

`git diff --stat` of the 4 code touch-points: 4 files, +88 / −31.

## Tier-2 guards (all confirmed)

| Guard | Result |
|---|---|
| `src/**` files touched | **0** |
| `.fsi` files touched | **0** |
| version / `.nuspec` / `Directory.*.props` changed | **0** (template `.fsproj` stays `0.1.58-preview.1`) |
| dependency added | **0** |
| `.codex/skills/` sources in `template.json` | **0** (never written — unchanged) |
| `docs/reports/skills-parity.md` regenerated | **no** (no skill added/removed) |
| Feature 224 / 225 gates edited | **no** (untouched; the `template/product-skills/` sources still resolve) |

## Provider surface never shrank

Every live observation checks `.agents/skills/` == S(profile); no assertion over `.agents/skills/`
membership was loosened. The emission invariant was made **more** precise (surface-specific), not looser
(Constitution V) — see [agents-tree-intact.md](./agents-tree-intact.md).

## `git check-ignore` proof (Feature 168)

`specs/*/readiness/` is gitignored by default; the `.gitignore` allowlist entry
`!specs/228-fix-scaffold-skill-leak/readiness/` + `/**` was added **before** staging. Post-allowlist
`git check-ignore -q` returns non-zero (not ignored) for the readiness files — they are now trackable:
`env.md`, `leak-surface-map.md`, `gate-transcripts.md`, `success-criteria.md` all `tracked-ok`.

## Environment caveats (disclosed, not summarized as green)

- **NU1403 lock-file hash mismatch (pre-existing, environment).** At session start every in-solution
  test project failed to restore with `NU1403: content hash validation failed for FSharp.Core.10.1.301`
  (the locally-available package differs from the committed `packages.lock.json` hashes). Cleared for the
  gate run via `dotnet restore --force-evaluate`. That recomputes lock hashes in ~12 `packages.lock.json`
  files — **environment-only churn, reverted before commit** (outside this feature's Tier-2 surface). The
  committed lock files are unchanged. See [baseline.md](./baseline.md) (before) / [baseline-after.md](./baseline-after.md).
- **T010 end-to-end SDD scaffold — environment-limited** (no rendering-provider registration provisioned
  here); disclosed substitute in [success-criteria.md](./success-criteria.md).
