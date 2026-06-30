# Quickstart — validate the replaceable game starter

Runnable validation scenarios that prove the feature end to end. Details of the contract and the
edit set live in [contracts/fs-gg-ui-template-contract.md](./contracts/fs-gg-ui-template-contract.md)
and [data-model.md](./data-model.md) — this is the run guide, not the implementation.

> Use the repo's normal template pack/instantiate tooling and the generated product's
> `build.sh`/`fake.sh` (`Dev` / `Test` / `Verify`). The NU1403 FSharp.Core lockfile workaround in
> auto-memory applies if a restore is blocked.

## Prerequisites
- Local NuGet feed populated (`~/.local/share/nuget-local/`) with a coherent `FS.GG.UI.*` set.
- Template packed/installed so `game`, `app`, `headless-scene`, `governed`, `sample-pack`
  profiles can be instantiated.

## Scenario A — Foundational profile-matrix probe (run BEFORE authoring game branches)
Confirms the plan's reachability hypotheses (research.md Decision 2) and captures the FR-007
byte-diff baseline.

1. Instantiate each profile into a scratch dir: `app`, `headless-scene`, `governed`,
   `sample-pack` (pre-change), and `game` (once it exists).
2. For each: run `Test` (build + `Product.Tests`) and record pass/fail + the generated
   `Program.fs` default-launch branch and `Product.fsproj` package set.
3. Snapshot `headless-scene`/`governed`/`sample-pack` output as the **diff baseline**.

**Expected**: existing profiles green; `sample-pack` default branch shows
`Viewer.runApp viewerOptions generatedHost` and the controls package set (confirms the
`game || sample-pack` launch grouping is safe). If not, adjust pinning per research.md.

## Scenario B — SC-001 / SC-004: swap the game starter to Pong (the headline journey)
1. Scaffold the **game** default: instantiate the `game` profile → product launches the **minimal
   Pong-style skeleton** at the normal entrypoint (no flags).
2. Run `Test` on the unmodified default → **green** (the default is a valid product; edge case).
3. Replace the starter scene with your own Pong by editing **only** the developer seam:
   `<ProductDir>/Model.fs`, `<ProductDir>/View.fs`, and `tests/Product.Tests/BehaviorTests.fs`
   (plus the documented re-point of `LayoutEvidence.fs`/`EvidenceCommands.fs` model fields).
4. Run `Test` again.

**Expected**: green build + `Product.Tests`, with **zero edits to `GovernanceTests.fs`** and
**no `-- pong`-style flag** introduced. This is SC-001, SC-004, and FR-008.

## Scenario C — SC-002: the default entrypoint launches the dev's game
1. From the Scenario-B product, run the normal command (`dotnet run --project
   src/<ProjectName>/<ProjectName>.fsproj`, no extra args).

**Expected**: the launch reports `mode=interactive-window` for the game family
(`Viewer.runApp`); the developer's Pong is what surfaces — **0** hidden-flag workarounds
(on an unsupported host, the explicit `unsupported`/diagnostic classification is reported, not a
silent evidence fallback).

## Scenario D — SC-003 / SC-005: edit set matches the scaffold map
1. From a clean scaffold, perform the documented swap (Scenario B step 3).
2. `git status` / diff the generated tree.

**Expected**: changed files ⊆ the scaffold-map **replaceable** + **re-point** classification;
**0** undocumented files forced to change. The map's description matches the real swap.

## Scenario E — SC-006: other profiles unchanged; controls still available
1. Re-instantiate `headless-scene`, `governed`, `sample-pack`; diff against the Scenario-A
   baseline.
2. Instantiate the explicit **`app`** profile; run `Test`.

**Expected**: `headless-scene`/`governed`/`sample-pack` diff is **empty** (FR-007); `app` still
generates the controls showcase and passes its governance tests (FR-006). **0** regressions.

## Scenario F — FR-009: cross-repo coordination is filed
**Expected**: a Coordination-board issue + ADR record the `fs-gg-ui-template` default-starter
change and new `game` profile for SDD (scaffold-provider default flip) and Templates (governance
expectations); the contract/compatibility registry entry is updated; the template republish is
sequenced (alongside sibling item #32).
