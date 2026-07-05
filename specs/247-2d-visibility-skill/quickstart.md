# Quickstart: Validate the 2D Visibility Skill + Import-and-Adapt Source

Proves the spec's acceptance scenarios end-to-end on a real generated product. Run from the repo root.
Commands are illustrative of the flow; exact task steps are scheduled by `/speckit-tasks`.

## Prerequisites

- .NET `net10.0` SDK; the repo builds (`./fake.sh build -t Dev`).
- The local package feed is current (game/sample-pack products reference `FS.GG.UI.Canvas`/`.Scene`).

## A. Repo-side gates (skill + registry coherence)

```bash
# Regenerate the manifest after adding fs-gg-visibility to the catalog; must be clean.
dotnet fsi scripts/generate-skill-manifest.fsx --check

# Validate the lifecycle template (frameworkChecked bumped 15 → 16).
dotnet fsi scripts/validate-lifecycle-template.fsx

# Materialize the skill roots + wrapper and assert .claude ≡ .agents parity (0 findings).
dotnet run --project tools/Rendering.Harness -- skill-parity

# Coherence + logic tests (Package.Tests is NOT in the Deterministic slnx — run it explicitly).
dotnet test tests/Package.Tests        # Feature247VisibilitySkillTests + 231/238/204/219 count gates
dotnet test tests/Canvas.Tests         # VisibilityHelperTests
dotnet test tests/Rendering.Harness.Tests   # Deterministic gate: skill inventory / parity
```

**Expected**: manifest up to date; lifecycle-template clean; parity 0 findings;
`Feature247VisibilitySkillTests` green (manifest / template.json / parity agree; materializes only for
game/sample-pack); the `231/238/204/219` gates green with visibility added; `VisibilityHelperTests` green
(occlusion, bounded closed polygon, repeat-run byte-identity, degenerate totals).

## B. US1 — the source materializes, compiles, and is adaptable

```bash
# Scaffold a game product into a temp dir (mechanism per the template engine).
dotnet new fs-gg-ui --profile game --output /tmp/vis-demo   # illustrative
cd /tmp/vis-demo

# 1) Visibility.fs is present as product-owned source, and the skill materialized.
test -f src/*/Visibility.fs && echo "helper present"
test -f .agents/skills/fs-gg-visibility/SKILL.md && echo "skill present"

# 2) It compiles with the product.
./fake.sh build -t Build      # expected: success
```

**Expected (Acceptance 1–2)**: `Visibility.fs` exists under `src/<ProductDir>/`, is not a package
reference, compiles, and `Visibility.polygon` returns an ordered, closed `VisibilityPolygon` (not a
boolean) for a source with an occluder between it and a target region — the region behind the wall is
excluded.

## C. US1 — editing a visibility parameter changes behavior

1. Open `src/<ProductDir>/Visibility.fs` (or the call site) and change `Settings.Radius` (or cone the
   sweep to a field-of-view range, or switch the output to a per-cell mask).
2. Rebuild and run the product's visibility example (or the FSI prelude).

**Expected (Acceptance 3)**: the computed visible region changes accordingly (smaller radius → smaller
lit region; FOV cone → a wedge), with **no** framework edit and **no** added package reference.

## D. US1 — delete safety

```bash
rm src/*/Visibility.fs
./fake.sh build -t Build      # expected: STILL succeeds (Exists-guarded compile item)
./fake.sh build -t Verify     # expected: no governance/acceptance gate fails on the deletion
```

**Expected (Acceptance 4 / FR-007)**: build succeeds and no gate hard-fails solely because the helper was
removed.

## E. US2 — skill gating

```bash
# A non-visibility profile must NOT get the skill or the source.
dotnet new fs-gg-ui --profile app --output /tmp/app-demo        # illustrative
test ! -e /tmp/app-demo/.agents/skills/fs-gg-visibility && echo "skill absent (correct)"
test ! -e /tmp/app-demo/src/*/Visibility.fs && echo "source absent (correct)"
```

**Expected (US2 Acceptance 1–2 / FR-003)**: present for game/sample-pack, absent for app/headless-scene.

## F. US3 — catalog coherence & swap-guidance reach

```bash
# The visibility helper is listed as consumer-owned replaceable source.
grep -c "Visibility.fs" template/product-skills/fs-gg-model-swap/SKILL.md   # >=1
grep -c "Visibility.fs" template/base/docs/scaffold-map.md                  # >=1
```

**Expected (US3)**: `fs-gg-visibility` is registered coherently (0 drift) and the helper appears in the
scaffold's swap/adapt file taxonomy as consumer-owned replaceable source, next to `Collision.fs`.

## Success criteria mapping

| Quickstart step | Spec criterion |
|-----------------|----------------|
| B, C | SC-001 (working visibility by editing only the helper), SC-005 (reuse, no re-rolled types) |
| B | SC-002 (one dedicated skill, Red Blob Games–cited) |
| D | SC-003 (edit/delete without gate failure) |
| A (VisibilityHelperTests) | SC-004 (replay-deterministic polygon), SC-008 (degenerate totals) |
| E | SC-006 (gating) |
| A (Feature247 + 231/238/204/219 gates) | SC-007 (registry coherence) |
| F | SC-002, SC-007 (swap-guidance reach + coherence) |
