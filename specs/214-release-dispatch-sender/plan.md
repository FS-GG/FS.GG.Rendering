# Implementation Plan: Release → Templates Dispatch Sender

**Branch**: `214-release-dispatch-sender` | **Date**: 2026-06-28 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/214-release-dispatch-sender/spec.md`

## Summary

When Rendering publishes a `FS.GG.UI.Template` coherent-set release (the template-scoped tag
`fs-gg-ui-template/v<version>`, per Feature 206), it must send a `repository_dispatch` notification
to FS.GG.Templates with event type `fs-gg-ui-template-released` and `client_payload.version` set to
the released version. The receiver (`upstream-bump.yml` in FS.GG.Templates) already exists and opens
a pin-bump PR; only this sender half is missing. Today the receiver must be triggered by hand.

**Technical approach**: add a small, isolated sender — a new workflow `template-dispatch.yml`
triggered on `push` of tags matching `fs-gg-ui-template/v*` (plus a `workflow_dispatch` manual entry;
a manual run lands on a non-tag ref, so version derivation fails and the job fails loudly rather than
sending — consistent with FR-006/FR-007), which derives the version from the tag
ref and calls a single dependency-free shell step (`scripts/template-released-dispatch.sh`) that
validates the version, builds the JSON payload, and dispatches via the `gh` CLI REST call. The
workflow is fork-gated (`github.repository == 'FS-GG/FS.GG.Rendering'`) and uses an org-provided
cross-repo credential (the standing blocking dependency, FS-GG/.github#21/#22). Keeping the sender in
its own workflow leaves the existing `release.yml` gating untouched (FR-008) and makes the
trigger pattern itself the "only genuine template releases" guard (FR-007).

> **Standing assumption — root-cause hypotheses are unverified until the app is run.**
> This feature has no running "app"; the equivalent live check is an end-to-end dispatch, which is
> **blocked** by the org cross-repo credential. `/speckit-tasks` MUST schedule, in the Foundational
> phase, the strongest local proof available before any fix work: `actionlint` on the new workflow
> plus a `DRY_RUN=1` exercise of the dispatch script that confirms the derived version and payload
> shape (and the fail-loud paths). The real cross-repo send is a documented deferred verification,
> not a silent assumption.

## Technical Context

**Language/Version**: GitHub Actions workflow YAML + POSIX `bash` + `gh` CLI. The repository is
F#/.NET `net10.0`, but this feature adds **no F# code** and touches no public module surface.

**Primary Dependencies**: GitHub Actions `ubuntu-latest` runner; `gh` CLI (preinstalled on the
runner); the GitHub REST `repository_dispatch` endpoint (`POST /repos/FS-GG/FS.GG.Templates/dispatches`).
No new third-party Action is introduced (constitution: dependencies minimized).

**Storage**: N/A — the only persistent state is the cross-repo notification itself.

**Testing**: `actionlint` (workflow structure/semantics) + a local dry-run harness for
`scripts/template-released-dispatch.sh` (`DRY_RUN=1`) asserting version derivation, payload shape,
and fail-loud branches. End-to-end live dispatch is **deferred** (blocked by the org credential) and
documented as such; any synthetic stand-in is disclosed.

**Target Platform**: GitHub Actions, canonical repo `FS-GG/FS.GG.Rendering` only.

**Project Type**: CI / release automation (single repository).

**Performance Goals**: N/A — one REST call per template release.

**Constraints**: MUST NOT alter or weaken existing `release.yml` gating (FR-008); fork-safe, no fork
secret exposure (FR-005); fail-loud on any send failure incl. undeterminable version (FR-006);
depends on an org-owned cross-repo credential not provisioned in this repo (BLOCKING for live path).

**Scale/Scope**: one new workflow file, one new shell script (plus a small dry-run test harness), one
contract doc, plus spec artifacts.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

**Change classification**: **Tier 1** — realizes a cross-repo contract (the sender half of
`fs-gg-ui-template`). No public F# API surface changes, so the `.fsi`/surface-baseline obligations of
Tier 1 are **N/A here**; the contract obligation (FR-009) is met by recording the dispatch contract
under `contracts/` and keeping it coherent with the registry.

| Principle | Verdict | Notes |
|-----------|---------|-------|
| I. Spec → FSI → Semantic Tests → Impl | **N/A** | No F# module is added; there is no FSI surface. The analogue (contract-first) is honored: the dispatch contract is fixed by the existing receiver and recorded before implementation. |
| II. Visibility lives in `.fsi` | **N/A** | No `.fs`/`.fsi` files touched. |
| III. Idiomatic simplicity | **PASS** | One small workflow + one plain POSIX script; no clever abstractions, no new Action dependency. |
| IV. Elmish/MVU boundary | **N/A** | No stateful F# workflow. The CI step is declarative; its single side effect (the REST dispatch) is isolated and fail-loud. |
| V. Test evidence mandatory | **PASS (with disclosed deferral)** | `actionlint` + `DRY_RUN=1` script tests are real, runnable evidence. The live cross-repo send is blocked by the org credential and is documented as a deferred real-evidence path (constitution V permits this when disclosed). |
| VI. Observability & safe failure | **PASS** | The send step echoes the target/event/version, runs under `set -euo pipefail`, and fails the job on any error (missing credential, undeterminable version, receiver/network error) — never silent (US2, FR-006). |

No violations requiring Complexity Tracking.

## Project Structure

### Documentation (this feature)

```text
specs/214-release-dispatch-sender/
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── template-released-dispatch.md   # event id + client_payload schema (sender↔receiver)
├── checklists/
│   └── requirements.md  # (existing) spec quality checklist
├── readiness/           # evidence output (baselines, dry-run logs, actionlint, deferred-check)
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
.github/
└── workflows/
    ├── release.yml                 # UNCHANGED (FR-008) — existing release-only gating
    └── template-dispatch.yml       # NEW — sender; triggers on tag fs-gg-ui-template/v*

scripts/
├── template-released-dispatch.sh   # NEW — derive version, validate, build payload, gh dispatch
│                                   #       (DRY_RUN=1 prints payload and skips the network call)
└── test-template-released-dispatch.sh  # NEW — POSIX dry-run harness driving the four quickstart
                                     #       scenarios (happy / empty / malformed / missing token)
```

**Structure Decision**: A dedicated sender workflow + a single extracted shell script. The dedicated
workflow isolates the new behavior from `release.yml` (FR-008) and lets the tag-pattern trigger
(`fs-gg-ui-template/v*`) be the FR-007 guard. Extracting the derive/validate/payload/dispatch logic
into `scripts/template-released-dispatch.sh` makes FR-002/FR-003/FR-006 and the edge cases locally
testable via `DRY_RUN=1`, with no GitHub round-trip and no credential.

## Complexity Tracking

> No constitution violations — section intentionally empty.
