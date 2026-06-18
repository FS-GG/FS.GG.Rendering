# Research: Compositor Proof Interpreter

## Decision 1: Treat Feature 153 as a proof interpreter, not a new readiness policy

**Decision**: Reuse Feature 152 proof-set vocabulary and limit this slice to running live proof
attempts, classifying attempts, evaluating the three-attempt proof set, and publishing readiness
evidence. Same-profile parity and timing remain later gates.

**Rationale**: The spec explicitly keeps Feature 152 as the authoritative acceptance language and
states that this feature does not claim the full P7 performance win. Narrowing the slice prevents
the interpreter from redefining partial-redraw policy while still producing the real evidence the
later gates need.

**Alternatives considered**: Reopen the full P7 readiness package and combine proof, parity, and
timing. Rejected because it would blur the report's proof/parity/timing sequence and risk
overclaiming partial redraw from proof evidence alone.

## Decision 2: Keep host-backed proof execution in SkiaViewer and orchestration in Rendering.Harness

**Decision**: `SkiaViewer` owns host profile detection, sentinel presentation, damage-scoped
presentation, pixel/readback observation, and attempt classification. `Rendering.Harness` owns CLI
routing, repeated attempts, artifact paths, proof-set summaries, unsupported-host evidence, and
readiness publication.

**Rationale**: This matches existing Feature 147-152 boundaries: host behavior is a viewer concern,
while evidence packaging and command execution live in the harness. It also keeps filesystem and
process I/O at the edge rather than inside pure proof classification.

**Alternatives considered**: Put all interpreter state into `Rendering.Harness`. Rejected because
the host-proof rules need to exercise SkiaViewer present/readback behavior directly and should be
available to semantic tests through the package surface when public.

## Decision 3: Model each live attempt as an MVU-style state machine

**Decision**: The attempt workflow uses explicit phases: initialized -> profile detected ->
sentinel presented -> damage presented -> samples observed -> artifact quality evaluated ->
classified. The public or observable surface must expose or wrap a model/message/effect boundary
where stateful I/O is introduced.

**Rationale**: The constitution requires an Elmish/MVU boundary for stateful or I/O workflows.
Proof attempts include GL host setup, drawing, readback, filesystem artifacts, and failure
classification. Making those effects explicit gives tests a pure transition surface and keeps
host interaction isolated in interpreters.

**Alternatives considered**: Implement the proof attempt as one imperative function returning a
record. Rejected because it hides partial states and makes unsupported-host and artifact-quality
classification harder to test precisely.

## Decision 4: Accept attempts only from fresh, current-run, non-synthetic artifacts

**Decision**: A proof attempt can be accepted only when required sentinel and damage artifacts are
present, decodable, non-blank, fresh for the current run, non-synthetic, and tied to the attempt
id, host profile, proof method, and frame role.

**Rationale**: The known readiness gap is that deterministic and environment-limited evidence
exists but no accepted partial-redraw artifacts exist. Accepting stale, blank, synthetic, or
ambiguous artifacts would weaken the safety policy the previous features established.

**Alternatives considered**: Allow synthetic or deterministic offscreen artifacts to stand in for
capable-host proof. Rejected because Feature 152 already marks synthetic evidence as rejection-path
coverage only.

## Decision 5: Require an explicit set of exactly three matching attempts

**Decision**: The accepted proof set records exactly three selected attempts. Each attempt must be
accepted, fresh, same-host, same-proof-method, and artifact-quality accepted. If additional
attempts exist, readiness may link them as supporting context but the accepted proof-set identity
names the exact three attempts used.

**Rationale**: The feature requirements say the interpreter must aggregate exactly three fresh
matching capable-host attempts. An explicit selected set avoids accidental acceptance from mixed,
stale, or host-drifted evidence directories.

**Alternatives considered**: Accept any list containing at least three good attempts. Rejected
because it makes proof-set identity ambiguous and can hide mismatched or failed attempts in the
same directory.

## Decision 6: Unsupported hosts remain environment-limited and non-accepting

**Decision**: Missing display, missing GL renderer, failed context creation, readback
unavailability, capture permissions, timeout, or host setup failure produce
`environment-limited` or `failed` with a specific reason and zero accepted partial-redraw
artifacts.

**Rationale**: CI and local machines may not have a capable presentation host. Those runs should
remain useful as regression evidence without weakening proof acceptance.

**Alternatives considered**: Skip unsupported-host runs without writing readiness evidence.
Rejected because the spec requires unsupported-host behavior to be explicit and reviewable.

## Decision 7: Publish one readiness summary that states remaining gates

**Decision**: `validation-summary.md` is the review entry point. It links attempts, proof-set
status, unsupported-host behavior, fallback status, compatibility impact, and remaining parity and
timing gates.

**Rationale**: Reviewers and package consumers must not infer that partial redraw or performance is
accepted just because live attempts can run. One summary keeps the proof evidence and non-claim
language together.

**Alternatives considered**: Only write per-attempt files. Rejected because per-attempt evidence
does not answer the readiness question or the remaining-gates question in under five minutes.

## Decision 8: Add public surface only when needed for proof vocabulary or consumer validation

**Decision**: Prefer extending existing `CompositorProof` and readiness helper surfaces only when
the interpreter needs package-visible proof attempts, proof-set decisions, or consumer validation
tokens. Keep harness-only formatting and file-writing private to `Rendering.Harness`.

**Rationale**: This is a Tier 1 feature, but the constitution still requires minimized
dependencies and public drift. Public API changes should be justified by consumer-facing proof or
readiness contracts, not by implementation convenience.

**Alternatives considered**: Expose every artifact and command record as public package types.
Rejected because that would expand the stable package contract beyond what reviewers and consumers
need.
