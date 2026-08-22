---
schemaVersion: 1
workId: 1256-transition-aware-elmish-host-bridge
title: Transition Aware Elmish Host Bridge
stage: plan
changeTier: tier1
status: planned
sourceSpec: work/1256-transition-aware-elmish-host-bridge/spec.md
sourceClarifications: work/1256-transition-aware-elmish-host-bridge/clarifications.md
sourceChecklist: work/1256-transition-aware-elmish-host-bridge/checklist.md
publicOrToolFacingImpact: true
---

# Transition Aware Elmish Host Bridge Plan

Prose status: planned

## Source Snapshot
- spec: work/1256-transition-aware-elmish-host-bridge/spec.md sha256:7ea2b641990c0ade9d97e0913fc3eb4a1b7a1d3c904f6f87b6320d9db517d71c schemaVersion:1
- clarifications: work/1256-transition-aware-elmish-host-bridge/clarifications.md sha256:bfa19139c47cb447e698341bc48e3053eaeb6999624c4889202162c1c5375a5b schemaVersion:1
- checklist: work/1256-transition-aware-elmish-host-bridge/checklist.md sha256:73873495b03babb5b60b63468cfa5c2bb5d530c737e9410dd0b5b9aa9bea248e schemaVersion:1

## Plan Scope
- Work item 1256-transition-aware-elmish-host-bridge is planned from the current specification, clarification, and checklist facts.
- Requirement count: 10.
- Clarification decision count: 6.
- Checklist result count: 10.

## Technical Context
- `FS.GG.UI.Elmish` is an F# library whose current adapter is pure and additive; the new `TransitionHost` module remains independent of Scene/SkiaViewer values so the same source compiles under .NET and Fable.
- React scheduling and DOM mutation are host effects. The F# state machine owns generation/revision fencing, deferred responses, visibility, controlled values, focus/ARIA intent, input decisions, and an immutable typed ledger.
- Official React behavior makes the separation load-bearing: transition updates are interruptible, async updates after an `await` need a fresh transition scope, transition updates cannot control text inputs, and custom async transition ordering must be handled by the caller/library. The adapter therefore supplies an exact commit token and keeps controlled state synchronous.
- PERF-SMOKE baseline is the filed consumer artifact `artifacts/svg-pipeline-post-batched-revert-seven/summary.json` at S.I.R. candidate `36d0500`: playback max 34 ms/drop 1 and modality-transition max 34 ms/drop 2. It is baseline debt, not ship evidence for this producer.

## Constitution Check
- Principle III: declare `TransitionHost` types/functions in `Elmish.fsi` before implementation and refresh `readiness/surface-baselines/FS.GG.UI.Elmish.txt` from the built assembly.
- Principle V: `TransitionHost.update` is pure; `RequestPresentation`, pointer release, suppression, and focus effects are values interpreted by the Fable/React edge.
- Principle VI: focused deterministic tests cover every semantic branch and are inverted against a named failure; the production browser fixture supplies non-synthetic timing evidence.
- Principle VIII: stale responses/acknowledgements and hidden presentation are rejected into the ledger, never ignored silently.

## Design
- Introduce opaque `TransitionGeneration` plus `TransitionCommitToken<'target>` carrying generation, target, and revision. A new target increments generation and starts revision zero; each accepted current response increments revision, making every older presentation token uncommittable.
- `TransitionHostMsg<'target,'response>` handles begin, delayed response, visibility, presentation acknowledgement, controlled value/file/blur, pointer capture, and global key/click/file attempts. `TransitionHost.update` returns the new state plus `TransitionHostEffect` values.
- The state stores one authoritative request, one committed token, the current response queue, visibility, controlled maps, one focus target, and ledger entries. Public observers expose values without exposing a mutable record constructor.
- Visible begin/current-response emits `RequestPresentation`; hidden begin/current-response records `PresentationWithheld`. Hidden→visible emits one current request and visible→visible is a no-op. Acknowledgement succeeds only when visible and exactly equal to the latest requested token.
- Controlled value/file/blur messages update synchronously in every phase. While pending, pointer capture emits release plus suppression and global key/click/file emits suppression; normal state remains unchanged. Begin and commit emit the single pending/committed focus target respectively.
- The generated fragment documents the host interpreter: wrap every `RequestPresentation`, including delayed responses, in React `startTransition`; dispatch `Presented token` from a layout effect after that token's DOM commits; keep controlled input setters outside the transition.

