# Feature Specification: Input/Render Responsiveness

**Feature Branch**: `167-input-render-responsiveness`

**Created**: 2026-06-19

**Status**: Draft

**Input**: User description: "start next item in docs/reports/2026-06-19-00-24-framework-and-skills-retrospective.md"

**Resolved Item**: The next unimplemented high-priority retrospective item is the responsiveness work: add diagnostics that measure pointer/key input through visible presentation, and decouple live input dispatch from synchronous retained rendering so native input receipt stays short. Earlier follow-ups for package-feed validation, shared visual-readiness tooling, render/layout inspection metadata, and validation lane runner hardening are already covered by Features 163, 164, 165, and 166.

## Context

The retrospective found that interactive samples can be screenshot-ready while still feeling slow. Pointer routing itself was usually fast, but state-changing clicks and keys could synchronously trigger expensive post-input screen recomposition, layout, text, paint, and presentation work before the input callback returned. Later input then queued behind that work, which makes clicks and keyboard activations appear delayed.

This feature makes responsiveness a first-class readiness surface. Maintainers and sample owners need correlated timing records, budget summaries, and a live scheduling boundary where input receipt enqueues timestamped work, returns quickly, and lets the frame/update loop process inputs and render at most once per dirty frame.

## Change Classification

**Tier 1 (runtime scheduling and diagnostics contract)**. This feature changes live interaction scheduling behavior and adds reusable responsiveness diagnostics. Planning must identify public compatibility impact, preserve product-facing interaction semantics, and provide real evidence for pointer and keyboard activation paths where the host environment allows it.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - See End-to-End Interaction Latency (Priority: P1)

A maintainer diagnosing an interactive sample can enable responsiveness diagnostics, perform pointer and keyboard activations, and see one correlated record for each discrete interaction from input receipt through visible presentation.

**Why this priority**: The retrospective showed that routing-only measurements hid the actual lag source. The most valuable first slice is making the whole interaction visible so maintainers can distinguish routing cost from post-update render and presentation cost.

**Independent Test**: Run a diagnostic replay with one pointer activation and one keyboard activation against a representative interactive screen. Each activation must produce a timing record that includes total visible-response time, queue state, phase breakdown, and whether the product state changed.

**Acceptance Scenarios**:

1. **Given** diagnostics are enabled for an interactive sample, **When** a user clicks a state-changing control, **Then** the resulting record names the input, queue delay, routing time, update time, recomposition time, painting/presentation time, and total visible-response time.
2. **Given** diagnostics are enabled, **When** a keyboard activation changes state, **Then** the record uses the same fields as pointer activation so both paths can be compared.
3. **Given** routing is fast but presentation is slow, **When** the summary is generated, **Then** it identifies presentation-side work as the dominant contributor instead of reporting the interaction as healthy.
4. **Given** the host cannot measure a required timing boundary, **When** diagnostics are summarized, **Then** the missing boundary is reported as unavailable or environment-limited rather than silently omitted.

---

### User Story 2 - Keep Input Receipt Short (Priority: P1)

An interactive user can move the pointer, click, and type without the native input receipt path doing heavyweight screen recomposition before returning.

**Why this priority**: The reported lag comes from expensive work on the input-triggered call path. Input receipt must become a short enqueue-and-signal operation so later input is not blocked by immediate rendering work.

**Independent Test**: Run a burst replay with continuous pointer movement, pointer clicks, and keyboard activations while the screen changes. The replay must show short input receipt durations, preserved discrete input order, coalesced continuous movement, and at most one screen recomposition before each presented frame.

**Acceptance Scenarios**:

1. **Given** a pointer move arrives while previous movement is still pending, **When** the frame loop has not consumed it yet, **Then** continuous movement can be coalesced and the coalesced count is recorded.
2. **Given** click and key events arrive in sequence, **When** the queue is drained, **Then** all discrete events are processed in their original order and none are dropped because of movement coalescing.
3. **Given** one input produces multiple product messages, **When** the frame loop processes that input, **Then** the messages are folded before screen recomposition so the input does not trigger several immediate recompositions.
4. **Given** no state, size, or runtime fact changed, **When** queued input is processed, **Then** the system avoids unnecessary screen recomposition and records that no visible response was required.

---

### User Story 3 - Validate Responsiveness Budgets (Priority: P2)

A sample owner can run a documented responsiveness check and receive a budget report with p50, p95, maximum latency, long-frame counts, and readiness status by page, input type, and control group.

**Why this priority**: Screenshot readiness is not enough for live samples. A maintainer needs a repeatable report that can block accepted readiness when click or key activation feels slow.

**Independent Test**: Run the responsiveness check against passing and deliberately slow fixtures. The report must accept the passing case, block the slow case, and show the first failing budget plus the slowest pages or control groups.

**Acceptance Scenarios**:

