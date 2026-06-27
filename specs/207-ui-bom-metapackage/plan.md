# Implementation Plan: Optional FS.GG.UI BOM / Metapackage Pinning the Coherent Package Set

**Branch**: `207-ui-bom-metapackage` | **Date**: 2026-06-27 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/207-ui-bom-metapackage/spec.md`

## Summary

Ship the structural fix the P5 epic *"Make the FsSkiaUiVersion staleness bug class structurally
impossible"* calls for: an **optional, full-set BOM / metapackage** published alongside the
`FS.GG.UI.*` framework set. A consumer references **one** package (`FS.GG.UI`) at **one** version
and thereby pins the entire coherent 16-package set to the matching version — no per-package
alignment to get wrong, and any attempt to mix a member at a different version fails **loud**
(restore/build conflict) rather than silently resolving to a stale/mixed graph.

**Chosen mechanism (research R1): a NuGet metapackage with exact-version (`[X]`) dependencies on
every member of the published coherent set, packed from a hand-authored `.nuspec` whose version
token (`$version$`) is supplied by the same `-p:Version=V` invocation that packs the members.**
Exact `[X]` brackets are what make deviation loud in *both* directions (a stale `Y < X` downgrade
and a newer `Y > X` upgrade both conflict with `[X]`); a floating lower bound (`[X,)`) would only
catch the downgrade and silently absorb the upgrade — exactly the mixed-set failure the epic
forbids. The membership list the metapackage enumerates is guarded against drift by a
**parity test** (the BOM's dependency-ID set MUST equal the set of packable `FS.GG.UI.*` projects),
so a member added or removed from the framework without a matching BOM edit fails a test loudly
(US2 AS3).

The BOM is packed **in the same one-command coherent pack** as the members
(`dotnet pack FS.GG.Rendering.slnx -c Release -p:Version=V`) and published in the same
snapshot/tag (FR-006), at the same version with the same channel semantics (FR-005). Adoption is
**optional and additive**: the existing `FsSkiaUiVersion` / CPM per-package pinning in the
`fs-gg-ui` template is unchanged and remains the default for minimal footprint (FR-007); the
template is **not** migrated to consume the BOM (Out of Scope). After the consumer-facing behavior
is verified, the cross-repo compatibility registry in `FS-GG/.github` records the BOM as part of
the coherent `FS.GG.UI` set (FR-008), gated on that evidence.

> **Standing assumption — coherence is unverified until the BOM is packed and a real consumer
> restores against it.** A green unit test (membership parity, nuspec shape) is **not** evidence
> that a clean consumer referencing only `FS.GG.UI` at X restores+builds the whole set at X, nor
> that a forced mismatch actually fails. The only trustworthy signal is a real
> *pack → clean-consumer restore → build → forced-mismatch-conflict* run against the freshly
> packed local feed. `/speckit-tasks` MUST schedule that live pack+restore as the **first
> Foundational step**, before the registry write. The registry/issue MUST NOT be recorded on a
> hypothesis (FR-008).

## Technical Context

**Language/Version**: F# on .NET 10 (`net10.0`). The deliverable is a **NuGet packaging artifact**,
not F# library surface: the metapackage carries dependencies only, no compiled assembly
(`IncludeBuildOutput=false`), so there is no `.fs`/`.fsi` public surface to design.

**Primary Dependencies**: The 16 published, co-versioned `FS.GG.UI.*` member packages (the coherent
set recorded in feature 204's snapshot manifest): `Build`, `Scene`, `Canvas`, `Controls`,
`Controls.Elmish`, `DesignSystem`, `Diagnostics`, `Elmish`, `KeyboardInput`, `Layout`, `SkiaViewer`,
`Symbology`, `Symbology.Render`, `Testing`, `Themes.AntDesign`, `Themes.Default`. Tooling:
`dotnet pack` (nuspec-driven), the local feed at `~/.local/share/nuget-local/`, and a clean
consumer fixture restored against that feed.

**Storage**: N/A — a new packable project + hand-authored `.nuspec`, a parity test, a consumer
validation report, and a cross-repo registry record. No application data.

**Testing**: `tests/Package.Tests` (Expecto) gains (a) a **membership-parity** test (BOM deps ==
packable `FS.GG.UI.*` project set, single shared version token) and (b) an env-gated **live
consumer** test mirroring `GeneratedConsumerValidationTests` /
`Feature163PackageFeedValidationTests` (`FS_GG_RUN_BOM_CONSUMER_SMOKE`-style): pack the snapshot,
restore a clean consumer referencing only `FS.GG.UI@X`, assert every resolved `FS.GG.UI.*` is at X,
then force a member to `Y≠X` and assert a restore/build conflict (NU1605/NU1107). The always-on
gate asserts the committed validation report; the heavy live pack+restore is opt-in behind the env
flag (repo pattern).

**Target Platform**: Linux desktop / headless. Restore+build is platform-neutral; no GL context is
required to prove pinning behavior.

**Project Type**: Packaging / release-engineering addition to the framework repo + a cross-repo
coordination deliverable. No new product feature, control, or API.

**Performance Goals**: N/A — correctness/coherence task.

**Constraints**:
- **Exact-version (`[X]`) member dependencies are mandatory** — a floating range fails FR-004 for
  the `Y > X` mixed-set case. The nuspec uses `[$version$]` for every member.
- **Single-source-of-version invariant (FR-009)**: the BOM introduces **no** second version
  literal. Every member dependency version and the BOM's own version derive from the single
  `-p:Version=V` of the coherent pack (`$version$`). No hand-maintained per-member version.
- **Channel match (FR-005)**: because the BOM version *is* `V`, `-preview.N` ⇒ preview and bare
  `x.y.z` ⇒ stable automatically; it cannot drift from the members it pins.
- **Same-snapshot publish (FR-006)**: the BOM project is added to `FS.GG.Rendering.slnx` so the
  existing one-command pack produces it with the members; it is tagged in the same
  `fs-skia-ui/v<version>` snapshot (feature 204 mechanism).
- **Optional / additive (FR-007)**: `template/base/Directory.Packages.props` (`FsSkiaUiVersion`) is
  **not** modified; no consumer is migrated. The `FsSkiaUiVersion` single-source invariant asserted
  by GovernanceTests stays green.
- **No bare-`FS.GG.UI` collision**: the `FS.GG.UI` package ID is currently unused (16 members are
  all `FS.GG.UI.<suffix>`; the bare ID is free) — it is the natural metapackage name. Confirm no
  feed/source producer already claims it.
- **Cross-repo state** lives in `FS-GG/.github` and is mutated through the GitHub-native cross-repo
  coordination protocol (`gh` + the `cross-repo-coordination` skill), **not** files in this repo,
  and **only after** US1/US2 behavior is verified (FR-008).
- Scope is the BOM only — no profile-scoped/slim BOMs, no template migration, no member API change
  (Out of Scope).

**Scale/Scope**: 1 new packable metapackage project + 1 `.nuspec` (16 exact deps); 1 slnx entry;
1 membership-parity test + 1 env-gated consumer test; 1 consumer validation report; 1 git tag
(shared snapshot); 1 cross-repo registry record (2 files in a sibling repo, via `gh`). `src/**`
members and the `fs-gg-ui` template are **read-only** for this feature (members are re-packed at the
new version, not edited).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Spec → FSI → Semantic Tests → Implementation | ✅ Pass (N/A shape) | The metapackage carries **no F# surface** (`IncludeBuildOutput=false`, no `.fs`/`.fsi`). There is nothing to draft in FSI. "Tests" map to the membership-parity test + the real pack→consumer-restore→forced-conflict evidence, authored failing-first. |
| II. Visibility lives in `.fsi` | ✅ Pass (N/A) | No public `.fs` module is added; no assembly ships from the BOM; no access modifiers introduced. |
| III. Idiomatic Simplicity Is the Default | ✅ Pass | A nuspec with exact deps + one parity test is the plainest mechanism that satisfies "loud deviation". The hand-listed membership (NuGet has no auto-membership metapackage with exact pins) is **justified here** and guarded by the parity test — no operators/SRTP/reflection/CEs. See research R1 for the rejected ProjectReference auto-membership alternative (floating range ⇒ not loud). |
| IV. Elmish/MVU boundary | ✅ Pass (N/A) | No stateful or I/O behavior; a static packaging artifact has no `Model`/`Msg`/`update`. |
| V. Test Evidence Is Mandatory | ✅ Pass | Verification is real: a freshly packed feed, a clean consumer that restores+builds, and a forced-mismatch run that must produce a real NU1605/NU1107 conflict. No synthetic evidence. The registry write is gated on this real evidence (FR-008). |
| VI. Observability and Safe Failure | ✅ Pass | The design fails **loud and closed**: exact `[X]` ⇒ restore/build conflict on any deviation; the parity test fails loudly when membership drifts; a missing/partial snapshot keeps the registry unrecorded (no silent green). |

**Change classification**: **Tier 1 (contracted change)** — it publishes a **new package artifact**
(`FS.GG.UI`) that becomes a versioned distribution surface and consumer contract, and it records a
new entry in the cross-repo `fs-skia-ui-version` compatibility registry. Per Change Classification
this requires the full artifact chain (spec, plan, test evidence, doc/record updates). There are
**no `.fsi`/surface-area baselines** to touch because the metapackage ships no F# surface. No gate
violations — **Complexity Tracking not required**.

## Project Structure

### Documentation (this feature)

```text
specs/207-ui-bom-metapackage/
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 — mechanism decision (nuspec exact-[X]), version/channel, naming, footprint
├── data-model.md        # Phase 1 — entities (BOM artifact, member set, snapshot/tag, parity invariant, registry row)
├── quickstart.md        # Phase 1 — pack → clean-consumer restore → forced-mismatch → record run guide
├── contracts/           # Phase 1
│   ├── bom-metapackage.md          # the artifact: ID, exact-[X] deps, version derivation, same-snapshot pack (US1/US3 FR-001/005/006/009)
│   ├── consumer-pinning-behavior.md# one-ref ⇒ coherent set; deviation is loud (US1/US2 FR-002/003/004)
│   └── cross-repo-record.md         # registry entry, gated on verified US1/US2 (US3 FR-008)
├── checklists/
│   └── requirements.md  # Spec quality checklist (from /speckit-specify, if present)
└── tasks.md             # Phase 2 (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
# New metapackage (the only new shipped artifact)
src/Meta/
├── FS.GG.UI.metaproj      # packable project: PackageId=FS.GG.UI, IncludeBuildOutput=false,
│                          #   NuspecFile=FS.GG.UI.nuspec, NuspecProperties=version=$(Version); IsPackable=true
└── FS.GG.UI.nuspec        # 16 <dependency id="FS.GG.UI.*" version="[$version$]" /> — the single membership list

# Solution wiring (so the one-command coherent pack produces the BOM with the members, same V/tag)
FS.GG.Rendering.slnx       # add the src/Meta project under /src/

# Verification (new tests; mirror existing Package.Tests patterns)
tests/Package.Tests/
├── Feature207BomMembershipTests.fs    # ALWAYS-ON: BOM nuspec deps == packable FS.GG.UI.* set; single version token; exact [..] brackets
├── Feature207BomConsumerTests.fs      # ALWAYS-ON gate asserts the committed report; live pack+restore behind FS_GG_RUN_BOM_CONSUMER_SMOKE
└── Package.Tests.fsproj               # register the two new files

# Verification evidence (env-gated regenerator writes this; gitignored readiness mirror)
specs/207-ui-bom-metapackage/readiness/
└── bom-consumer-validation.md         # clean-consumer restore/build at X + forced-mismatch conflict proof

# Reproducible-snapshot artifact (feature 204 mechanism, extended to include the BOM)
<git tag>  fs-skia-ui/v<version>       # annotated tag at the resolution commit; snapshot now includes FS.GG.UI@version

# Read-only this feature (members re-packed at the new V via -p:Version, NOT edited)
src/*/                                  # 16 packable members; src/ColorPolicy stays IsPackable=false
template/base/Directory.Packages.props  # FsSkiaUiVersion / CPM — UNCHANGED (FR-007; Out of Scope)

# Cross-repo record (sibling repo, mutated via gh — NOT files here; gated on US1/US2 evidence)
FS-GG/.github : registry/dependencies.yml + docs/registry/compatibility.md   # record FS.GG.UI BOM in the fs-skia-ui-version coherent set
```

**Structure Decision**: One new packable project (`src/Meta`) producing the bare `FS.GG.UI`
metapackage from a hand-authored `.nuspec` with exact `[$version$]` member dependencies, added to
the existing solution so the established one-command `dotnet pack ... -p:Version=V` packs it in the
same coherent snapshot as the 16 members (same version, same channel, same `fs-skia-ui/v<version>`
tag). Membership drift is locked by an always-on parity test in `tests/Package.Tests`; pinning
behavior is proven by an env-gated live consumer restore/build + forced-mismatch test mirroring the
repo's existing `GeneratedConsumerValidationTests` pattern. The `fs-gg-ui` template and member
sources are read-only (members are re-packed at the new version, not modified). The cross-repo
registry record is made in `FS-GG/.github` via `gh`, gated on verified US1/US2 evidence.

## Complexity Tracking

> No Constitution Check violations — section intentionally empty.
