# Quickstart: Validate the Grid-Parts Skill + Import-and-Adapt Source

Proves the spec's acceptance scenarios end-to-end on a real generated product. Run from the repo root.
Commands are illustrative of the flow; exact task steps are scheduled by `/speckit-tasks`.

## Prerequisites

- .NET `net10.0` SDK; the repo builds (`./fake.sh build -t Dev`).
- The local package feed is current (game/sample-pack products reference `FS.GG.UI.Canvas`/`.Scene`).

## A. Repo-side gates (skill + registry coherence)

```bash
# Regenerate the manifest after adding fs-gg-grids to the catalog; must be clean.
dotnet fsi scripts/generate-skill-manifest.fsx --check

# Validate the lifecycle template (frameworkChecked bumped 16 → 17).
dotnet fsi scripts/validate-lifecycle-template.fsx

# Materialize the skill roots + wrapper and assert .claude ≡ .agents parity (0 findings).
dotnet run --project tools/Rendering.Harness -- skill-parity

# Coherence + logic tests (Package.Tests is NOT in the Deterministic slnx — run it explicitly).
dotnet test tests/Package.Tests        # Feature249GridsSkillTests + 231/238/204/219 count gates
dotnet test tests/Canvas.Tests         # GridsHelperTests
dotnet test tests/Rendering.Harness.Tests   # Deterministic gate: skill inventory / parity
```

**Expected**: manifest up to date; lifecycle-template clean; parity 0 findings;
`Feature249GridsSkillTests` green (manifest / template.json / parity agree; materializes only for
game/sample-pack); the `231/238/204/219` gates green with grids added; `GridsHelperTests` green
(adjacency round-trip, pixel round-trip, repeat-run byte-identity, degenerate totals).

## B. US1 — the source materializes, compiles, and is adaptable

```bash
# Scaffold a game product into a temp dir (mechanism per the template engine).
dotnet new fs-gg-ui --profile game --productName Grids249 -o /tmp/grids-demo   # illustrative
cd /tmp/grids-demo

# 1) Grids.fs is present as product-owned source (namespace rewritten), and the skill materialized.
test -f src/Grids249/Grids.fs && echo "helper present"
test -f .agents/skills/fs-gg-grids/SKILL.md && echo "skill present"

# 2) It compiles with the product.
./fake.sh build -t Build      # expected: success
```

**Expected (Acceptance 1–2)**: `Grids.fs` exists under `src/<ProductDir>/` (namespace rewritten to the
product name), is not a package reference, compiles, and — feeding a `Cell` to `Grids.cellEdges` /
`Grids.cellCorners` — returns four edges and four corners in the documented order; taking one of those
edges, `Grids.edgeCells` returns the two faces it separates, one of which is the original cell (the
adjacency relationships round-trip).

## C. US1 — editing a grid-parts parameter changes behavior

1. Open `src/<ProductDir>/Grids.fs` (or the call site) and change the `GridSpec` — e.g. move `Origin` or
   change `CellSize` (or add a diagonal-edge variant, reorder the corners, or extend toward hex).
2. Rebuild and run the product's grid example (or the FSI prelude); observe the pixel positions.

**Expected (Acceptance 4)**: the computed pixel positions change accordingly (bigger `CellSize` → a larger
cell rect; a shifted `Origin` → the whole grid moves), with **no** framework edit and **no** added package
reference.

## D. US1 — delete safety

```bash
rm src/*/Grids.fs
./fake.sh build -t Build      # expected: STILL succeeds (Exists-guarded compile item)
./fake.sh build -t Verify     # expected: no governance/acceptance gate fails on the deletion
```

**Expected (Acceptance 5 / FR-007)**: build succeeds and no gate hard-fails solely because the helper was
removed.

## E. US2 — skill gating

```bash
# A non-grids profile must NOT get the skill or the source.
dotnet new fs-gg-ui --profile app --productName AppDemo -o /tmp/app-demo        # illustrative
test ! -e /tmp/app-demo/.agents/skills/fs-gg-grids && echo "skill absent (correct)"
test ! -e /tmp/app-demo/src/*/Grids.fs && echo "source absent (correct)"
```

**Expected (US2 Acceptance 1–2 / FR-003)**: present for game/sample-pack, absent for app/headless-scene.

## F. US3 — catalog coherence & swap-guidance reach

```bash
# The grid-parts helper is listed as consumer-owned replaceable source.
grep -c "Grids.fs" template/product-skills/fs-gg-model-swap/SKILL.md   # >=1
grep -c "Grids.fs" template/base/docs/scaffold-map.md                  # >=1
```

**Expected (US3)**: `fs-gg-grids` is registered coherently (0 drift) and the helper appears in the
scaffold's swap/adapt file taxonomy as consumer-owned replaceable source, next to `Collision.fs` and
`Visibility.fs`.

## Success criteria mapping

| Quickstart step | Spec criterion |
|-----------------|----------------|
| B, C | SC-001 (working grid-parts result by editing only the helper), SC-005 (reuse, no re-rolled Cell/Point/Rect types) |
| B | SC-002 (one dedicated skill, Red Blob Games–cited ×2) |
| D | SC-003 (edit/delete without gate failure) |
| A (GridsHelperTests) | SC-004 (replay-deterministic parts + adjacency round-trip), SC-008 (degenerate totals) |
| E | SC-006 (gating) |
| A (Feature249 + 231/238/204/219 gates) | SC-007 (registry coherence) |
| F | SC-002, SC-007 (swap-guidance reach + coherence) |
