# Research: Validation Lane Runner

## Decision: Extend `Rendering.Harness.ValidationLanes`

**Rationale**: The repository already has a thin `scripts/run-validation-lanes.fsx`
wrapper and a `ValidationLanes` module with lane definitions, lane results,
summary rendering, and a pure `Model`/`Msg`/`Effect` boundary. Extending that
surface keeps the feature close to existing package-feed validation work and
avoids a second runner with competing status tokens.

**Alternatives considered**: A new standalone shell script was rejected because
timeouts, cancellation, structured summaries, and cross-platform process control
are safer in F#. A new external task runner was rejected because no new
dependency is needed and the constitution favors narrow repo-owned checks.

## Decision: Default to required lanes, keep aggregate optional

**Rationale**: The retrospective problem was an opaque full-solution validation
gate. Required readiness should come from smaller named lanes. The aggregate
solution lane remains useful as an optional signal, but it must never hide or
override required lane failures.

**Alternatives considered**: Running all lanes by default was rejected because it
would include the optional aggregate and recreate the original opaque gate.
Removing the aggregate lane was rejected because reviewers still benefit from
seeing its incomplete, timed-out, or passed state separately.

## Decision: Use stable lane ids and readiness roles

**Rationale**: The spec requires lanes to declare whether they are required,
optional, or informational. A boolean `Required` flag is insufficient for review
semantics and future informational diagnostics. Stable ids also make CLI
selection, summaries, and result records deterministic.

**Alternatives considered**: Keeping only `Required: bool` was rejected because
it cannot represent informational lanes. Deriving roles from naming conventions
was rejected because summaries need explicit review semantics.

## Decision: Reject invalid requests before starting work

**Rationale**: Unknown lane names, duplicate lane definitions, duplicate result
ids, unwritable evidence roots, and unsafe schedules must be caught before any
child process starts. This prevents partial evidence that looks like a validation
attempt when the request itself was invalid.

**Alternatives considered**: The current behavior of running known selected lanes
and reporting unknown lanes afterward was rejected because it violates the spec's
unknown-lane edge case. Warning-only validation was rejected because missing or
ambiguous evidence must fail closed.

## Decision: Record no-progress as a timeout subtype

**Rationale**: The operator cares whether a lane exceeded its total budget or
stopped producing visible activity. The structured result should expose both the
status token and the timeout kind. To preserve existing Feature 163 concepts, a
specific `no-progress-timeout` token may be rendered, but readiness rules treat
it as a non-passing timeout.

**Alternatives considered**: Treating no-progress as a plain failure was rejected
because it loses the stalled-run diagnosis. Treating it as passed with a caveat
was rejected because incomplete or stalled validation is not readiness success.

## Decision: Add explicit infrastructure errors

**Rationale**: The spec requires infrastructure-error outcomes, especially for
log/evidence write failures. Process-start failures, unwritable evidence
directories, invalid lane configuration, and result-write failures must be
distinguishable from validation failures in the tested product.

**Alternatives considered**: Reusing `environment-limited` was rejected for
general infrastructure failures because it incorrectly suggests an accepted host
limitation. Reusing `failed` was rejected because reviewers need to know whether
the validation command ran and failed or the runner itself could not collect
evidence.

## Decision: Store each run under a run id

**Rationale**: Re-running a lane must not silently overwrite prior evidence. A
run-id child directory gives each session separate logs, results, summaries, and
diagnostics while allowing readiness evidence to be copied or committed as a
package.

**Alternatives considered**: Writing directly to `readiness/lanes/<lane-id>` was
rejected because it overwrites previous logs. Appending timestamps only to log
files was rejected because summaries and structured records still need a coherent
session root.

## Decision: Keep execution sequential by default

**Rationale**: Sequential execution is enough for Feature 166 and prevents the
known file-lock race where two `dotnet test` commands share a project/config
output directory. Lane definitions still declare concurrency groups and output
scopes so future parallel execution can serialize, isolate, or reject unsafe
pairings before work starts.

**Alternatives considered**: Parallelizing all independent lanes was rejected for
this slice because correctness and evidence clarity matter more than throughput.
Relying on operator discipline was rejected because the retrospective identified
this exact validation trap.

## Decision: Emit runner heartbeats

**Rationale**: A silent lane needs operator-visible progress at least every
60 seconds. The runner can report the active lane, elapsed time, timeout budget,
and last activity timestamp even when the child process is silent.

**Alternatives considered**: Depending only on child-process output was rejected
because some test commands can stop producing output. Waiting until timeout was
rejected because the operator cannot tell which lane is active.

## Decision: Preserve direct validation commands

**Rationale**: The lane runner is an orchestration layer. Contributors must still
be able to run focused commands directly for local debugging and existing
workflows.

**Alternatives considered**: Replacing docs with only lane-runner commands was
rejected because it would make simple focused debugging heavier and contradict
FR-014.
