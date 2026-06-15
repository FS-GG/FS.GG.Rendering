# Quickstart — Validate the Visual-State Style Layer (Feature 093)

A run/validation guide for the **backfill conformance pass**: confirm the four shipped suites are
green, the frozen-oracle parity matches, and the public-surface baseline is unchanged. No
implementation steps — the code already ships.

## Prerequisites

- .NET `net10.0` SDK (per `Directory.Build.props`).
- Repo restored/built once: `dotnet build src/Controls/Controls.fsproj`.
- No GL context required — every 093 proof is deterministic and headless.

## 1. Run the four semantic suites

From the repo root:

```bash
# All Feature 093 suites at once
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "FullyQualifiedName~Feature093"
```

Or one at a time:

```bash
# SC-001 / SC-002 — variant distinctness, precedence, class override
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "FullyQualifiedName~Feature093StyleResolver"

# SC-004 — purity, determinism, outermost state over ≥1000 generated inputs (FsCheck, Gen093)
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "FullyQualifiedName~Feature093StyleProperty"

# SC-003 / SC-007 — frozen-oracle byte parity for Button/CheckBox + unmigrated no-delta
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "FullyQualifiedName~Feature093Parity"

# SC-005 — state-driven paint survives a position-shifting re-render via the live retained path
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "FullyQualifiedName~Feature093RetainedState"
```

**Expected**: all green. The property suite reports ≥1000 cases per property.

## 2. Inspect the frozen-oracle parity evidence

`Feature093ParityTests` writes six structural-scene artifacts:

```bash
ls specs/093-visual-state-style-layer/readiness/parity/
# button.light.normal.scene.txt        button.dark.normal.scene.txt
# check-box.light.normal.scene.txt     check-box.dark.normal.scene.txt
# check-box-checked.light.normal.scene.txt   check-box-checked.dark.normal.scene.txt
```

Each is the resolver-driven scene, asserted byte-equal to a frozen inline reproduction of the
pre-refactor `buttonGeom`/`checkboxGeom` geometry. **Disclosed limitation**: this is structural scene
equality, not a pixel or desktop-visibility proof.

## 3. Confirm zero public-surface delta

```bash
# The surface-drift check must pass unchanged — Style, the five styling types, and the
# StyleClassesValue/VisualStateValue carriers are already committed.
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "FullyQualifiedName~Surface"
grep -E "Style|VisualState|StyleVariant|StyleClass|ResolvedStyle|ValidationState" \
    tests/surface-baselines/FS.GG.UI.Controls.txt
```

**Expected**: no baseline diff; the styling surface is present.

## 4. Sanity-check the resolver in FSI (optional)

The honest FSI exercise from Principle I — resolve the identity case and a precedence case:

```fsharp
open FS.GG.UI.Controls
let theme = Theme.light                       // active DTCG palette
let baseStyle : ResolvedStyle = (* a migrated kind's default *) ...

// C-4 identity: no class, Normal => base, exactly
Style.resolve theme baseStyle [] Normal = baseStyle      // val it : bool = true

// C-3 precedence: Disabled (state) wins over Primary (class) on a shared field
let r = Style.resolve theme baseStyle [ Variant StyleVariant.Primary ] Disabled
// r.Fill is the Disabled (muted) token, not the Primary accent

// C-5 unknown Custom => identity delta
Style.resolve theme baseStyle [ Custom "no-such-class" ] Normal = baseStyle   // true
```

## Mapping: success criteria → evidence

| Criterion | Where validated |
|---|---|
| SC-001 (variant distinctness, accent/danger families, Custom flow) | `Feature093StyleResolverTests` |
| SC-002 (state distinctness, `Loading==Normal`, state-over-class, later-class-wins) | `Feature093StyleResolverTests` |
| SC-003 (Button/CheckBox frozen-oracle parity, deterministic) | `Feature093ParityTests` + `readiness/parity/` |
| SC-004 (purity/determinism/outermost-state, ≥1000 cases, identity) | `Feature093StylePropertyTests` |
| SC-005 (state survives position-shifting re-render) | `Feature093RetainedStateTests` |
| SC-006 (colours trace to tokens; theme swap re-paints) | `Feature093ParityTests` (both themes) + code review |
| SC-007 (unmigrated kind: no render delta) | `Feature093ParityTests` |

See [contracts/style-resolver.md](./contracts/style-resolver.md) for the full behavioral contract and
[data-model.md](./data-model.md) for the styling vocabulary and fold semantics.
