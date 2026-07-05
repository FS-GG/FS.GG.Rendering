# Quickstart: Validate the Collision Skill + Import-and-Adapt Source

Proves the spec's acceptance scenarios end-to-end on a real generated product. Run from the repo root.
Commands are illustrative of the flow; exact task steps are scheduled by `/speckit-tasks`.

## Prerequisites

- .NET `net10.0` SDK; the repo builds (`./fake.sh build -t Dev`).
- The local package feed is current (game/sample-pack products reference `FS.GG.UI.Canvas`/`.Scene`).

## A. Repo-side gates (skill + registry coherence)

```bash
# Regenerate the manifest after adding fs-gg-collision to the catalog; must be clean.
dotnet fsi scripts/generate-skill-manifest.fsx --check

# Materialize the skill roots and assert .claude ≡ .agents parity.
dotnet fsi scripts/check-agent-skill-parity.fsx

# Coherence + logic tests.
./fake.sh build -t Test    # runs Feature246CollisionSkillTests + CollisionHelperTests
```

**Expected**: manifest up to date; parity clean; `Feature246CollisionSkillTests` green (manifest /
template.json / skillist / parity agree; materializes only for game/sample-pack; game-core trimmed);
`CollisionHelperTests` green (overlap→separation, repeat-run byte-identity, degenerate totals).

## B. US1 — the source materializes, compiles, and is adaptable

```bash
# Scaffold a game product into a temp dir (mechanism per the template engine).
dotnet new fs-gg-ui --profile game --output /tmp/collide-demo   # illustrative
cd /tmp/collide-demo

# 1) Collision.fs is present as product-owned source, and the skill materialized.
test -f src/*/Collision.fs && echo "helper present"
test -f .agents/skills/fs-gg-collision/SKILL.md && echo "skill present"

# 2) It compiles with the product.
./fake.sh build -t Build      # expected: success
```

**Expected (Acceptance 1–2)**: `Collision.fs` exists under `src/<ProductDir>/`, is not a package
reference, compiles, and `Collision.step`/`collide` returns contacts **and** resolutions (not a boolean)
for two overlapping bodies.

## C. US1 — editing the response rule changes behavior

1. Open `src/<ProductDir>/Collision.fs`, change the rule passed to `resolve`/`step` from
   `SeparateEqually` to `PushFirst` (or a `Bounce` restitution).
2. Rebuild and run the product's collision example (or the FSI prelude).

**Expected (Acceptance 3)**: overlapping bodies separate differently (e.g. one body immovable), with
**no** framework edit and **no** added package reference.

## D. US1 — delete safety

```bash
rm src/*/Collision.fs
./fake.sh build -t Build      # expected: STILL succeeds (Exists-guarded compile item)
./fake.sh build -t Verify     # expected: no governance/acceptance gate fails on the deletion
```

**Expected (Acceptance 4 / FR-007)**: build succeeds and no gate hard-fails solely because the helper
was removed.

## E. US2 — skill gating

```bash
# A non-collision profile must NOT get the skill or the source.
dotnet new fs-gg-ui --profile app --output /tmp/app-demo        # illustrative
test ! -e /tmp/app-demo/.agents/skills/fs-gg-collision && echo "skill absent (correct)"
test ! -e /tmp/app-demo/src/*/Collision.fs && echo "source absent (correct)"
```

**Expected (US2 Acceptance 1–2 / FR-003)**: present for game/sample-pack, absent for app/headless-scene.

## F. US3 — one source of truth

```bash
grep -c "fs-gg-collision" template/product-skills/fs-gg-game-core/SKILL.md   # ≥1 (the pointer)
```

**Expected (US3)**: `fs-gg-game-core`'s `## Collision` is a pointer to `fs-gg-collision`; the detailed
detection/broad-phase/response guidance lives only in the new skill.

## Success criteria mapping

| Quickstart step | Spec criterion |
|-----------------|----------------|
| B, C | SC-001 (working response by editing only the helper), SC-005 (reuse, no re-rolled types) |
| F | SC-002 (one dedicated skill; no duplication) |
| D | SC-003 (edit/delete without gate failure) |
| A (CollisionHelperTests) | SC-004 (replay-deterministic) |
| E | SC-006 (gating) |
| A (Feature246 test) | SC-007 (registry coherence) |
