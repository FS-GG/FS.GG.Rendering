# Contract: Overlay Visual Proof

## Scope

This contract defines Feature 145 validation behavior. It is a readiness/evidence contract, not a new product
overlay behavior contract. Exact internal F# names may change during implementation, but the semantics below are
required unless the plan is amended.

## Host Capability Contract

Before claiming visual success, the validation path must determine whether the host can produce real visual
artifacts for the selected overlay scenario.

Required capability evidence:

- effective display backend
- display identifier when available
- GL renderer and version when available
- capture availability
- blocked stage when unavailable
- diagnostic category

`NoDisplay`, missing GL renderer, unavailable capture, or failed window/offscreen setup cannot pass visual proof.
Those outcomes are reported as environment-limited or failed according to the failure category.

## Artifact Acceptance Contract

A successful visual-proof run must produce at least two artifacts:

1. open overlay state
2. final closed state

Each accepted artifact must:

- exist under `specs/145-overlay-visual-proof/readiness/artifacts/`
- decode as an image
- have positive width and height
- have non-blank pixel content when non-blank is required
- belong to the current run id and scenario id
- be linked from the evidence record
- be human-inspectable

Validation must fail when an artifact is missing, blank, zero-sized, unreadable, outside the readiness artifact
tree, stale from a previous run, or disconnected from the current scenario.

## Open-State Proof Contract

The open-state record must prove:

- the selected transient surface is open
- the surface appears above covered content in the artifact
- the evidence names the matching topmost eligible hit target
- focus state matches the existing behavioral evidence
- product dispatch summary matches the expected open/interaction step

If the visual artifact and hit-order evidence disagree, readiness fails with an overlay-behavior diagnostic.

## Closed-State Proof Contract

The closed-state record must prove:

- dismissed overlay content is no longer visible
- no stale overlay hit target remains active
- focus recovery matches the expected Feature 144 flow
- product dispatch summary contains only the expected close/selection outcome

If the final artifact still shows overlay content or the evidence keeps a stale hit target, readiness fails.

## Behavioral Correlation Contract

Every visual artifact must be correlated with deterministic evidence from the existing overlay flow:

- scenario id
- input step
- expected overlay state
- topmost hit decision
- focus state
- product dispatch summary
- replay log or readiness evidence reference

A screenshot without this metadata is not sufficient proof.

## Unsupported-Host Contract

Unsupported-host runs must produce an environment-limited readiness record with:

- owner
- cause
- host facts
- next proof path
- rationale for why deterministic behavioral evidence remains separate from visual proof
- explicit `notAuthoritativeFor` visual-proof disclosure

Unsupported runs must not claim a screenshot path, live-viewer capture, non-blank visual proof, or
`provesScreenshot=true`.

## Failure Classification Contract

Failures must be classified as one of:

- `environment`: display, GL renderer, host setup, or capture capability unavailable
- `capture`: artifact creation, decode, dimensions, or non-blank validation failed
- `overlay-behavior`: pixels, hit order, focus, or dispatch disagree with expected overlay behavior
- `evidence-bookkeeping`: stale path, wrong run id, missing scenario link, or readiness record mismatch

The readiness decision must include the category and keep the Feature 144 caveat open unless a capable-host run
passes.

## Compatibility Contract

The planned implementation must not change product-facing overlay behavior, public control APIs, portable scene
serialization, browser rendering, compositor behavior, intrinsic layout, text shaping, text editing, selection
editing, or widget catalog behavior.

If implementation requires a public `.fsi` contract, package surface, or compatibility change, work must pause
for Tier 1 reclassification, `.fsi`-first design, semantic tests, surface baselines, migration notes, and
versioning rationale.
