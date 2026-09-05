# FS.GG item lifecycle ledger

Every claimed item has one externally durable, append-only phase ledger on its canonical GitHub issue
and one private immutable runtime-usage receipt per phase. Each event is posted as an
`fsgg:item-lifecycle/v1` structured issue comment through `fsgg-coord comment create`, then read back and
digest-verified. The live claim holder is the single append authority: critics and other actors return
timestamped phase/usage receipts to it, and it records their `actor` unchanged. The command refuses a
lifecycle write by any other worker or to any target other than the canonical item. Optional immutable
normal-item exports use:

```text
logs/items/<owner>.<repo>/<issue-number>/lifecycle.jsonl
```

A roadmap export uses its roadmap path instead:

```text
logs/roadmap/<roadmap-slug>/<run-id>/<unit-id>.jsonl
```

Create the ledger when work begins. Never edit, delete, reorder, or renumber accepted comments. Bind
each event to the previous digest, item, live claim generation, and review generation when applicable.
A status reply is a projection of the ledger; prose and a GitHub timestamp are evidence, not substitutes.

The candidate branch is never the live authority. Review, merge, protected-main, projection, and cleanup
facts occur after its exact head is fixed; appending them inside that branch would invalidate the review
and can never converge. A tracked JSONL file is only an immutable export of a closed interval and never
gates the PR that contains it. Export happens afterward under a distinct source-bound item. Raw usage
CSVs and local session paths remain private and untracked. A phase receipt may grow while that phase is
active, but freezes when its digest is cited; later phases use new receipts so appends cannot invalidate
earlier events.

GitHub comment ids are the server-assigned total order. For a legacy or pre-upgrade fork, export accepts
the lowest-id child of a predecessor and reports every later sibling on stderr as preserved rejected-fork
evidence. Never edit or delete either comment. After create, the command re-reads complete live authority
and elects the lowest GitHub comment id for the same run/unit/revision/predecessor key; only that call
succeeds. Concurrent calls by the same claim worker therefore still yield one accepted successor.

## Phase events

Each JSONL line is one phase transition. The shared v1 fields are `schema_version`, `run_id`, `unit_id`,
`item`, `sequence`, `phase_order`, `phase`, `event`, `at`, `actor`, `model`, `source`, `evidence`,
`actual_minutes`, `historical_durations_minutes`, `historical_average_minutes`, `token_usage`, `tooling`,
`revision`, `previous_digest`, `digest`, and `authority`.

- `revision` is contiguous. `previous_digest` is null only at revision 1 and otherwise equals the prior
  event digest. `digest` is SHA-256 over compact sorted-key JSON excluding `digest` itself.
- `authority` binds `github_issue_comment`, the canonical issue subject, and live claim generation.

- `item` contains the canonical GitHub `repo`, positive issue `number`, and exact HTTPS issue `url`.
- `phase` is a lowercase stable identifier. Every numbered review, repair, recovery, merge, verification,
  projection, and cleanup pass is distinct.
- `event` is `started`, `completed`, `blocked`, or `resumed`. Independent actor phases may overlap;
  transitions remain ordered within each phase. The status line still names exactly one primary active
  process position and notes any concurrent phase in prose.
- `at` is the actual observation time in canonical UTC; never pre-author another actor's start. Terminal
  duration is elapsed wall time from the first start, rounded to the nearest whole minute. Historical
  averages use a supplied validated prior-event corpus for the same phase and tooling fingerprint.
- `actor` is the minted worker, critic, or host identity.
- `tooling` binds `ledger_schema` plus `runtime`, `coordination`, `sdd`, and `contracts` components.
  Each component records its exact name/version/source or an explicit `unavailable`/`not_applicable`
  reason. For Codex, take the runtime version from `session_meta.cli_version`; for Claude Code, take it
  from status-line `version`. Capture `fsgg-coord --version`, `fsgg-sdd --version`, and the governed
  contracts version at phase start. Historical comparisons must group by the canonicalized `tooling`
  object; do not blend durations or token rates from different toolchain fingerprints.
