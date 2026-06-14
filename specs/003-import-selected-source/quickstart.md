# Quickstart / Validation: Import Selected Source (Stage R4)

Unlike R2/R3, this feature is validated by **building and testing real code**, not review.

## Prerequisites

- .NET `net10.0` SDK; the dev container provides hardware GL (R3 capability baseline) for
  GL unit/smoke tests.
- Source available at `/home/developer/projects/FS-Skia-UI` (commit `f759f399`) for the import.
- R2/R3 outputs: `docs/product/module-map.md`, `docs/product/layering.md`,
  `docs/validation/validation-set.md`.

## Validation scenarios

### V1 — Solution builds from a fresh checkout (SC-001)

```bash
dotnet build FS.GG.Rendering.sln -c Release
```
**Pass** if it restores and compiles with all 10 runtime modules present, on `net10.0`.

### V2 — Default local test tier passes (SC-002)

```bash
# the local-frequency projects from docs/validation/validation-set.md
dotnet test tests/Color.Tests tests/Scene.Tests tests/Layout.Tests \
            tests/Input.Tests tests/KeyboardInput.Tests tests/Elmish.Tests \
            tests/Controls.Tests tests/Testing.Tests tests/SkiaViewer.Tests \
            tests/Smoke.Tests
```
**Pass** if green; any skip carries a written rationale (Principle V).

### V3 — No governance / no Vulkan / identity preserved (SC-003/005/007)

```bash
grep -rIl -E 'SkillSupport|GRVkBackend|Silk\.NET\.Vulkan' src tests   # expect: no matches
ls src/SkillSupport tests/Governance.Tests tests/SkillSupport.Tests 2>/dev/null  # expect: absent
grep -rE 'FS\.Skia\.UI' Directory.Build.props **/*.fsproj | head      # identity preserved
```
**Pass** if no governance/Vulkan refs, excluded projects absent, identity intact.

### V4 — `.fsi`/baseline compliance (SC-004)

```bash
# every public module has a .fsi; no .fs top-level access modifiers
grep -rnE '^(let|type|module)[[:space:]]+(private|internal|public)\b' src --include=*.fs   # expect: none
```
**Pass** if no access modifiers leak into `.fs`, and each module has a `.fsi` + surface baseline.

### V5 — Provenance present (SC-006)

Open `PROVENANCE.md`; confirm source repo + commit `f759f399` + path map + adaptations.

## Done when

- V1–V5 pass; `checklists/requirements.md` items remain satisfied.
- See [`contracts/build-and-test-contract.md`](./contracts/build-and-test-contract.md) for the
  full acceptance contract.
