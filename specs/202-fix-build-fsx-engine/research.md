# Phase 0 Research: Fix the generated build.fsx governance-engine resolution

This file resolves the one open decision the spec deliberately left to `/speckit-plan` — **which
governance engine the generated `build.fsx` binds, and how** — and records the root-cause map that
grounds the fix.

## Root-cause map (grounded in source/feed/cache inspection)

| # | Symptom | Evidence | Verdict |
|---|---------|----------|---------|
| R0 | `Verify` aborts on the evidence gates in every generated product | `build.fsx` `runGeneratedEvidence` restores + reflects `FS.GG.UI.Build`; that package restore fails | Confirmed (two independent causes below) |
| R1 | Stale pre-rebrand cache probe | `template/base/build.fsx:126` builds `path [ … "fs.skia.ui.build"; version; … "FS.GG.UI.Build.dll" ]` | Confirmed — wrong folder id (`fs.skia.ui.build` vs `fs.gg.ui.build`); would miss the engine even if present |
| R2 | No producer for `FS.GG.UI.Build` anywhere | No `.fsproj` sets `<PackageId>FS.GG.UI.Build`; no `GeneratedRunner` source in any repo; not in `~/.local/share/nuget-local/` (only `FS.GG.UI.*` libs + `FS.GG.Governance.Cli.0.1.1`) nor in `~/.nuget/packages`; `PROVENANCE.md` records `build/Governance` (`FS.Skia.UI.Build` engine) was **excluded at import** | Confirmed — restore can never succeed; the engine must be *produced*, not merely re-pathed |
| R3 | Original engine source is unrecoverable | Excluded from `EHotwagner/FS-Skia-UI` import; not in this repo's git history; `build/Governance/PackageSurface.fs` is package *metadata*, not the engine | Confirmed — a faithful **re-author** is required, not a port |
| R4 | The engine contract *is* recoverable | `template/base/docs/evidence-formats.md` is "GENERATED from `FS.GG.UI.Build.Evidence.EvidenceFormatSchema`" and enumerates each readiness file's required tokens; `build.fsx` fixes the reflected entrypoint shape; `evidence-audit.md` requires a `verdict` token | Confirmed — the re-authored engine has an authoritative contract to honor |

## Decision: (re)establish an in-repo, in-process `FS.GG.UI.Build` producer

- **Decision**: Author a fresh packable library at `src/Build/FS.GG.UI.Build.fsproj`
  (`PackageId=FS.GG.UI.Build`, `IsPackable=true`, `OutputType=Library`, `net10.0`) exposing
  `FS.GG.UI.Build.Evidence.GeneratedRunner.run : string -> string -> int`. It senses the generated
  product's `readiness/**` evidence surface, builds the EvidenceGraph and EvidenceAudit reports, writes
  `readiness/evidence-graph.md` + `readiness/evidence-audit.md` (honoring `evidence-formats.md`), and
  returns an exit code (0 = pass). Correct the `build.fsx` cache probe `fs.skia.ui.build` →
  `fs.gg.ui.build`. The existing package harness packs the engine into the coherent feed at `$(Version)`
  automatically.

- **Rationale**:
  1. **FR-006 (in-process)** — only `dotnet test` may be an external process. An in-repo library
     loaded by `Assembly.LoadFrom` + reflection satisfies this; a CLI tool subprocess does not.
  2. **FR-004 (single version literal, lock-step)** — an `FS.GG.UI.*` package packed at the same
     `$(Version)` moves with the libraries on one `FsSkiaUiVersion` edit. The harness discovers it by
     `PackageId` prefix + `IsPackable` (no hardcoded list; `tools/Rendering.Harness/PackageFeed.fs`
     `discoverPackablePackages`, mirrored in `dev-repack.fsx`), so a coherent `-p:Version=<V>` pack is
     all the wiring needed — **zero** new pack code.
  3. **Constitution** — the repo "MUST be buildable … without depending on any external governance
     platform." An in-repo engine keeps the generated product self-contained; re-pointing to a sibling
     governance platform reintroduces exactly that dependency.
  4. **Contract is fixed and recoverable** — `build.fsx` + `GovernanceTests.fs` already pin the
     reflected surface (`GeneratedRunner`, `run`, `EvidenceGraph`/`EvidenceAudit` targets), and
     `evidence-formats.md` documents the output contract. Re-authoring to a known target is low-risk.
  5. **FR-002 (no pre-rebrand identity)** — the same change deletes the last `fs.skia.ui.build` cache
     path, satisfying US2 acceptance #3.

