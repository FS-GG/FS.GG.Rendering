# Mailbox and board operations

Use `issues` for REST-backed dedupe, `add` for board membership, `set-field` for explicit board writes,
`child` for hierarchy, and `say`/`inbox`/`room open` for worker communication. Budget exhaustion queues
eligible writes; `flush` replays them, and a fresh read must confirm the result.

<!-- BEGIN GENERATED: fsgg-protocol:filing-rules -->
<!--
  DO NOT EDIT THIS REGION. It is emitted from src/FS.GG.Coord.Core/Protocol.fs by
  scripts/generate-projections, and `projections` in CI fails on any diff.

  These rules were restated by hand here, and #916 measured why that is not survivable: the
  hand-written copies AGREED with each other for as long as they existed and were wrong the
  whole time. Edit Protocol.fs and regenerate.
-->

*Generated from the typed core. The engine that refuses your `Paths:` line is the engine that
wrote this. The full rule set, with the incident behind each one, is in
[intra-repo-parallel-work](../../intra-repo-parallel-work/SKILL.md).*

**`Paths:` is a declaration, and a fenced one is a QUOTATION**

Declare the touch-set as a `Paths:` line at up to three leading spaces. A `Paths:` line INSIDE a fenced code block is a quotation of the grammar, not a use of it — the protocol docs quote it constantly. `Paths: none` is a SENTINEL meaning "this item deliberately has no touch-set", and it is not the same fact as having forgotten one.

**The touch-set grammar — it is NOT a glob language**

supported: an exact path ('src/Foo.fs'), or a directory prefix ('src/Foo', 'src/Foo/*', 'src/Foo/**'). There is no glob matcher: a leading '**/' or an interior '*' matches nothing — spell the paths out.

**A MERGED blocker is RESOLVED; an unreadable one BLOCKS**

`Blocked by` is a Projects v2 board FIELD, not a body line — the same medium split as `Paths:` and its own fence rule, in reverse: `Paths:` lives in the body and a `Blocked by` FIELD is the only place this dependency is recorded. A `Blocked by:` line written into the issue BODY is inert: nothing that clears a blocker reads the body, so it looks like a declaration and does nothing. Write the edge with `set-field <ref> "Blocked by" <ref>`. Once the edge is on the field: `Blocked by` clears on CLOSED **or MERGED**. It does not clear on OPEN, on a blocker whose state could not be read (unverifiable), or on prose that is not an issue ref at all (unparseable) — all three BLOCK.

<!-- END GENERATED: fsgg-protocol:filing-rules -->

<!-- BEGIN GENERATED: fsgg-protocol:board-statuses -->
<!--
  DO NOT EDIT THIS REGION. It is emitted from src/FS.GG.Coord.Core/Protocol.fs by
  scripts/generate-projections, and `projections` in CI fails on any diff.

  These six strings were a table row here, hand-kept, in BOTH skill roots. A drifted option is
  not a misprint: a reconciler selects it in jq, matches nothing, and reports a CLEAN BOARD over
  a board it never read (#476).

  SIX, NOT SEVEN. `BoardStatus` has a seventh case — `NoStatus`, wire form `""` — and it is
  the ABSENCE of a column rather than an option a filer can select. Protocol.fs drops it in a
  total match, so a new case cannot join the union without being classified.
-->

*Generated from the typed core: `Types.statusWireName` writes these option names, so the engine
that reads a column back is the engine that wrote this table. `startable?` is
`Schedulability.columnStartability` — the predicate `take` schedules on, not a copy that agrees
with it today.*

| option | startable? | what it means |
|---|---|---|
| `Backlog` | only with `--include-backlog` | Filed, not triaged. The honest resting place for a finding nobody has scheduled yet. A scheduler passes it over unless the caller asks for it (`--include-backlog`), so a park here is invisible to a plain `take` BY DESIGN — that is what parking means. |
| `Ready` | **YES** | Triaged and startable. The only column a scheduler hands out unconditionally, so an item that is real work belongs here or it will not be worked. |
| `In progress` | no | A worker holds it — the claim's own footprint, written by `claim` and reset by `release`. Not a column to set by hand: the claim marker is the lock, and this column is only its shadow. |
| `Blocked` | no | Something else must land first, and `Blocked by` names what. Mirrors the `blocked` label. Not offered — and an item sitting here whose blockers have all resolved is startable work no `take` will ever offer, which is why /check-board re-verifies them. |
| `In review` | no | The work is written and its PR is open. Not offered: claiming it would duplicate an implementation already in flight. |
| `Done` | no | Finished and merged, and the stamp says so. `done --flip` sets it once it confirms the merge; setting it by hand is how a board starts lying. |
<!-- END GENERATED: fsgg-protocol:board-statuses -->

Do not hand-edit project item IDs or use git as a queue. The issue is the durable request; the board is
its projection and scheduling ledger.

Repo-scope phase projection follows the board schema: a `net` item `P8 Net`; preserve that mapping
when filing or reconciling a Net request.
