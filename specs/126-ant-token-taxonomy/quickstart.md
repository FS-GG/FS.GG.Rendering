# Quickstart — validating the design-token taxonomy (F1)

The validation runbook for F1. It proves the taxonomy is **real** (generated, named, drift-gated) and
**invisible** (zero public surface, zero value change, byte-identical render). References
`data-model.md` (the taxonomy), `contracts/token-taxonomy-contract.md` (source schema + internal
shape), and `contracts/generation-drift-contract.md` (the checks) rather than restating them.

## Prerequisites

- .NET SDK for `net10.0`; the repo restores cleanly.
- A clean tree on branch `126-ant-token-taxonomy`.
- Baseline confidence: before starting, `dotnet build -c Release` is green and the existing suite
  passes (the oracle the move must preserve).

## V1 — The generator is real and idempotent (FR-003)

```bash
dotnet fsi scripts/generate-design-tokens.fsx          # regenerate in place
git diff --quiet -- src/DesignSystem/DesignTokensExt.fs # expect: no diff (committed == generated)
dotnet fsi scripts/generate-design-tokens.fsx --check   # expect: exit 0 (committed matches source)
```

**Expected**: the committed `DesignTokensExt.fs` equals freshly generated output; a second run produces
no diff. The file carries the `// GENERATED — do not edit` header. Confirms FR-003/FR-010.

## V2 — Zero public-surface delta (US2, FR-005)

```bash
dotnet build FS.GG.Rendering.slnx -c Debug
dotnet fsi scripts/refresh-surface-baselines.fsx
git status --porcelain tests/surface-baselines/        # expect: empty (no change)
```

**Expected**: no `tests/surface-baselines/*.txt` changes — the internal `DesignTokensExt` adds no
public surface to any package. Confirms FR-005/SC-002.

## V3 — Existing token values unchanged (US2, FR-004)

```bash
dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --filter "design-token"
```

**Expected**: `DesignTokenParityTests` green — every existing Light/Dark primitive value byte-identical,
the public `Theme` record shape unchanged. Confirms FR-004/FR-011.

## V4 — Name a token from every layer (US1, SC-001/SC-005)

```bash
dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --filter "126"
```

**Expected**: `Feature126TokenTaxonomyTests` green — a `Seed`, a `Map.Light` + `Map.Dark`, an
`Alias.Light`/`Alias.Dark`, a `Component.Button`, a `Space`, a `Density`, a `Type`, and an `Elevation`
token each resolve by name to their expected values (via `InternalsVisibleTo`); `Map`/`Alias` mode
parity holds. Confirms SC-001/SC-005/FR-006/FR-007.

## V5 — Drift gate catches a stale artifact (FR-010)

```bash
# tamper the source, then run --check (should fail until regenerated)
#   edit a value in src/Themes.Default/design-tokens.tokens.json
dotnet fsi scripts/generate-design-tokens.fsx --check    # expect: non-zero exit (stale)
dotnet fsi scripts/generate-design-tokens.fsx            # regenerate
git checkout -- src/Themes.Default/design-tokens.tokens.json src/DesignSystem/DesignTokensExt.fs
```

**Expected**: `--check` fails while the committed artifact lags the source; regenerating restores
parity. Confirms the drift gate is real (FR-010).

## V6 — Behaviour-neutral: suite + render identity (US2, SC-003)

```bash
dotnet test FS.GG.Rendering.slnx -c Release
dotnet test samples/ControlsGallery/ControlsGallery.Tests/ControlsGallery.Tests.fsproj -c Release
```

**Expected**: the full suite passes with the **same** pass/skip counts as before F1; gallery
`ThemeInvarianceTests`/`PageRenderTests` green (rendered output byte-identical). No token in the new set
is read by any render path. Confirms SC-003/FR-008.

## V7 — Build green (SC-006)

```bash
dotnet build FS.GG.Rendering.slnx -c Release   # expect: 0 warnings, 0 errors
```

## Done when

V1–V7 pass: the taxonomy is generated + drift-gated (V1/V5), nameable from every layer (V4), and
entirely invisible to the public surface, existing values, and rendered output (V2/V3/V6) — the
vocabulary every later F/D phase consumes is in place, and nothing observable changed.