- **Alternatives considered**:
  - **Re-point to `FS.GG.Governance.Cli` (v0.1.1, already in the feed) as a subprocess tool** —
    *Rejected.* Adds an external process (FR-006 ✗), a second/independent version line (FR-004 ✗), and
    an external-governance-platform dependency (constitution ✗). It is the spec's named alternative but
    is foreclosed by three hard constraints, not a preference.
  - **Load `FS.GG.Governance.*` assemblies in-process by reflection** — *Rejected.* Avoids the
    subprocess but still introduces a second version line and an external-platform dependency, and its
    `EvidenceGraph<'id>`/`ProjectEvidenceReport` surface does not match the `GeneratedRunner.run(target,
    dir): int` contract the script + tests pin — it would force consumer + test churn for no benefit.
  - **Remove the EvidenceGraph/EvidenceAudit gates from the generated `build.fsx`** — *Rejected.*
    Violates FR-001/FR-007 (the gates must run; governance tests assert their presence) and degrades
    the template's "governed product" promise.
  - **Only fix the cache path (R1) without producing an engine** — *Rejected.* Necessary but
    insufficient: R2 means restore still fails. The spec's Assumptions make "make `Verify` fully pass"
    the operator decision, not graceful degradation.

## Supporting findings

- **Harness auto-pack** (`tools/Rendering.Harness/PackageFeed.fs`, `discoverPackablePackages`): scans
  `repoRoot/src/**.fsproj`; includes when `PackageId.StartsWith "FS.GG.UI."` **and** `IsPackable=true`;
  emits `{id}.{ver}.nupkg`. No hardcoded package list; `dev-repack.fsx` and
  `refresh-local-feed-and-samples.fsx` use the same filter. ⇒ `src/Build/FS.GG.UI.Build.fsproj` is
  picked up with no tooling edits.
- **Coherence requirement** (memory `template-feed-version-model`): each `src/*` fsproj carries its own
  `<Version>`, so a plain `dotnet pack` yields a *mixed* feed the single pin cannot resolve. Validation
  MUST pack with `-p:Version=<V>` (fresh, above all per-project versions) and set `FsSkiaUiVersion=V`.
  The new engine inherits this automatically.
- **Generated evidence surface the engine senses** (`template/base/src/Product/EvidenceCommands.fs`,
  `LayoutEvidence.fs`): headless profiles emit `readiness/layout-evidence.txt`,
  `readiness/headless-scene-evidence.txt`; interactive profiles add launch/image/screenshot/
  pixel-readback/window-diagnostics/window-options/bounded-smoke artifacts. `evidence-formats.md`
  enumerates the required tokens per file — the engine's audit checks presence/well-formedness and emits
  `verdict=`.
- **Open runtime question (drives the early smoke run, not a blocker for the decision):** the `Verify`
  target sequence does **not** itself invoke the product's evidence CLI before EvidenceGraph, so which
  `readiness/**` artifacts exist at gate time must be observed live. The engine must be tolerant of the
  available surface (graph what exists; audit the artifacts the profile is expected to have), and
  `quickstart.md` documents producing evidence before/within Verify where required. Resolve empirically
  in the Foundational early smoke run.

## Open clarifications

None blocking. The engine's *internal* rule set is scoped out by the spec ("out of scope except for
the entrypoint/contract the build calls and the requirement that the engine be obtainable"); the
re-authored engine honors `evidence-formats.md` as its authoritative output contract and is free to
implement the minimal faithful graph/audit over the available readiness surface. Depth of audit rules
is an implementation choice bounded by "must actually execute, not a log-only stub" (FR-001/SC-001).
