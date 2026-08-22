---
schemaVersion: 1
workId: 1256-transition-aware-elmish-host-bridge
title: Transition Aware Elmish Host Bridge
stage: clarify
changeTier: tier1
status: needsAnswers
sourceSpec: work/1256-transition-aware-elmish-host-bridge/spec.md
publicOrToolFacingImpact: true
---

# Transition Aware Elmish Host Bridge Clarifications

## Source Specification
- work/1256-transition-aware-elmish-host-bridge/spec.md

## Clarification Questions
- CQ-001 [AMB:AMB-001]: Does an asynchronous response complete a workspace transition, or only prepare current-generation data for a separately acknowledged host commit?
- CQ-002 [AMB:AMB-002]: Which facts may advance while hidden, and what causes resume convergence?
- CQ-003 [AMB:AMB-003]: How does the bridge distinguish permitted controlled-value updates from unsafe old-DOM global activation while pending?
- CQ-004 [AMB:AMB-004]: Which route and measurements qualify the hard timing acceptance rather than a synthetic stand-in?

## Answers
- CQ-001 → a matching response only records current-generation data/readiness; an explicit `Presented` acknowledgement carrying generation and target identity is the sole commit boundary.
- CQ-002 → authoritative generation/target and matching response data may advance while hidden, but no presentation request or acknowledgement is accepted; the first visible event emits one latest-generation presentation request and repeated visible events are idempotent.
- CQ-003 → messages are typed by origin and intent: controlled value/blur reconciliation remains a pure synchronous model transition, while pointer capture and global key/click/file activation are host-origin actions suppressed during pending presentation.
- CQ-004 → qualification must run compiled Fable against production React scheduling and DOM/event-loop behavior with a consumer-shaped Editor→Plan→Simulate fixture, delayed responses, and an observed renderer-task/frame ledger; deterministic adapter tests are necessary but not timing evidence.

## Decisions
- **DEC-001** [CQ-001] [AMB:AMB-001] [FR-002] [FR-003]: Responses can make the current generation presentation-ready but never committed; only a matching current-generation/current-target host acknowledgement commits, and every mismatch is recorded as a rejection.
- **DEC-002** [CQ-002] [AMB:AMB-002] [FR-006]: Hidden mode accepts authoritative target replacement and matching response data only; it records presentation as withheld, rejects acknowledgements, and the hidden→visible edge emits exactly one request for the newest generation unless that generation is already committed.
- **DEC-003** [CQ-003] [AMB:AMB-003] [FR-004] [FR-005] [FR-007]: Controlled value and blur reconciliation are explicit synchronous bridge messages; old-DOM pointer capture plus global key/click/file activation are explicit host inputs whose dispatch policy is `Suppress` while pending, with exactly one pending focus target until a matching commit restores target focus/ARIA.
- **DEC-004** [CQ-004] [AMB:AMB-004] [FR-009]: Ship evidence uses a production-built Fable/React browser fixture and renderer-task/frame observations over the consumer-shaped route; it fails on any task over 16 ms, p95 over 16 ms, p99 over 32 ms, or any dropped frame, and no synthetic/headless surrogate satisfies this obligation.
- **DEC-005** [FR-001] [FR-003] [FR-008]: Generations are opaque monotonically increasing `int64` values allocated by the pure bridge state; targets are caller-owned comparable typed values, and the ledger is an append-ordered immutable list exposed for authoritative host/test inspection.
- **DEC-006** [FR-010]: The API is additive under `TransitionHost`; existing `ElmishAdapter` behavior stays source/binary compatible, while the coherent release updates the `.fsi`, implementation, surface snapshot, fragment documentation/fixture, package acceptance, and handoff together.

## Accepted Deferrals
- None.

## Remaining Ambiguity
- None. AMB-001 through AMB-004 are resolved by DEC-001 through DEC-004.

## Lifecycle Notes
- Next lifecycle action: `fsgg-sdd checklist --work 1256-transition-aware-elmish-host-bridge`.
