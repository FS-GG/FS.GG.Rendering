# Mechanical reconciliation

`scripts/fsgg-coord reconcile` owns classification. Do not recreate its finding table with `jq`.
The five typed chores are stale-claim collection, claim/Status lag, closed issue/Done lag,
cleared-blocker/Ready lag, and open-blocker/Blocked lag. `--apply` performs only those remedies.

Use `--repo NAME` to narrow the reported and applied subjects; omit it for the org-wide pass.
Use `--json` when another tool consumes the dry-run result. Apply after inspecting that result;
`--apply` is deliberately human-readable so mutation output cannot corrupt a JSON document.

<!-- BEGIN GENERATED: fsgg-protocol:reconcile-rules -->
<!--
  DO NOT EDIT THIS REGION. It is emitted from src/FS.GG.Coord.Core/Protocol.fs by
  scripts/generate-projections, and `projections` in CI fails on any diff.

  These rules were restated by hand here, and a RECONCILER restating them is the sharpest form
  of the problem: this pass exists to correct the projection, so a rule it gets wrong is a rule
  it enforces WRONG across the whole board. The hand-written copies agreed with each other for
  as long as they existed (#916). Edit Protocol.fs and regenerate.
-->

*Generated from the typed core. The engine that resolves your blockers is the engine that wrote
this. The full rule set, with the incident behind each one, is in
[intra-repo-parallel-work](../../intra-repo-parallel-work/SKILL.md).*

**`Paths:` is a declaration, and a fenced one is a QUOTATION**

Declare the touch-set as a `Paths:` line at up to three leading spaces. A `Paths:` line INSIDE a fenced code block is a quotation of the grammar, not a use of it — the protocol docs quote it constantly. `Paths: none` is a SENTINEL meaning "this item deliberately has no touch-set", and it is not the same fact as having forgotten one.

**A MERGED blocker is RESOLVED; an unreadable one BLOCKS**

`Blocked by` clears on CLOSED **or MERGED**. It does not clear on OPEN, on a blocker whose state could not be read (unverifiable), or on prose that is not an issue ref at all (unparseable) — all three BLOCK.

**A read that did not happen may never render as a confident answer**

An error, an empty result, and a legitimate "no" are three different facts. A failed board scan is not an empty board; a failed marker read is not an unheld item; an unread issue body is not an undeclared touch-set. Every one of them fails CLOSED and says which it was.

<!-- END GENERATED: fsgg-protocol:reconcile-rules -->

<!-- BEGIN GENERATED: fsgg-protocol:blocker-states -->
<!--
  DO NOT EDIT THIS REGION. It is emitted from src/FS.GG.Coord.Core/Protocol.fs by
  scripts/generate-projections, and `projections` in CI fails on any diff.

  The hand-written copy NAMED its own source — "the five cases of the engine's `BlockerState`"
  — and was still a copy. Naming a source is not reading it. Generatable only since #1012 gave
  the vocabulary an owner in Core; before that it was two private INVERSE copies outside it, and
  typing the cases into Protocol.fs would have been a THIRD (#865).

  TWO sources, because this region states two different facts. The `.state` strings and the case
  list come from Types.fs (`BlockerState`, `blockerStateWireName`) — that is where a NEW state or
  a renamed one belongs. The `holds?` bit and the prose are Protocol.fs. Regenerate either way.
-->

*Generated from the typed core: `Types.blockerStateWireName` writes these strings, so the engine
that emits `.state` is the engine that wrote this table. Lower case, deliberately — an issue's own
`state` is UPPER case on the wire and a blocker's is not, and the two conventions are not to be
"unified" (§3).*

| `.state` | holds? | what it means |
|---|---|---|
| `open` | **YES** | The blocker is open. It HOLDS. |
| `closed` | no | The blocker issue is closed. It does not hold — the work it named is finished or abandoned. |
| `merged` | no | The blocker is a MERGED pull request. It does not hold. A rule that cleared only on CLOSED would unblock when the PR was ABANDONED and block forever once it was FINISHED — the gate opening precisely when the work is thrown away (#476). |
| `unknown` | **YES** | The ref parsed and its state could not be read. It HOLDS: "I could not look" is not "I looked and it is fine" (#266). Usually an off-board ref the scan could not resolve — board it, and it becomes `open` or clears. |
| `unparseable` | **YES** | The `Blocked by` text is not an issue ref at all. It HOLDS: prose in a dependency field is a question nobody answered, and a field this pass cannot read is not a field it may declare empty. |
<!-- END GENERATED: fsgg-protocol:blocker-states -->

<!-- BEGIN GENERATED: fsgg-protocol:snapshot-shape -->
<!--
  DO NOT EDIT THIS REGION. It is emitted from src/FS.GG.Coord.Core/Protocol.fs by
  scripts/generate-projections, and `projections` in CI fails on any diff.

  The hand-written literal this replaced was ILLUSTRATIVE and read as NORMATIVE — the ambiguity
  was the defect, not any one of its errors. It had the key order wrong in two places and a
  leaseMinutes that matched no default.

  TWO sources, as blocker-states has. The schema string and the key list are Protocol.fs
  (snapshotSchema, snapshotKeys); the document they describe is written by Scan.snapshot and
  read by Snapshot.parse, and ScanRoundTripTests PINS Protocol against both — Core states a
  shape it does not render, which is #1058's ownership call and the test is its price.
-->

*Generated from the typed core. The keys are in the order `Scan.snapshot` WRITES them, and
`ScanRoundTripTests` drives the real writer to assert it — so this table cannot disagree with the
document `scan --json` actually emits. `reconciled` is the column that matters: it says which
keys a reconciler may select on.*

```json
{ "schema": "fsgg.coord.snapshot/1", "allowBacklog": …, "limit": …, "leaseMinutes": …, "items": [ … ], "inFlight": [ … ] }
```

| key | reconciled? | what it carries |
|---|---|---|
| `schema` | no | The document's contract, `fsgg.coord.snapshot/1`. `Snapshot.parse` REFUSES a document without it rather than defaulting — a malformed snapshot is an error, never a default. |
| `allowBacklog` | no | Whether the scan was asked to include `Backlog`. The scan's own parameter, echoed back: `lanes` reads it from HERE rather than taking its own flag (#991), which is why it is on the document at all. |
| `limit` | no | The `-n` cap the scan was asked for, or `null` for uncapped. The scan's parameter, not a board fact. |
| `leaseMinutes` | no | The lease window the scan resolved staleness against (`FSGG_CLAIM_LEASE_MIN`, default 120). The scan's parameter. The prose this replaced hardcoded `90`, which was neither the default nor a fact — the clearest evidence a reader cannot tell an example from a contract when both are hand-typed. |
| `items` | **YES** | The board rows — THE reconcilable key, and the only one. Each carries `owner`, `repo`, `number`, `status`, `state`, `body` and `blockers`. Named `items` on the wire, not `candidates`: that is what the parser reads. |
| `inFlight` | no | What live claims already reserve, each naming its HOLDER. A scheduler's input, not a column to reconcile: `check-board` acts on the MARKER through `who`, which carries the lease state this does not. |
<!-- END GENERATED: fsgg-protocol:snapshot-shape -->

Manual REST reads are reserved for `lint` findings whose facts were unknown. Read the exact issue or
PR named by the finding; preserve unknown when the read fails. Never use a broad issue-body rewrite as
a board repair.