- `model` records `provider`, exact `name`/variant, optional `effort`, and authoritative `source`. A model
  switch begins a new phase. Never derive a model from an agent nickname.
- `token_usage` on a start/resume is `pending`. A terminal event is drafted locally at the boundary but
  posted only after the response has finished and the collector can seal it as `measured` or
  `unavailable`. Never post a pending terminal comment and never edit an accepted comment. `unavailable`
  is allowed only after the named authoritative
  source was checked and had no usable record.

Measured phase usage records total input, cached input, cache-write input, output, reasoning output, and
total tokens. The validator joins its session/turn identities to the private report and requires counts,
exact model, and all four tool versions to match. Cached/cache-write counts are subsets or components of input according to the provider;
`total = input + output`, and reasoning output is already included in output. Bind the measurement to one
or more `session_id`/`turn_id` rows in the usage report. When a phase spans turns, sum completed-turn
records. When a turn spans phases, do not invent a split: keep the affected phase pending until a boundary
receipt exists, or mark it unavailable with that exact limitation.

## Runtime collection

Each private phase receipt uses this stable report header:

```text
timestamp,task,session_id,thread_id,turn_id,response_id,provider,model,effort,runtime_version,coordination_version,sdd_version,contracts_version,ledger_schema,input,cached_input,cache_write_input,output,reasoning,total,turn_input,turn_cached_input,turn_cache_write_input,turn_output,turn_reasoning,turn_total,thread_input,thread_cached_input,thread_cache_write_input,thread_output,thread_reasoning,thread_total,source
```

Codex persists `token_usage_record` rows under `~/.codex/sessions/YYYY/MM/DD/`. Each row binds one
`response_id`'s request usage plus current turn and full-thread totals; the matching `turn_context`
supplies the exact model variant and effort. Capture all completed responses so long-running goal turns
and user-steered continuations do not collapse into one misleading final row. Public provenance is a
content digest such as `codex-session-jsonl:sha256:<digest>`, never an absolute local path. Run:

```sh
scripts/fsgg-coord telemetry usage collect codex \
  --session-file <rollout.jsonl> --task <item/phase> --turn-id <turn-id> \
  --coord-version <version> --sdd-version <version> --contracts-version <version> \
  --all-responses --append <private-untracked-phase-usage.csv>
```

Never append another phase to a receipt whose SHA-256 is already cited. The validator accepts repeated
`--usage` arguments and resolves each event against the immutable receipt digest it names.

The local Codex JSONL is an internal interface, so the collector validates every required key and fails
closed on shape drift. For direct API work, prefer the stable response `usage` object.

Claude Code exposes `session_id`, `prompt_id`, `model.id`, effort, and the latest API response's input,
cache-creation, cache-read, and output usage to a status-line command. Persist those snapshots after each
assistant message. A `Stop` hook runs after the main response finishes and receives `session_id` and
`transcript_path`; use it to append the saved snapshot. `SubagentStop` supplies a separate
`agent_transcript_path`, so subagent usage must remain separate. The collector derives a stable response
identity from session, prompt, timestamp, model, and usage, so repeated hook delivery is idempotent. The status-line value is one latest API
response, not a whole multi-call turn; aggregate only captured requests. Claude's ordinary usage does not
separate reasoning from output, so the report leaves `reasoning` empty rather than guessing. For
non-interactive work, `--output-format json`/`stream-json` or the Agent SDK result is preferred.

### Supervising a completed child

Post-response collection is a parent responsibility, not a promise delegated to the response that is
ending. Every child handoff returns its phase, actor, start/end timestamps, exact session and turn (or
Claude prompt/transcript) identity, model/effort, tool versions, evidence, and an **unposted** terminal
event draft whose token state is `pending final usage`. The supervising parent waits for the child to be
terminal, then:

