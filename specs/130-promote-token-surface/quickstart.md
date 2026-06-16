# Quickstart — Validating F5 (Public Token/Resolver Surface)

Runbook proving the promotion landed correctly and neutrally. Headless; no GL context required.
Prerequisites: .NET `net10.0` SDK; repo on branch `130-promote-token-surface`; `dotnet` on PATH.

References: [public-surface-contract](./contracts/public-surface-contract.md),
[gate-neutrality-contract](./contracts/gate-neutrality-contract.md), [data-model](./data-model.md).

## V1 — Build is clean (Principle: warning-free Tier-1 landing)

```sh
dotnet build -c Release FS.GG.Rendering.slnx
```

Expected: 0 warnings, 0 errors. Confirms the two new `.fsi` files match their `.fs`, the removed `internal`
modifiers compile, and the removed `InternalsVisibleTo` grants didn't break any consumer. (→ INV-6, INV-7)

## V2 — Token generator drift is green over BOTH files (INV-2)

```sh
dotnet fsi scripts/generate-design-tokens.fsx --check
```

Expected: reports no drift for `DesignTokensExt.fs` AND `DesignTokensExt.fsi` (committed == generated). Then
confirm values are byte-identical aside from the dropped `internal`:

```sh
git diff -- src/DesignSystem/DesignTokensExt.fs   # only the `module` line loses `internal`; values unchanged
```

## V3 — Surface-baseline delta is exactly the promoted symbols (INV-1)

```sh
dotnet fsi scripts/refresh-surface-baselines.fsx
git diff --stat tests/surface-baselines/
git diff tests/surface-baselines/FS.GG.UI.DesignSystem.txt
```

Expected: ONLY `FS.GG.UI.DesignSystem.txt` changes; the diff is additions only — `StyleResolver`,
`StyleResolver+IntentPolicy`, `DesignTokensExt`, and the `DesignTokensExt+<Sub>` nested-module type rows. No other
baseline file appears in `--stat`. (Verify by diff, not exit code — the script always rewrites.)

## V4 — Public-path consumption, value parity, render neutrality, divergence (INV-3/4/5)

```sh
dotnet test FS.GG.Rendering.slnx --filter "130"
```

Expected: `Feature130PublicSurfaceTests` green —
- compiles & calls `StyleResolver.*` and reads `DesignTokensExt.*` with **no** IVT grant (public access);
- representative promoted token values equal their known literals (value parity);
- `resolveDefault` output byte-identical to the Feature129 neutral oracle across the cross-product;
- a divergent `IntentPolicy` ("danger" → `theme.Danger`) yields a style differing from `resolveDefault`, with no
  control edits.

## V5 — Pre-existing suites still green against the now-public surface (INV-7)

```sh
dotnet test FS.GG.Rendering.slnx --filter "126"
dotnet test FS.GG.Rendering.slnx --filter "129"
```

Expected: both green unchanged — they now reach the promoted symbols publicly (grants removed). No assertion edits
required beyond compilation.

## V6 — Full suite & render identity (INV-3, INV-7)

```sh
dotnet test FS.GG.Rendering.slnx
```

Expected: 0 failures; pass/skip counts equal the pre-F5 baseline **plus** the additive Feature130 tests; gallery /
render-identity suites unchanged (rendered output byte-identical under the neutral policy).

## V7 — Decision record present and two-way consistent (INV-1, INV-8, SC-004)

Open `docs/product/decisions/0004-public-token-resolver-surface.md` and confirm it:
- enumerates the promoted surface (StyleResolver members + the DesignTokensExt taxonomy) and agrees with the V3
  baseline diff (every added row accounted for; no promised symbol missing);
- records `ColorPolicy` as deliberately deferred, with rationale;
- records the stability/reversibility commitment and the deferred `DesignTokens` unification (R4).

## Done when

All of V1–V7 pass: clean build, generator drift green over both files, baseline delta == promoted symbols only,
public-path/parity/neutrality/divergence tests green, prior suites green, full suite 0 failures, decision record
present and consistent.