## Plan Decisions
- PD-001 [AC-001] [FR-001] [DEC-005] complete: Add an opaque monotonic generation and pure `begin` transition whose revision-zero presentation effect carries caller target and focus/ARIA facts; leave the existing adapter and all controlled messages synchronous.
- PD-002 [AC-002] [FR-002] [DEC-001] complete: Store accepted current-generation response payloads in arrival order, increment the commit revision per response, re-request visible presentation, and ledger-reject target/generation mismatches without changing authoritative state.
- PD-003 [AC-003] [FR-003] [DEC-001] [DEC-005] complete: Require exact generation, target, and revision equality for `Presented`; rapid replacement makes all prior tokens stale, so only the latest Simulate token can commit.
- PD-004 [AC-004] [FR-004] [DEC-003] complete: Model pointer capture and global activation as typed input attempts; pending pointer capture yields release plus suppression, and pending global key/click/file attempts yield suppression with an ordered ledger fact.
- PD-005 [AC-005] [FR-005] [DEC-003] complete: Put pending and committed `TransitionFocusTarget` values on the request, expose one current focus observer, and emit one `MoveFocus` host effect on begin/commit with the matching ARIA label.
- PD-006 [AC-006] [FR-006] [DEC-002] complete: Continue authoritative target/response transitions while hidden but withhold presentation and reject acknowledgement; only the hidden→visible edge emits the newest exact token once.
- PD-007 [AC-007] [FR-007] [DEC-003] complete: Keep controlled text/file values and blur reconciliation in pure synchronous messages; pending blur preserves the pending focus destination and old-DOM global file activation remains suppressible separately.
- PD-008 [AC-008] [FR-008] [DEC-005] complete: Expose an append-ordered typed ledger containing begin, response acceptance/rejection, request/withhold, visibility, input dispatch/suppression, acknowledgement/rejection, focus movement, and commit facts.
- PD-009 [AC-009] [FR-009] [DEC-004] complete: Add a source-bound Fable-compiled bridge plus production React `createRoot`/`startTransition` Chromium fixture for workload digest `a49fc53e...73acb`; capture renderer tasks and presentation frames without an injected RAF sampler and enforce max≤16/p95≤16/p99≤32/drop0 across 20 measured runs after two warmups.
- PD-010 [AC-010] [FR-010] [DEC-006] complete: Ship signature, implementation, exact public-surface baseline, focused tests, browser fixture, template fragment documentation, package validation, lifecycle evidence, and consumer handoff as one head-bound coherent-set release.

## Contract Impact
- PC-001 [PD-001] [PD-002] [PD-003] publicApi: `Elmish.fsi` adds opaque generation/state plus typed request, token, response, input, message, effect, ledger, observer, `init`, and `update` contracts under `TransitionHost`; no existing signature changes.
- PC-002 [PD-009] performanceEvidence: `workspace-transition-workload.json` is the immutable workload definition and the production browser summary carries source/workload/build digests, environment/capability, raw renderer-task/frame samples, recomputed percentiles, dropped frames, and the hard verdict.
- PC-003 [PD-010] templateProjection: `template/fragments/elmish/README.md` teaches the production host transaction, delayed-response re-entry, layout-effect acknowledgement, controlled input, input capture, visibility, focus/ARIA, and exact consumer handoff.
- PC-004 [PD-010] releaseContract: the coherent FS.GG.UI set is source-bound to one merge/release head and the public-contract registry projection is refreshed when its live generator reports a change.