1. **Given** a representative interaction demo meets the latency budget, **When** the responsiveness check finishes, **Then** the report marks the checked scope accepted and lists the measured p50, p95, and maximum values.
2. **Given** a page exceeds the input-to-visible-response budget, **When** the report is generated, **Then** readiness for that scope is blocked and the slowest interaction records are linked or summarized.
3. **Given** a frame contains long-running render or presentation work, **When** diagnostics are summarized, **Then** the long-frame condition is counted and cannot be hidden by successful routing measurements.
4. **Given** a run is environment-limited, **When** readiness is summarized, **Then** the result is not marked accepted unless the limitation and substitute evidence are explicitly documented.

---

### User Story 4 - Preserve Existing Interaction Semantics (Priority: P3)

Generated products and sample owners keep the same pointer, focus, keyboard, and model-update behavior while gaining safer scheduling and better diagnostics.

**Why this priority**: Responsiveness fixes must not require product code rewrites or change which user actions fire commands.

**Independent Test**: Run existing interaction tests and a focused keyboard baseline after the scheduler change. Pointer activation, focus routing, and keyboard activation outcomes must match the prior behavior while the new timing records are produced.

**Acceptance Scenarios**:

1. **Given** an existing pointer activation test, **When** it runs after the scheduler change, **Then** the same product-visible action occurs.
2. **Given** the representative keyboard baseline uses Enter and Space on key-down for activation, **When** those keys are replayed, **Then** they keep the same activation behavior and key-up remains non-activating unless a focused control handles it.
3. **Given** a product relies on pure state updates followed by rendering, **When** queued inputs are processed, **Then** the final visible state matches the state that synchronous processing would have produced for the same ordered discrete inputs.
4. **Given** diagnostics are disabled, **When** the application runs normally, **Then** interaction behavior remains unchanged and diagnostics overhead is not user-visible.

### Edge Cases

- Continuous pointer movement arrives faster than the frame loop can consume it.
- A burst contains mixed pointer moves, clicks, key-down events, key-up events, and resize events.
- One discrete input produces multiple product messages.
- A discrete input does not change product state but still changes focus or hover state.
- A state-changing input arrives while a previous frame has not presented yet.
- A render or presentation phase exceeds the long-frame budget.
- Host timestamps are unavailable, low precision, or not monotonic.
- Diagnostics output cannot be written or is interrupted mid-run.
- The host environment lacks a visible presentation surface for part of the check.
- An update, recomposition, render, or presentation error occurs after input has been queued.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST assign a stable sequence identity and receipt timestamp to every pointer and keyboard input considered by the live viewer.
- **FR-002**: Native input receipt MUST enqueue normalized input work, signal processing, and return without performing full screen recomposition or presentation work.
- **FR-003**: Queued input MUST distinguish discrete inputs that preserve order from continuous inputs that may be coalesced.
- **FR-004**: Discrete pointer and keyboard inputs MUST be processed in receipt order and MUST NOT be dropped by continuous-input coalescing.
- **FR-005**: Continuous pointer movement MAY be coalesced before heavyweight processing, but coalesced counts and the latest retained movement state MUST be reported.
- **FR-006**: The frame/update loop MUST drain queued inputs, apply their resulting state transitions, mark dirty state, and trigger screen recomposition only when required.
- **FR-007**: All product messages produced by one input MUST be folded before that input causes screen recomposition.
- **FR-008**: The system MUST recompute the visible screen at most once for a dirty frame after draining that frame's eligible input batch.
- **FR-009**: Timing records MUST include input identity, input kind, receipt time, queue delay, routing duration, update duration, screen recomposition duration, layout or text-work duration when available, paint or presentation duration when available, total input-to-visible-response duration, queue depth, coalesced movement count, state-changed flag, dirty-area or changed-region summary when available, and environment status.
- **FR-010**: Diagnostics MUST provide both reviewer-readable summaries and machine-readable records suitable for readiness evidence.
- **FR-011**: Responsiveness summaries MUST report p50, p95, maximum latency, long-frame counts, and readiness status by checked page or screen, input type, and control group when those groupings are known.
- **FR-012**: The system MUST expose default readiness budgets for input receipt duration, input-to-visible-response duration, and long-frame reporting.
- **FR-013**: A checked scope MUST NOT be marked accepted when required timing boundaries are unavailable, when the run is environment-limited without accepted substitute evidence, or when measured latency exceeds the configured budget.
- **FR-014**: Existing pointer, focus, keyboard, and product state semantics MUST be preserved for ordered discrete inputs.
- **FR-015**: Existing deterministic interaction and performance checks MUST remain available; this feature adds live responsiveness evidence rather than replacing structural tests.
- **FR-016**: Failures during queued input processing, update, recomposition, paint, presentation, or diagnostic writing MUST be reported with actionable diagnostics and MUST NOT be silently swallowed.
- **FR-017**: Maintainers MUST be able to run a documented responsiveness check for at least one representative interactive sample without manually editing source files.
- **FR-018**: The feature MUST include evidence for pointer activation, keyboard activation, continuous-move coalescing, multiple messages from one input, no-state-change input, long-frame reporting, environment-limited reporting, and disabled-diagnostics behavior.