1. resolves the named local session/transcript and confirms its terminal usage record was written after
   the child's last response;
2. runs `fsgg-coord telemetry usage collect` into a new private immutable receipt scoped only to that phase;
3. seals and posts the terminal lifecycle event with `measured` usage and the receipt digest; and
4. rejects the handoff, host acceptance, cycle completion, or Done while that seal is absent.

For Codex, a parent may use a unique phase marker only when the child could not return the session/turn
identity; multiple matching sessions are an attribution failure, not permission to choose one. For
Claude Code, consume the `SubagentStop` `agent_transcript_path`, never the parent's transcript. A strict
post-completion lookup that finds no unique terminal record, or a collector failure caused by internal
schema drift, may seal `unavailable` only when the exact lookup and validation failure are recorded.
“Final usage is written after this response” proves the state is pending and is forbidden as an
`unavailable` reason. When an immutable legacy terminal event already contains that reason, append a
separate `telemetry-reconciliation-<phase>` recovery phase carrying the measured record before advancing.

## Validation and completion

Seal, post, and export without hand-authoring digests or editing accepted comments:

````sh
set -euo pipefail
gh api repos/<owner>/<repo>/issues/<number>/comments --paginate --slurp > <comments.json>
scripts/fsgg-coord telemetry lifecycle export-comments \
  --run <run-id> --unit <unit-id> --comments <comments.json> --output <exported-lifecycle.jsonl>
scripts/fsgg-coord telemetry lifecycle seal-successor \
  --run <run-id> --unit <unit-id> --existing <exported-lifecycle.jsonl> \
  --draft <one-unposted-event-without-chain-fields.json> \
  --usage <every-cited-private-phase-receipt.csv> --output <one-sealed-successor.json>
event="$(<one-sealed-successor.json>)"
test -n "$event"
printf '<!-- fsgg:item-lifecycle/v1 -->\n```json\n%s\n```\n' "$event" > <owned-comment-file>
FSGG_WORKER=<worker> scripts/fsgg-coord comment create <item> <item> <owned-comment-file> --json
````

The exporter accepts only exact markers, orders by immutable GitHub comment id, and rejects edited
lifecycle comments. The successor sealer reads and validates the full existing chain plus exactly one
draft, derives the next revision/digest, and emits only that new event; it cannot repost the prefix or
seal an invalid terminal event. Run the recipe fail-fast exactly as shown: no comment write may run after
a failed or empty seal. A correction is a later digest-chained event, never an edit.

Run the validator at each handoff and gate. A normal item uses any stable lowercase `run_id` and path-safe
`unit_id`; a roadmap supplies its cycle values:

```sh
scripts/fsgg-coord telemetry lifecycle validate \
  --run <run-id> --unit <unit-id> --log <exported-lifecycle.jsonl> \
  --usage <private-phase-1.csv> --usage <private-phase-2.csv> \
  [--history-report <validated-history.csv>]
```

Use `--require-terminal --require-reconciled` before a done stamp or roadmap roll-up. The first rejects an
active or blocked phase; the second keeps the explicit completion intent visible even though the compiled
validator already rejects every terminal `pending` token record. A legacy terminal `unavailable` whose
reason says usage was still pending because the child response had not finished is not genuine
unavailability. Append a distinct completed `telemetry-reconciliation-<original-phase>` phase with measured
usage and exactly one evidence entry
`supersedes-lifecycle-digest:<64-hex-digest-of-the-original-terminal-event>`. The validator requires the
target digest to exist, to identify an unavailable record for that exact original phase, and to be
superseded only once; prose, a phase name alone, or a later usage receipt without this digest does not
reconcile it. Genuine post-completion lookup or collector-schema failures may remain `unavailable` when
their exact lookup/validation failure is recorded in the reason and evidence. Validate implementation changes through
the CLI test suite and the frozen black-box parity corpus.