## Verification Obligations
- VO-001 [PD-001] [PD-002] [PD-003] [PC-001] semanticTest: Deterministic adapter tests prove delayed worker/feature response queuing, revision fencing, stale response rejection, and rapid Editor→Plan→Simulate replacement.
- VO-002 [PD-004] [PD-007] [PC-001] semanticTest: Tests prove pointer release/global suppression and synchronous controlled text/file/blur preservation, including negative old-DOM dispatch attempts.
- VO-003 [PD-005] [PD-006] [PC-001] semanticTest: Tests prove exactly one pending focus target, committed focus/ARIA restoration, hidden withholding, acknowledgement rejection, and one resume convergence request.
- VO-004 [PD-008] [PC-001] semanticTest: A golden ledger assertion pins the complete authoritative message/effect/commit sequence and a replay equality property proves deterministic ordering.
- VO-005 [PD-009] [PC-002] productionBrowser: Compile the bridge source through Fable, build production React, run Chromium trace qualification for the exact workload digest, and independently recompute max/p95/p99/drop verdict from raw samples.
- VO-006 [PD-009] [PC-002] gateInversion: Mutate the hard renderer-task threshold to make the production fixture red, then restore 16 ms and record both command outcomes without weakening p95/p99/drop gates.
- VO-007 [PD-010] [PC-001] publicSurface: Build Release, refresh the Elmish surface baseline, and prove the public-surface and package-consumer gates pass with no untracked API drift.
- VO-008 [PD-010] [PC-003] generatedProduct: Prove the template fragment names the additive API and production host timing/controlled-input rules, and its fixture remains source-bound to the shipped bridge.
- VO-009 [PD-010] [PC-004] coherentRelease: Run full solution, Elmish, Package.Tests, SDD analyze/verify/ship, exact-head hosted CI, independent critique, delivery-obligation, merge, coherent publish, and consumer handoff gates.

## Performance Intent
- id: PI-001
- disposition: active
- targetFps: 60
- workloadIds: [workspace-transition-production]
- workloadDefinitionDigests: [workspace-transition-production=sha256:a49fc53e890dc93961e68d821c6b680a7f10f1f8d1aadd2cc96787cdbdd73acb]
- maximumExpectedScale: 1200 workspace rows; 4 delayed responses; 2 rapid replacements; 3 controlled-input edits; 4 unsafe-input attempts
- maxP95Ms: 16
- maxP99Ms: 32
- maxCatchUpFrames: 0
- structuralCostBudgets: [renderer-task-max-ms<=16, dropped-frames<=0]
- requiredCapability: production-fable-react-chromium
- liveCompositorRequired: true

## Migration Posture
- PM-001 [PC-001] [PC-003] additive: Existing `ElmishAdapter` users require no source migration; consumers opt into `TransitionHost`, interpret its effects at the React edge, and may adopt generation-fenced host transactions one workspace route at a time.

## Generated View Impact
- GV-001 [PD-010] workModel: Refresh `readiness/1256-transition-aware-elmish-host-bridge` after authored changes, update the exact Elmish public-surface baseline through its generator, and treat any stale lifecycle/performance/source digest as blocking rather than copying evidence.

## Accepted Deferrals
No accepted plan deferrals recorded.

## Planning Findings
No blocking planning findings recorded.

## Advisory Notes
- React reference: https://react.dev/reference/react/startTransition and https://react.dev/reference/react/useTransition (async re-entry, out-of-order custom actions, controlled-input caveat).
- Fable reference: https://fable.io/docs/getting-started/javascript.html (compiled browser route).
- Optional Governance pointers remain compatibility facts only; SDD reports readiness and the release/delivery host owns protected-boundary enforcement.

## Lifecycle Notes
- Next lifecycle action: `fsgg-sdd tasks --work 1256-transition-aware-elmish-host-bridge`.
