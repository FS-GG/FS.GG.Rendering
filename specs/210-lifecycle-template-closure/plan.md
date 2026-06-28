# Implementation Plan: Close the Lifecycle-Agnostic Template Epic

**Branch**: `210-lifecycle-template-closure` | **Date**: 2026-06-28 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/210-lifecycle-template-closure/spec.md`

## Summary

The P1 epic *"Make fs-gg-ui emit Spec Kit only when asked (lifecycle-agnostic template)"* is mechanically
implemented and published (children 204/205/206 Done), but cannot be responsibly closed because the proof
is scattered across three child readiness reports that were produced against each feature's **working
tree**, not against the **currently published package**.

This feature is **acceptance + guidance + coordination only** — it does not change the template's generated
output and does not implement scaffold-orchestrator behavior owned by other repos. Technical approach:

1. **Acceptance harness against the published package** — a new env-gated validation script
   (`scripts/validate-published-acceptance.fsx`) that installs the published `.nupkg` from the local feed
   into an isolated location, runs `dotnet new fs-gg-ui` across the 3 lifecycle values × 4 profiles, asserts
   the per-value gated-file-set result, the byte-identical default, and a bounded build spot-check (`dotnet
   build` on the `app`-profile `sdd`/`none` outputs to verify the "buildable" claim), then uninstalls/restores. It writes a
   single consolidated **Epic Acceptance Record** that pins the validated version and is reproducible from
   that pin. This is the crucial difference from the child evidence: the artifact under test is the package a
   consumer pulls, not the working tree.
2. **Consumer lifecycle guidance + migration note** — extend `.template.package/README.md` with a decision
   tree across `spec-kit`/`sdd`/`none`, per-value include/exclude, the standalone-`none` statement, and a
   migration note from the pre-lifecycle template.
3. **Cross-repo remainder tracking + board closure** — confirm/reuse the existing SDD scaffold-path request
   (`FS-GG/FS.GG.SDD#1`), capture the open constitution-ownership P0 decision for `lifecycle=sdd` as a tracked
   item, reference both from the closure record, and update the Coordination board to show Rendering-side
   complete with remaining items attributed to their owning repos.

> **Standing assumption — root-cause hypotheses are unverified until the app is run.**
> This feature ships **no product code change**, so the "live smoke run" obligation is satisfied by the
> acceptance harness running real `dotnet new` against the installed published package (not the working
> tree). `/speckit-tasks` MUST schedule that live published-package run early (Foundational phase), before
> writing any acceptance/guidance/coordination conclusion, so the close/don't-close statement rests on
> observed behavior of the published artifact rather than inference from child reports.

## Technical Context

**Language/Version**: F# scripting (`dotnet fsi`) for the acceptance harness; Markdown for records/guidance.
No product `.fs`/`.fsi` changes. Target runtime .NET `net10.0` (repo standard).

**Primary Dependencies**: `dotnet new` template engine; the published `FS.GG.UI.Template` package in the local
feed `~/.local/share/nuget-local/`; `git` (tag inspection); `gh` CLI (cross-repo issues + Projects v2 board).

**Storage**: Files only — acceptance record + readiness evidence under
`specs/210-lifecycle-template-closure/readiness/`; consumer guidance in `.template.package/README.md`;
cross-repo state in `FS-GG/.github` registry and GitHub issues/board (external).

**Testing**: Acceptance harness pattern mirrors `scripts/validate-lifecycle-template.fsx` — an always-on
env-free verdict/report core (so a fresh checkout is not red-by-default) plus an env-gated
(`FS_GG_RUN_PUBLISHED_ACCEPTANCE=1`) live loop that does the real install + `dotnet new` matrix and writes
`provenance: live`. Constitution V disclosure applies to any synthesized lines.

**Target Platform**: Linux dev/CI (no GL/window-system needed — template instantiation is filesystem-only; the
bounded build spot-check invokes `dotnet build` but needs no GL/window surface).

**Project Type**: Documentation/evidence/coordination feature over an existing published artifact (single repo,
no new product project).

**Performance Goals**: N/A (one-shot validation run; matrix is 12 instantiations plus a bounded build spot-check
— `dotnet build` on the `app`-profile `sdd` and `none` outputs to verify the FR-003/FR-004 "buildable" claim).

**Constraints**: MUST validate the **published package**, not the working tree (spec edge case: drift between
child evidence and published artifact). MUST pin the exact validated version and be reproducible from that
pin. MUST NOT change generated output. MUST NOT duplicate already-open cross-repo requests.

**Scale/Scope**: 3 lifecycle values × 4 profiles = 12 instantiations + 2 build spot-checks (`app`-profile `sdd`
and `none`); 1 acceptance record; 1 guidance block + migration note; ≤2 cross-repo tracked items (1 reused,
1 captured); 1 board update.

### Resolved decisions (see research.md)

