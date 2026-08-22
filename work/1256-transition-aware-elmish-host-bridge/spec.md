---
schemaVersion: 1
workId: 1256-transition-aware-elmish-host-bridge
title: Transition Aware Elmish Host Bridge
stage: specify
changeTier: tier1
status: specified
publicOrToolFacingImpact: true
performanceIntent:
  id: PI-001
  disposition: active
  targetFps: 60
  workloadIds: [workspace-transition-production]
  workloadDefinitionDigests: [workspace-transition-production=sha256:a49fc53e890dc93961e68d821c6b680a7f10f1f8d1aadd2cc96787cdbdd73acb]
  maximumExpectedScale: 1200 workspace rows; 4 delayed responses; 2 rapid replacements; 3 controlled-input edits; 4 unsafe-input attempts
  maxP95Ms: 16
  maxP99Ms: 32
  maxCatchUpFrames: 0
  structuralCostBudgets: [dropped-frames<=0, renderer-task-max-ms<=16]
  requiredCapability: production-fable-react-chromium
  liveCompositorRequired: true
  evidenceRefs: [DEC-004, FR-009]
---

# Transition Aware Elmish Host Bridge Specification

Prose status: specified

## User Value
Generated Elmish products can move between expensive, asynchronously prepared workspaces without a long synchronous React commit, stale presentation, unsafe old-DOM input, or lost controlled-input state.

## Scope
- SB-001: Add an additive typed generation/target host-transaction contract to `FS.GG.UI.Elmish`, with pure state transitions and explicit host effects for deferred presentation and commit acknowledgement.
- SB-002: Cover asynchronous worker and feature responses, rapid transition replacement, input and pointer suppression, focus/ARIA recovery, hidden-tab convergence, and controlled file/text/blur behavior.
- SB-003: Project the public bridge into the generated Elmish template fragment and qualify the consumer-style route through a production Fable/React fixture.
- SB-004: Publish the additive adapter surface through the coherent FS.GG.UI package set and provide a source-bound consumer handoff.

## Non-Goals
- SB-005: Do not make normal Elmish updates, simulation ticks, or controlled input editing asynchronous.
- SB-006: Do not change S.I.R. workspace code, infer consumer-specific messages inside the generic bridge, or accept stale generation/commit facts for convenience.
- SB-007: Do not treat synthetic timing, source inspection, a headless-only surrogate, or relaxed frame thresholds as production qualification.
- SB-008: Do not move optional Governance enforcement into SDD.

## User Stories
- US-001 (P1): As a product author, I can describe a workspace target and generation through a typed Elmish host transaction so expensive presentation work is deferred without changing normal synchronous updates.
- US-002 (P1): As a user, delayed worker/feature responses and rapid target changes cannot reveal or commit an obsolete workspace.
- US-003 (P1): As a keyboard, pointer, file, or assistive-technology user, the pending transition cannot dispatch old-DOM actions and exposes one reliable focus/ARIA destination through commit.
- US-004 (P1): As a user returning to a hidden tab, I see exactly the latest authoritative workspace after one convergence, without replaying hidden presentation work.
- US-005 (P1): As a consumer, I can rely on a production Fable/React qualification fixture that enforces the filed frame/task budget on the generated-product route.

## Acceptance Scenarios
- AC-001 [US-001] [FR-001]: Given an idle bridge, when a typed target begins, then it allocates a strictly newer generation, records the authoritative target, requests deferred presentation, and leaves normal user/simulation/controlled-input dispatch synchronous.
- AC-002 [US-001] [US-002] [FR-002]: Given a pending generation, when its delayed worker and feature responses arrive, then matching responses join that generation's transaction or deferred queue and stale-generation responses are rejected with typed ledger entries.
- AC-003 [US-002] [FR-003]: Given Editor→Plan→Simulate targets are requested before prior commits, when acknowledgements and responses arrive out of order, then only the latest generation can acknowledge and commit Simulate.
- AC-004 [US-003] [FR-004]: Given a transition is pending over an old DOM, when held pointer capture or global key/click/file events attempt dispatch, then capture is released/suppressed and none reaches the old target.
- AC-005 [US-003] [FR-005]: Given a transition is pending and then commits, when focus and accessibility state are inspected, then exactly one accessible pending focus target exists during the transition and correct focus/ARIA is restored for the committed target.
- AC-006 [US-004] [FR-006]: Given the document is hidden while generations and responses advance, when visibility resumes, then no hidden presentation acknowledgement was accepted and the bridge converges once to the latest authoritative generation.
- AC-007 [US-003] [FR-007]: Given controlled text/file input and blur activity around a pending transition, when events are processed, then live controlled state remains synchronous, old-DOM global/file dispatch is suppressed, and blur does not erase the authoritative value or focus destination.
- AC-008 [US-002] [FR-008]: Given any exercised sequence, when the authoritative ledger is inspected, then it contains typed message, response, presentation-request, suppression, visibility, acknowledgement, rejection, and commit facts in deterministic order with generation/target identity.
- AC-009 [US-005] [FR-009]: Given the generated-product production Fable/React fixture exercises delayed responses and rapid Editor→Plan→Simulate replacement, when measured on the declared route, then no renderer task exceeds 16 ms, p95 is at most 16 ms, p99 is at most 32 ms, and dropped frames equal zero.
- AC-010 [US-001] [US-005] [FR-010]: Given the contract is public and additive, when release evidence is assembled, then signature, implementation, surface baseline, template fragment, documentation, package-set version, registry projection if required, and consumer handoff are one coherent source-bound set.

