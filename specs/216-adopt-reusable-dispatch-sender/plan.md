# Implementation Plan: Adopt Reusable App-Token Dispatch-Sender

**Branch**: `216-adopt-reusable-dispatch-sender` | **Date**: 2026-06-28 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/216-adopt-reusable-dispatch-sender/spec.md`

## Summary

Migrate the Rendering→Templates cross-repo notifier (`fs-gg-ui-template` contract) from the
Feature 214 bespoke sender — which POSTs the `repository_dispatch` itself using a stored long-lived
PAT (`secrets.TEMPLATES_DISPATCH_TOKEN`) and **failed in ~9s on the first real release** because that
secret is empty — to the **org reusable App-token dispatch-sender** `FS-GG/.github` provides via
`workflow_call`. Rendering keeps only what it uniquely owns (the `fs-gg-ui-template/v*` trigger, the
canonical-repo guard, and version derivation/validation from the tag) and delegates the credential
minting + cross-repo POST to the reusable workflow, which mints a least-privilege GitHub App
installation token at run time. No long-lived cross-repo secret remains on Rendering.

> **Key research finding (changes the spec's premise):** the reusable workflow **already exists on
> `FS-GG/.github` `main`** at `.github/workflows/dispatch-sender.yml` (commit `5fed283`), with a
> fully specified `workflow_call` interface — even though tracking issue `.github#22` is still OPEN.
> The consumption interface is therefore **known, not speculative**. The remaining live-criterion
> blocker is no longer "the workflow doesn't exist" but "are the org App secrets from `.github#21`
> (CLOSED) actually set and named as expected, and does a real release authenticate." That is the
> one item that stays gated on live evidence (FR-008/FR-009).

**Tier**: Tier 1 (realizes a cross-repo contract mechanism). No F# public surface is added, removed,
or changed — this feature touches only GitHub Actions YAML and a Bash helper, so no `.fsi` or
surface-area baseline applies (see Constitution Check).

## Technical Context

**Language/Version**: GitHub Actions workflow YAML (schema validated by `actionlint`); Bash (POSIX
`sh`-compatible `set -euo pipefail`).

**Primary Dependencies**:
- Reusable workflow `FS-GG/.github/.github/workflows/dispatch-sender.yml` (consumed via
  `workflow_call`; pinned by commit SHA — see research R2).
- `actions/checkout@v4` (only on the derive job).
- The org cross-repo GitHub App + its two secrets (`app-id`, `app-private-key` ports on the reusable
  workflow), provisioned by `.github#21` (CLOSED). Exact org **secret names** = the cross-repo
  unknown (research R1).
- `actionlint` pinned (`@v1.7.7`) for workflow lint evidence; `gh`/`jq` are runner-preinstalled and
  used inside the reusable workflow, not by Rendering.

**Storage**: N/A.

**Testing**: `actionlint` over both workflows; the local dry-run harness retargeted to the
derive-only helper (`scripts/test-derive-template-version.sh`); a byte-diff regression check on
`release.yml` vs `origin/main`; deferred live cross-repo evidence (sender run URL + receiver PR URL).

**Target Platform**: `ubuntu-latest` GitHub-hosted runners; canonical repo `FS-GG/FS.GG.Rendering`.

**Project Type**: CI/CD automation + shell tooling (no application code).

**Performance Goals**: Receiver pin-bump PR appears within 10 min of tag push (SC-002).

**Constraints**: `release.yml` MUST be byte-identical to `origin/main` (SC-004); template-tag trigger
disjoint from the `v*` release trigger; fail-loud, never silent/unauthenticated (Principle VI);
forks never run the sender and never see the credential (FR-004).

**Scale/Scope**: One trigger, two jobs, one helper script + its harness, one contract doc. First
org consumer of the reusable dispatch-sender (no prior caller to copy secret wiring from).

## Constitution Check

*GATE: must pass before Phase 0. Re-checked after Phase 1 design (below).*

This feature contains **no F# code** — it is workflow YAML + a Bash helper. The F#-specific
principles are therefore N/A by construction, which is itself the gate result, not a waiver:

| Principle | Applies? | Disposition |
|-----------|----------|-------------|
| I. Spec→FSI→Tests→Impl | N/A | No `.fs`/`.fsi`; the analogue (design the *interface* before wiring) is satisfied — the reusable workflow's `workflow_call` contract is read and pinned first (research R2), then consumed. |
| II. Visibility in `.fsi` | N/A | No F# module added/changed. No surface-area baseline touched. |
| III. Idiomatic simplicity | ✅ | Plainest path: delete the bespoke send, delegate to the org workflow, keep only the unique derive/guard. No clever shell. |
| IV. Elmish/MVU boundary | N/A | No stateful F# workflow; the "I/O as data" spirit holds — version derivation is pure, the POST is delegated to the edge (reusable workflow). |
| V. Test evidence mandatory | ✅ | actionlint + retargeted dry-run harness fail-before/pass-after; live cross-repo evidence is the **disclosed deferred check** (Layer 3), never faked green. |
| VI. Observability & safe failure | ✅ | Derive job fails loudly on non-tag/malformed version; the reusable workflow fails closed (with a pointer to `.github#21`) when App secrets are absent — no silent or unauthenticated dispatch (FR-005). |

**Tier 1 obligations**: spec ✅, plan ✅, contract realization doc ✅ (no `.fsi`/baseline because no F#
surface), test evidence ✅, docs (quickstart + cross-repo request) ✅. **Gate: PASS.**

Engineering-constraints check: no new F# dependency; the one new *operational* dependency (reusable
workflow + App) states its need, pinning strategy (SHA pin, research R2), and maintenance owner
(`FS-GG/.github`, org-admin) per the "minimize dependencies" constraint. **PASS.**

## Project Structure

### Documentation (this feature)

```text
specs/216-adopt-reusable-dispatch-sender/
├── plan.md              # This file (/speckit-plan)
├── research.md          # Phase 0 — R1 secret names, R2 pin ref, R3 helper fate, R4 receiver compat
├── data-model.md        # Phase 1 — dispatch call entities & derivation rules
├── quickstart.md        # Phase 1 — Layer 1 lint / Layer 2 derive harness / Layer 3 deferred live
├── contracts/
│   └── template-released-dispatch.md   # Sender-via-reusable contract realization
└── tasks.md             # Phase 2 (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
.github/workflows/
├── template-dispatch.yml          # CHANGED: derive job (guard+version) + dispatch job (uses: reusable)
└── release.yml                    # UNCHANGED — byte-identical to origin/main (SC-004 guard)

scripts/
├── derive-template-version.sh     # NEW (repurposed from template-released-dispatch.sh): derive+validate
│                                  #   the version from the tag ref, emit it to stdout + $GITHUB_OUTPUT.
│                                  #   The send half is removed — the reusable workflow owns the POST.
├── test-derive-template-version.sh# NEW (retargeted harness): derive happy-path + non-tag/malformed edges
└── template-released-dispatch.sh  # RETIRED (send path superseded; removed with its old harness)
```

**Structure Decision**: Single-repo CI change. The sole behavioral edit is `template-dispatch.yml`,
split into a runner `derive` job (canonical-repo guarded, version derivation) and a `dispatch` job
that `uses:` the pinned reusable workflow and maps the two App secrets. Version-derivation logic
(and its tests) is preserved from Feature 214 but stripped of the now-dead credential-guard/send
steps and renamed to reflect its single responsibility (research R3). `release.yml` is not touched.

## Complexity Tracking

No constitution violations — table omitted.