- **Acceptance pin** *(user-confirmed)*: validate the **latest feed package
  `FS.GG.UI.Template.0.1.51-preview.1`** — the artifact a consumer pulls from the feed today. No dedicated
  *template* tag exists at 0.1.51 (only the framework tag `fs-gg-ui/v0.1.51-preview.1`); the record cites the
  package version + framework tag and **flags the missing template tag** as a coordination note (candidate
  follow-up: tag `fs-gg-ui-template/v0.1.51-preview.1`). FR-006 is satisfied by the feed-version pin +
  reproduction command; the tag gap is disclosed, not hidden.
- **Supported profiles** (the four from 204/206): `app`, `headless-scene`, `governed`, `sample-pack`.
- **Gated lifecycle set** (reused from 204, not redefined): `.specify/`, the generated constitution, the
  `.agents/` and `.claude/` skill/context trees, and the generated `AGENTS.md`/`CLAUDE.md` agent-context
  tree — present only under `lifecycle == "spec-kit"`.
- **"Byte-identical"**: default (`spec-kit`) output compared against the pre-lifecycle baseline captured by
  204/206; comparison covers **both file presence and file content** across all four profiles; the record
  restates the baseline so it is self-contained.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Change classification**: **Tier 2-equivalent** — no public API/`.fsi`/contract/observable-behavior change
  to any product. The feature adds evidence tooling, consumer documentation, and coordination state only.
  Spec FR-012 fixes the scope boundary (no generated-output change; no other-repo behavior). ✅
- **I. Spec → FSI → Tests → Impl**: No product surface added, so no FSI sketch required. The "exercise through
  the real surface a consumer uses" principle is honored by driving the **published package via `dotnet new`**
  rather than asserting against internals. ✅
- **II. Visibility in `.fsi`**: No `.fs` modules added; the harness is an `.fsx` script (no public module
  surface). ✅ N/A
- **III. Idiomatic simplicity**: Harness is plain F# scripting mirroring the existing validation script; no
  custom operators/SRTP/reflection/type providers. ✅
- **IV. Elmish/MVU boundary**: No stateful/I-O product workflow added. The harness is a one-shot script; its
  I/O (install, `dotnet new`, file diff) lives at the script edge. ✅ N/A
- **V. Test evidence mandatory**: The acceptance record IS real evidence from a live published-package run;
  the env-free report core synthesizes live-only lines only as a fresh-checkout fallback and discloses
  `provenance: verdict-core` per Constitution V. No assertion is weakened. For this non-test *artifact*, the
  `provenance: verdict-core` field (plus the `live`-only close gate) IS the V-equivalent disclosure mechanism —
  it stands in for the test-name `Synthetic` token / PR-listing that V mandates for synthetic *tests*; the
  intent (synthetic evidence is loud and never close-eligible) is met exactly. Likewise, an unavailable build
  toolchain records the buildability line `environment-limited` rather than a silent pass. ✅
- **VI. Observability/safe failure**: The harness fails loudly if install/instantiation fails or any gated
  file is misclassified; a missing published package is reported as a distinct, actionable error (not a silent
  pass). ✅
- **Repo-owned checks pay for themselves**: The acceptance harness is a one-feature closure proof, not a
  standing CI gate; it is invoked on demand and its cost (12 instantiations) is bounded. Disclosed here per
  the Development-Workflow "narrow checks" rule. ✅

**Result: PASS** — no violations; Complexity Tracking not required.

## Project Structure

### Documentation (this feature)

```text
specs/210-lifecycle-template-closure/
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output (entity schemas: acceptance record, guidance, remainder, closure state)
├── quickstart.md        # Phase 1 output (how to run the published-package acceptance + reproduce)
├── contracts/
│   ├── acceptance-harness.md       # CLI contract for the published-package validation script
│   ├── acceptance-record.md        # Required schema/fields of the Epic Acceptance Record
│   └── lifecycle-guidance.md       # Decision-tree + migration-note contract for consumer guidance
├── checklists/          # (pre-existing)
├── readiness/           # acceptance record + live run evidence (created in implementation)
│   └── epic-acceptance.md          # the single consolidated close/don't-close record (FR-001)
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
scripts/
├── validate-lifecycle-template.fsx        # EXISTING — validates the WORKING-TREE template (204)
└── validate-published-acceptance.fsx      # NEW — installs + validates the PUBLISHED PACKAGE (210)

.template.package/
└── README.md                              # EXTEND — lifecycle decision tree, per-value include/exclude,
                                           #          standalone-none statement, migration note (FR-007..009)

# External (not files in this repo) — coordination surface:
#   FS-GG/FS.GG.SDD#1                       # REUSE — existing scaffold-path request (FR-010)
#   FS-GG/.github registry/dependencies.yml # reference — template contract row (already coherent @206)
#   FS-GG Projects v2 "Coordination" board  # UPDATE — epic status: Rendering-side complete (FR-011)
```

**Structure Decision**: Single repo, no new product project. The only executable artifact is a standalone
acceptance `.fsx` script alongside the existing lifecycle validator; everything else is Markdown evidence,
consumer guidance edited in place, and external coordination state. The new script is deliberately separate
from `validate-lifecycle-template.fsx` because it tests a different artifact (installed published package vs.
working tree) and must not perturb the child-feature regenerator.

## Complexity Tracking

> No Constitution Check violations — section intentionally empty.