## Functional Requirements
- FR-001: The adapter MUST expose typed transition generation and target values plus a pure begin transition that records the newest authoritative target and requests host presentation while ordinary model updates, simulation, and controlled inputs remain synchronous. (covers AC-001)
- FR-002: Matching delayed worker and feature responses MUST be admitted to the current transaction or explicit deferred queue, while stale or mismatched responses MUST be rejected observably and MUST NOT mutate the committed target. (covers AC-002)
- FR-003: Commit acknowledgement MUST carry generation and target identity, MUST succeed only for the current presented generation, and MUST make rapid Editor→Plan→Simulate replacement commit only Simulate. (covers AC-003)
- FR-004: While pending, the bridge MUST expose host directives that release/suppress held pointer capture and suppress global key, click, and file dispatch from the old DOM without suppressing permitted synchronous controlled-input state. (covers AC-004)
- FR-005: While pending, the bridge MUST describe exactly one accessible focus target and pending ARIA state, then restore the committed target's correct focus and ARIA state on current-generation acknowledgement. (covers AC-005)
- FR-006: While hidden, the bridge MUST retain the latest authoritative target and matching data without accepting presentation or commit work; visibility resume MUST issue at most one convergence request for the latest generation. (covers AC-006)
- FR-007: Controlled text/file/input/blur handling MUST preserve authoritative controlled values and focus intent synchronously while suppressing old-DOM global/file activation during a pending transaction. (covers AC-007)
- FR-008: The bridge MUST expose a deterministic typed authoritative ledger covering messages, accepted/rejected responses, presentation requests, input suppression, visibility changes, commit acknowledgements/rejections, and commits, each bound to generation/target where applicable. (covers AC-008)
- FR-009: A production Fable/React generated-product qualification fixture MUST exercise delayed responses and rapid workspace replacement through the real host route and MUST fail when any renderer task exceeds 16 ms, p95 exceeds 16 ms, p99 exceeds 32 ms, or dropped frames are non-zero. (covers AC-009)
- FR-010: The additive public surface MUST ship coherently across `Elmish.fsi`, implementation, surface baseline, Elmish template fragment, focused/package tests, documentation, package-set release metadata, registry projection when the public contract requires it, and a source-bound consumer handoff. (covers AC-010)

## Ambiguities
- AMB-001 open: Does a response complete presentation immediately, or only record data until an explicit host acknowledgement confirms the current React commit?
- AMB-002 open: Which state changes remain legal while hidden, and what exact event causes the single resume convergence request?
- AMB-003 open: How are controlled input/file/blur messages distinguished from unsafe global old-DOM activation while pending?
- AMB-004 open: What fixture and clock facts qualify as production Fable/React evidence rather than a synthetic timing surrogate?

## Public Or Tool-Facing Impact
- Adds public discriminated unions/records/functions under `FS.GG.UI.Elmish` and therefore updates `Elmish.fsi`, its exact surface baseline, XML/API documentation, package acceptance, and the generated Elmish fragment.
- Adds an executable production Fable/React fixture under the declared Elmish test/template paths; its recorded timing ledger is evidence only when produced by the real renderer/event-loop route and the filed hard thresholds remain unchanged.
- The contract is additive: existing `ElmishAdapter` callers retain synchronous behavior unless they opt into the transition host bridge.

## Lifecycle Notes
- Route receipt: revision 1, digest `358a995a7448c0735f8f2b802acdfd4d72133bcc84858acc149c00e3dfe39cde`, work id `1256-transition-aware-elmish-host-bridge`.
- Implementation begins only after current `analysis.json` reports `implementationReady`.
- Next lifecycle action: `fsgg-sdd clarify --work 1256-transition-aware-elmish-host-bridge`.
