# Independent review and material filing

Review authority is the append-only structured ledger. Every decision is a digest-chained JSON
record posted with `scripts/fsgg-coord review record <ref> <draft.json> --pr <n> --json`.
Narrative prose, quoted JSON, and historical marker-shaped text carry no authority.

## Wire contract

<!-- BEGIN GENERATED: fsgg-protocol:review-policy -->
*Generated structured review contract. The digest validator and state machine consume these exact values.*

| fact | value |
|---|---|
| schema | `fsgg.coord.review-decision/v2` |
| kinds | `initial, confirmation, escalation, repair-phase, acceptance` |
| ordinary repair ceiling | 3 |
| repair-phase ceiling | 10 |

<!-- END GENERATED: fsgg-protocol:review-policy -->

- Marker: `<!-- fsgg:review-decision/v2 -->` followed immediately by one JSON object.
- Schema: `fsgg.coord.review-decision/v2`.
- Kinds: `initial`, `confirmation`, `escalation`, `repair-phase`, `acceptance`.
- Revisions start at one and are contiguous. `previousDigest` binds the prior canonical record.
- Every record binds the exact PR subject, 40-hex head SHA, minted critic identity, policy version,
  timestamp, kind-specific fields, and its canonical digest.
- Generic route identities such as `fsgg-critic-normal` are not minted critic identities.

Prepare a draft with the schema fields; the writer seals revision, predecessor, and digest from the
live ledger. It rejects malformed, gapped, stale, or concurrently advanced ledgers and never falls
back to prose.

## State transitions

<!-- BEGIN GENERATED: fsgg-protocol:lifecycle-policy -->
*Generated lifecycle boundary. These are machine-owned prerequisites; judgement about the work remains authored.*

Required housekeeping: `host-identity`, `stale-claim`, `engine-currency`, `pending-writes`, `reconcile`, `triage`.

Host acceptance fields: `accepted-head`, `initial-review`, `latest-confirmation`.

Terminal transition evidence: `merge` → `post-merge-obligations` → `done-stamp`.

<!-- END GENERATED: fsgg-protocol:lifecycle-policy -->

<!-- BEGIN GENERATED: fsgg-protocol:ledger-policy -->
*Generated ledger schema. The receipt id binds these fields; prose does not substitute for the ledger.*

Schema: `fsgg.coord.planning-receipt/3`.

Observation fields: `kind`, `observedAt`, `sourceSha`, `outcome`, `receiptId`.

Receipt fields: `schema`, `observedAt`, `sourceSha`, `complete`, `consolidationApproved`, `observations`, `contentIntakes`, `contentDispositions`.

<!-- END GENERATED: fsgg-protocol:ledger-policy -->

Mint a critic with `eval "$(scripts/fsgg-coord whoami --mint)"`. The `initial` record has verdict
`pass` or `changes-required`, round zero, and no review backlinks. A passing record carries either
four ordered meaningful-route evidence strings or exactly one not-meaningful reason. Set
`diffAuditRequired` when mechanically discovered semantic replacements exist.

After material repair, the same critic posts a `confirmation` for the new exact head. Rounds are
contiguous and one-based; `initialReview` names the initial comment and `precedingReview` names the
immediately prior structured comment. At most three ordinary confirmations are allowed.

If that ceiling is exhausted, append `escalation` then `repair-phase`. Escalation without the typed
repair-phase fact has no authority. Repair phase permits at most ten confirmations before human
escalation.

Only the host posts `acceptance`, after the latest critic record is `pass` and all checks are green.
It uses verdict `accepted`, binds the exact head, initial URL and latest critic URL, follows the
critic comment, and preserves generation critic identity. When diff audit is required it carries
base64 typed receipts; the engine recomputes live inventory and refuses missing, malformed, stale,
partial-coverage, mixed-head, or byte-drifted evidence.

Immediately before merge run:

`scripts/fsgg-coord landable <pr> --repo <repo> --wait --sha <head> --require fsgg:review-decision/v2`

A moved head retires the accepted older generation without rewriting it and requires a fresh initial
generation. Backlinks, head bindings, critic continuity, and digest continuity fail closed.

## Independent critic boundary

The critic is independent of implementation context: provide the roadmap/spec, diff, exact head and
verification evidence, but not hidden implementation reasoning. The same critic handles repairs in a
generation. Use the explicit succession workflow if replacement is unavoidable; prose is never
authority.

Report concrete findings first, ordered by severity and linked to files or commands. A pass means no
unresolved material finding remains at the reviewed head. The host validates the ledger and checks
itself and never translates a prose verdict into a structured pass.
