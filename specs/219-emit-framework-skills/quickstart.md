# Quickstart: validate framework-skill emission across lifecycles

Runnable validation that proves the feature end-to-end. This is a **validation/run guide**, not the implementation — the edits live in `tasks.md`. Run from the repo root.

## Prerequisites

- .NET SDK `net10.0` band (`global.json` rollForward latestFeature).
- The local template is instantiated from this checkout (the Feature 204 validator builds a temp template package, mirroring `validate-lifecycle-template.fsx`).
- No GL/window-system needed — this is instantiation only.

## Scenario 1 — Env-free gate (fast, deterministic, CI default)

Proves the 3-category gating is correct without any `dotnet new`.

```sh
# verdict-core only (no report write): fails loud if any source is mis-gated
dotnet fsi scripts/validate-lifecycle-template.fsx

# + write the report the Feature 204 / 219 tests assert
dotnet fsi scripts/validate-lifecycle-template.fsx --emit-report
```

**Expected**: prints `verdict-core OK: …; N lifecycle-workspace sources carry lifecycle == "spec-kit"; M framework product-skill sources profile-gated & lifecycle-independent; K product sources clean`. The report records `gated-condition: lifecycle-workspace sources carry lifecycle == "spec-kit"; framework product-skill sources are profile-gated and lifecycle-independent`.

## Scenario 2 — Live `dotnet new` matrix (the real proof)

Proves G-EMIT, G-NOREG, G-WORKSPACE against a real instantiation across 3 lifecycles × 4 profiles.

```sh
FS_GG_RUN_LIFECYCLE_VALIDATION=1 dotnet fsi scripts/validate-lifecycle-template.fsx
```

**Expected report lines** (per profile `P ∈ {app, headless-scene, governed, sample-pack}`):
- `spec-kit/P: generate=pass diff-vs-today=none` — **no regression** (G-NOREG / SC-003).
- `sdd/P: generate=pass gated-absent=ok product-present=ok diff-vs-default=gated-only` — workspace suppressed, **framework skills present** as product.
- `none/P: generate=pass gated-absent=ok product-present=ok` — same as sdd.
- `sdd/P: framework-skills-present=ok (<n> SKILL.md)` / `none/P: …` — the positive fact.
- `catalog-dangling: none`, `symbology: wired (scene-profiles)` (or `not-vendored`), `result: pass`.

## Scenario 3 — The original bug, reproduced and fixed (by hand)

The exact thing the consumer agent hit (issue #30): scaffold on the recommended SDD path and count skills.

```sh
# instantiate an app product on the SDD path into a temp dir (uses the locally packed template)
dotnet new fs-gg-ui --name Demo --profile app --lifecycle sdd -o /tmp/demo-sdd --allow-scripts yes
find /tmp/demo-sdd -name SKILL.md | sort
```

**Before this feature**: `find` returns **0** results (the bug).
**After this feature**: `find` returns the `app`-profile framework skills under both `.agents/skills/` and `.claude/skills/`, e.g. `.agents/skills/fs-gg-scene/SKILL.md`, `…/fs-gg-skiaviewer/…`, `…/fs-gg-elmish/…`, `…/fs-gg-keyboard-input/…`, `…/fs-gg-ui-widgets/…`, `…/fs-gg-symbology/…` (SC-001: 0 → profile count).

```sh
# the lifecycle workspace is still correctly absent under sdd (FR-003)
test ! -d /tmp/demo-sdd/.specify && echo ".specify absent (ok)"
find /tmp/demo-sdd -name 'speckit-*' -o -name 'CLAUDE.md' | sort   # expect empty
# the catalog does not dangle (FR-006): not emitted under sdd
test ! -f /tmp/demo-sdd/docs/skillist-reference.md && echo "catalog absent under sdd (ok)"
```

## Scenario 4 — Spec-Kit path unchanged (regression guard, by hand)

```sh
dotnet new fs-gg-ui --name Demo --profile app --lifecycle spec-kit -o /tmp/demo-speckit --allow-scripts yes
# framework skills AND the lifecycle workspace AND the catalog all present, exactly as before
find /tmp/demo-speckit -name SKILL.md | wc -l        # full set (framework + authoring + speckit-*)
test -d /tmp/demo-speckit/.specify && echo ".specify present (ok)"
test -f /tmp/demo-speckit/docs/skillist-reference.md && echo "catalog present under spec-kit (ok)"
```

The authoritative byte-identical proof is Scenario 2's `spec-kit/P: diff-vs-today=none`; this scenario is the human-eyeball version.

## Scenario 5 — Run the gates as the test suite

```sh
# Feature 204 (amended) + Feature 219 (new) gates via the package test project
dotnet test tests/Package.Tests
# heavy live proof behind the env flag (mirrors the validator)
FS_GG_RUN_LIFECYCLE_VALIDATION=1 dotnet test tests/Package.Tests
```

**Expected**: Feature204 GV-1..GV-8 green (with GV-2 on the 3-category counts, GV-4/GV-5 with framework-skills-present), Feature219 positive assertions green.

## Done signals

- Scenario 2 report: `result: pass`, all `spec-kit/* diff-vs-today=none`, all `sdd|none/* product-present=ok` with `framework-skills-present=ok`.
- Scenario 3: `find … SKILL.md` non-empty under `sdd` (and `none`).
- Registry `fs-gg-ui-template.parameters.lifecycle.notes` updated (FR-009); `FS-GG/FS.GG.Rendering#30` closed and its board item `Done`.
