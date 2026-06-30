# T030 — Consolidated quickstart validation (Scenarios A–F; SC-001…SC-006)

End-to-end run of `specs/220-game-starter-scene/quickstart.md` against the local template + local
`FS.GG.UI.* @ 0.1.53-preview.1` feed. The published package template was uninstalled so
`dotnet new fs-gg-ui` resolves the local repo template (so probe + diff compare the same source).

| Scenario | What | Result | Evidence | SC |
|---|---|---|---|---|
| A | Profile-matrix probe + FR-007 baseline (pre-change) | ✅ app 30/30, sample-pack 29/29 (`Viewer.runApp`+controls pkgs), governed 5/5; baseline snapshot captured | [profile-matrix-probe.md](./profile-matrix-probe.md), [smoke-run.md](./smoke-run.md) | — |
| B | Scaffold `game`, swap starter → Pong, `Test` | ✅ default 26/26; swap 27/27, **0** `GovernanceTests.fs` edits, no `-- pong` flag | [game-default.md](./game-default.md), [swap-to-pong.md](./swap-to-pong.md) | SC-001, SC-004 |
| C | `game` default entrypoint launches the dev's scene | ✅ live persistent `Viewer.runApp` window; `--launch-evidence` first-frame-presented=true; `--image-evidence` decodable PNG | [game-default.md](./game-default.md) | SC-002 |
| D | Swap edit set ⊆ scaffold-map classification | ✅ only `Model.fs`/`View.fs`/`BehaviorTests.fs` changed; **0** undocumented files | [edit-set-diff.md](./edit-set-diff.md) | SC-003, SC-005 |
| E | `headless-scene`/`governed`/`sample-pack` diff = empty; `app` controls still green | ✅ all three byte-identical (src+tests); `app` 30/30 | [fr007-diff.md](./fr007-diff.md), [app-controls.md](./app-controls.md) | SC-006 |
| F | Cross-repo coordination filed | ADR 0010 accepted; originating **Rendering#31** resolved; **SDD#44** (enumerate+flip app→game) + **Templates#36** (governance expectations) filed; registry PR **.github#77** (additive `game` profile); all four placed on the **Coordination** board with `Contract=fs-gg-ui-template`, sequenced w/ sibling #32 | [decisions/0010](../../../docs/product/decisions/0010-fs-gg-ui-template-default-starter.md) | FR-009 |

## No-regression baseline (T002)

The comprehensive baseline ([baseline.md](./baseline.md)) is **17/21 RED** — all the known
pre-existing NU1403 FSharp.Core lockfile failures (in-solution `src/`/`tests/` projects) + the known
`Package.Tests` Build-engine surface-baseline red. These are documented in auto-memory and are **not
220 regressions**; every generated-product lane in this feature builds against the local feed (like
the green `samples/**`) and is green.

## Verdict

SC-001 through SC-006 satisfied. The `game` profile is a runnable, replaceable game/rendering
default; the controls showcase is preserved as the explicit `app` option; the three non-interactive
profiles are provably byte-identical. **Feature 220 implementation: GREEN.**

## Readiness-evidence ledger (Feature-168 rule)

`specs/*/readiness/` is gitignored by default; the 220 allowlist was added to `.gitignore`
(`!specs/220-game-starter-scene/readiness/` + `/**`) before staging, with the bulky
`fr007-baseline/` probe trees re-ignored. `git check-ignore` proof:

- `…/readiness/quickstart-validation.md` → **not ignored** (allowlisted, committed).
- `…/readiness/fr007-baseline/` → **ignored** (transient probe snapshot, not committed).

All evidence here is real (live builds/tests/launches on `DISPLAY=:1` against the local feed); no
synthetic, degraded, or pending-review substitutions.

> **T029 version bump / republish:** the template package version derives from the release tag
> `fs-gg-ui-template/v<version>` at merge/CI time (`scripts/derive-template-version.sh`); there is no
> in-repo template-version file to edit. The coherent-preview bump + local-feed repack + push are
> performed by the merge/release flow (`speckit-merge`).
</content>