### Scope Boundaries

- In scope: live input queuing, frame-paced input draining, discrete-input ordering, continuous-input coalescing, dirty-state scheduling, correlated interaction timing records, responsiveness summaries, readiness budget reporting, representative sample adoption, and preservation of existing interaction semantics.
- Out of scope: broad visual redesign, complete controls rewrite, mandatory product code rewrites, full renderer-thread migration, subjective visual quality review, package-feed validation, screenshot/contact-sheet workflow changes, and general render optimization unrelated to the input scheduling boundary.
- Follow-up scope: damage narrowing, retained subtree caching, text measurement caching, and deeper rendering throughput work may be planned separately unless required to satisfy the initial representative responsiveness budgets.

### Key Entities

- **Input Envelope**: A normalized record of one pointer or keyboard input, including identity, timestamp, kind, priority, original payload, and diagnostic context.
- **Input Queue**: The ordered live buffer that holds pending discrete inputs and coalescible continuous inputs until the frame/update loop drains them.
- **Frame Update Cycle**: One processing pass that drains eligible input, applies product-visible state transitions, marks dirty state, recomposes the screen when needed, and hands work to presentation.
- **Latency Record**: The correlated timing evidence for one discrete input or coalesced continuous batch, from receipt through visible response or explicit no-visible-response classification.
- **Responsiveness Budget**: A configured threshold for input receipt, input-to-visible-response, and long-frame reporting.
- **Coalescing Policy**: The rule set that determines which continuous inputs may be merged and how dropped or merged samples are counted.
- **Responsiveness Summary**: A reviewer-readable and machine-readable rollup of latency records, percentiles, slowest interactions, long-frame counts, readiness status, and environment limitations.
- **Environment Limitation**: A declared condition that prevents complete live timing or presentation evidence, such as missing visible presentation support or unavailable timing boundaries.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: During representative diagnostic replay, at least 95% of input receipt callbacks complete in under 4 ms and 100% complete in under 16 ms.
- **SC-002**: For at least one representative interaction demo, 95% of scripted pointer and keyboard activations produce a visible response in under 50 ms; any environment-limited run is reported as blocked or environment-limited rather than accepted.
- **SC-003**: In a burst replay with at least 100 pointer movement samples and 20 discrete pointer or keyboard inputs, 100% of discrete inputs are processed in receipt order, no discrete input is lost, and coalesced movement counts are reported.
- **SC-004**: In 100% of scripted state-changing inputs that produce one or more product messages, screen recomposition occurs no more than once before the next presented frame.
- **SC-005**: 100% of completed scripted interactions produce latency records containing input identity, queue depth, phase timing, total visible-response duration, state-changed status, and environment status.
- **SC-006**: A maintainer can identify the first failed budget and the three slowest checked pages or control groups from the responsiveness summary in under one minute.
- **SC-007**: Existing pointer activation and representative keyboard activation tests pass unchanged after the scheduling change.
- **SC-008**: Every diagnostic run reports long-frame or long-presentation work over 50 ms when it occurs, and such work cannot be hidden inside accepted readiness.

### Measurement Definitions

- **Input receipt callback** means the host-visible path from native input notification to the point where the input has been normalized, queued, and processing has been signaled.
- **Visible response** means the first presented frame, explicit no-visible-response classification, or environment-limited marker that can be correlated to the input sequence.
- **Representative interaction demo** is the initial bounded interactive sample named in readiness evidence. It must include at least one pointer activation and one keyboard activation that change visible state.
- **Long-frame work** means any measured recomposition, paint, presentation, or combined frame segment that exceeds 50 ms.

## Assumptions

- "Next item" is interpreted as the next unimplemented P0 retrospective follow-up after Features 163-166: responsiveness diagnostics and decoupling live input dispatch from synchronous retained rendering.
- AntShowcase is the primary motivating sample, but the first accepted budget may use a bounded representative interaction demo if AntShowcase still exposes deeper render-performance bottlenecks that need follow-up work.
- The feature prioritizes the scheduler boundary and evidence surface; broad damage narrowing, retained-render caching, text caching, and renderer-thread separation are follow-up work unless planning proves they are required for the representative budget.
- Product-facing state/update/view semantics remain stable. The scheduling change should alter when work runs, not which product messages are produced for ordered discrete inputs.
- Keyboard baseline for the representative sample is Enter and Space on key-down for activation; key-up and unrelated keys remain non-activating unless focused-control routing consumes them.
- Real host timing and presentation evidence are preferred. Synthetic or environment-limited evidence must be disclosed and cannot silently satisfy accepted readiness.
