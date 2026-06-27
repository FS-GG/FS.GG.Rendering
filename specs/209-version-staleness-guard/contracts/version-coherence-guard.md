# Contract — Version Coherence Guard

The guard's "interface" is its CLI/exit-code behavior, its failure-message schema, and the gate-step
wiring. This is the surface a maintainer and CI depend on (Constitution Principle I — the honest
audience for a machinery feature is the command line + CI lane, not an `.fsi`).

## 1. Script CLI contract — `scripts/validate-version-coherence.fsx`

**Invocation**
```
dotnet fsi scripts/validate-version-coherence.fsx
FS_GG_RUN_VERSION_COHERENCE_SMOKE=1 dotnet fsi scripts/validate-version-coherence.fsx
```

**Layers** (mirrors `validate-bom-consumer.fsx`)
| Layer | When | Cost | Proves |
|-------|------|------|--------|
| Structural verdict-core | always (env-free) | text + `git tag` only | the Lockstep Invariant minus RestoreProof |
| Restore-grounded proof | `FS_GG_RUN_VERSION_COHERENCE_SMOKE=1` | one Release pack + one clean restore | RestoreProof (FR-008) |

**Exit codes**
| Code | Meaning |
|------|---------|
| `0` | Coherent — every invariant conjunct holds (for the layers that ran). |
| `1` | Drift — ≥1 conjunct false. stderr lists each failure; the readiness report is still written. |
| `2` | Guard error — inputs unreadable (missing props/nuspec), tags not fetched, pack/restore tooling failed. Fails **closed** (never reported as coherent). |

**Inputs** (read-only): `template/base/Directory.Packages.props`, `src/Meta/FS.GG.UI.nuspec`,
`src/**/*.fsproj`, `template/base/build.fsx`, `git tag --list 'fs-gg-ui/v*'`. Throwaway pack feed
under the system temp dir for the live layer. Does **not** read `Directory.Build.props` `<Version>`
(decoupled, D5).

**Output**: a `Verdict` summarized to stdout/stderr + a regenerated report at
`specs/209-version-staleness-guard/readiness/version-coherence.md` with `provenance:
verdict-core | live`.

## 2. Failure-message schema (FR-007)

Every failure names the location with expected-vs-actual — **never** a bare "incoherent":
```
DRIFT [<rule-id>] <location>
  expected: <value-or-relation>
  actual:   <value>
  fix:      <the unambiguous corrective edit>
```

Rule ids and example messages:
| rule-id | Trigger | Example `expected` / `actual` |
|---------|---------|-------------------------------|
| `pin-lags-tag` | FsGgUiVersion < latest tag (204) | expected `>= 0.1.51-preview.1` (latest `fs-gg-ui/v…`); actual `0.1.50-preview.1` |
| `pin-no-tag` | FsGgUiVersion has no `fs-gg-ui/v<V>` tag (phantom) | expected a tag `fs-gg-ui/v0.1.99-preview.1`; actual none |
| `bom-member-skew` | `B.ids != P.members` | expected `{…16…}`; actual missing `FS.GG.UI.Foo` / extra `FS.GG.UI.Bar` |
| `bom-pin-not-token` | a BOM dep version != `[$version$]` | expected `[$version$]`; actual `[0.1.50-preview.1]` |
| `template-pin-hardcoded` | a template pin not `$(FsGgUiVersion)` | expected `$(FsGgUiVersion)`; actual `0.1.50-preview.1` |
| `template-consumed-skew` | `T.pins != T.expected` | expected the 11-member consumed manifest; actual missing/extra |
| `single-source-not-unique` | `occurrences != 1` | expected `1` `<FsGgUiVersion>`; actual `2` |
| `runtime-regex-broken` | `build.fsx` regex no longer matches | expected a match for `<FsGgUiVersion>…`; actual none |
| `restore-partial` | live: a member did not resolve to `V` | expected all members `@0.1.51-preview.1`; actual `FS.GG.UI.Scene @0.1.50-preview.1` |

## 3. Gate-step contract — `.github/workflows/gate.yml`

- **Checkout MUST fetch tags**: `actions/checkout@v4` with `fetch-depth: 0` (or `fetch-tags: true`) —
  without it `git tag` is empty and the guard fails closed with exit `2` (D2).
- **New merge-blocking step** "Version coherence guard": runs the structural verdict-core (always)
  and the scoped restore-grounded proof (`FS_GG_RUN_VERSION_COHERENCE_SMOKE=1`). Non-zero exit fails
  the gate ⇒ PR cannot merge to `main` (FR-006, SC-001/002/004). Placed alongside the existing
  surface-baseline-drift step (the established "regenerate-and-fail-on-drift" gate pattern).
- **Step summary**: on failure, echo the `DRIFT […]` lines to `$GITHUB_STEP_SUMMARY` so the reviewer
  sees the named location without opening logs (SC-006).
- **Policy independence (FR-004)**: the verdict compares the BOM token/bracket and pins directly; it
  does **not** depend on `WarningsAsErrors=NU1605;NU1608`. (Contrast: `validate-bom-consumer.fsx`'s
  consumer-loudness layer *does* depend on that policy — that layer proves consumer behavior, not the
  in-repo gate.)

## 4. Release-lane relationship (`.github/workflows/release.yml`)

The deeper **full generate→restore→build of a product from the template** (all profiles) remains the
release lane's existing Package.Tests / product-from-template responsibility. The guard does not move
or duplicate it; the gate's scoped restore is the minimum real-restore that grounds FR-008 for every
PR (D4).

## 5. Cross-repo contract (FR-010, D8)

Upholds — does not modify — the `fs-gg-ui-version` and `fs-gg-ui-bom` registry rows. A note/ADR in
`FS-GG/.github` (via the `cross-repo-coordination` skill) records that drift is now structurally
caught by this repo's gate before merge. Recorded **after** in-repo verification passes (208 ordering).

## Acceptance mapping

| Spec acceptance | Verified by |
|-----------------|-------------|
| US1 #1 (no snapshot) | `pin-no-tag` |
| US1 #2 (204 lag, expected-vs-actual) | `pin-lags-tag` |
| US1 #3 (all lockstep passes) | exit `0`, report `result: pass` |
| US1 #4 (PR blocked) | gate step non-zero exit |
| US2 #2 (BOM half-bump, policy-independent) | `bom-pin-not-token` / direct compare, no warnings-as-errors |
| US2 #3 (unwired new member) | `bom-member-skew` / `template-consumed-skew` |
| US3 #1/#2 (partial/undefined restore fails) | `restore-partial` (live layer) / exit `2` on undefined property |
