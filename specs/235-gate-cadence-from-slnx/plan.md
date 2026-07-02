# Implementation Plan: derive the gate test loop from the slnx

**Branch**: `235-gate-cadence-from-slnx` · **Spec**: [`spec.md`](./spec.md) · **Issue**: FS-GG/FS.GG.Rendering#47

## Summary

Replace `gate.yml`'s hardcoded deterministic-tier project list with a loop derived from
`FS.GG.Rendering.slnx`, sharing one `GL_TEST_PROJECTS` source of truth with the GL step. Add a
Build.Tests meta-guard asserting gate coverage == slnx test set (and doc coherence). Fix the
`fs-gg-samples` template skill's `package-pin-drift` gap (unblocking the harness parity test) and
regenerate the skill manifest. Refresh `cadence-map.md` and `validation-set.md`.

## Technical context

- **Language/build**: F# / .NET 10, Expecto tests, `dotnet test --no-build` in the gate.
- **CI**: `.github/workflows/gate.yml` (required), deterministic + GL steps.
- **Sources of truth**: `FS.GG.Rendering.slnx` (test membership); `GL_TEST_PROJECTS` (GL set);
  the meta-guard binds them.

## Constitution / governance check

- No public API surface change (Build.Tests is `IsPackable=false`; no `.fsi` touched).
- Skill-content + manifest change is coherent (manifest regenerated; Feature231 manifest test and
  Feature168 parity both re-pass).
- Cadence docs remain *derivations* of the slnx; the meta-guard makes the derivation machine-checked.

## Approach

1. **gate.yml** — job-level `env: GL_TEST_PROJECTS: "SkiaViewer Smoke"`. Deterministic step iterates
   the slnx `tests/*.Tests.fsproj`, skipping `GL_TEST_PROJECTS`. GL step iterates `$GL_TEST_PROJECTS`.
2. **fs-gg-samples skill** — rewrite the package-pin bullet to reference
   `scripts/refresh-local-feed-and-samples.fsx` and "local feed" (covers all four rule groups).
3. **skill manifest** — `dotnet fsi scripts/generate-skill-manifest.fsx` to refresh the digest.
4. **meta-guard** — `tests/Build.Tests/CadenceCoverageTests.fs` (+ fsproj `Compile` entry): parse
   slnx test set, `gate.yml` (`GL_TEST_PROJECTS` + slnx-derivation present, no hardcoded list),
   `cadence-map.md`, `validation-set.md`; assert coverage + doc coherence.
5. **docs** — refresh `cadence-map.md` §2/§3.1 and `validation-set.md` local-inner-loop list.

## Files

- `.github/workflows/gate.yml` — env + two derived loops.
- `template/fragments/samples/skill/SKILL.md` — local-feed reference.
- `template/skill-manifest/skill-manifest.json` — regenerated digest.
- `tests/Build.Tests/CadenceCoverageTests.fs` (new) + `tests/Build.Tests/Build.Tests.fsproj`.
- `docs/ci/cadence-map.md`, `docs/validation/validation-set.md` — refreshed membership.

## Risks & mitigations

- **A future GL test lands in the deterministic tier** → fails loudly headless (never silent); the
  fix is a one-line `GL_TEST_PROJECTS` add, which the meta-guard validates ⊆ slnx.
- **Manifest digest drift** → regenerated in-repo; Feature231 (release) verifies.
- **Doc parser fragility** → the meta-guard asserts targeted, robust facts (slnx names present, no
  retired names), not brittle full-table equality.

## Out of scope

See `spec.md` "Out of scope".
