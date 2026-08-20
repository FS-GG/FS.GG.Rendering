---
name: fs-gg-feedback-report
description: Capture and synthesize evidence-backed feedback about the complete scaffolded FS.GG development experience. Use during a feature or milestone to checkpoint friction, rework, capability gaps, and unexpectedly effective patterns, and at cycle end to write and validate a durable report covering scaffolding, guidance, skills, SDD, implementation APIs, build, testing, evidence, runtime quality, performance, documentation, packaging/upgrades, and worker/PR orchestration.
---

# fs-gg-feedback-report

Preserve development experience while it is fresh, then turn it into one comparable report per
cycle. Use two modes:

- **Checkpoint:** append a small structured event when something materially helps or hinders work.
- **Finalize:** draft, independently critique, verify, then preserve a schema-v2 report and bound audit.

Do not recreate the retired Spec Kit hook fabric. Checkpoints are lifecycle-independent and
agent-invoked. Do not checkpoint routine success; capture events that teach the scaffold provider,
tooling, package owner, skill author, or orchestrator something reusable.

## Checkpoint mode

Checkpoint immediately after:

1. scaffold/onboarding and the first build;
2. lifecycle authoring, before implementation;
3. the first implementation/test/evidence loop;
4. verify, ship, and PR orchestration;
5. any misleading instruction, avoidable retry, workaround, missing capability, or unusually
   effective composition that would be hard to reconstruct later.

Run the bundled tool from the product root:

```sh
dotnet fsi .agents/skills/fs-gg-feedback-report/scripts/feedback-tool.fsx -- checkpoint \
  --cycle <cycle-id> \
  --phase <phase> \
  --surface <surface-id> \
  --kind <kind> \
  --summary "<one observed fact>" \
  --evidence "<command, artifact, diff, screenshot, source location, timing, or interaction>" \
  --cost "<avoidable retries, edits, or elapsed time; use none when zero>" \
  --owner "<repo/component likely able to improve it>"
```

The tool appends one JSON object to `feedback/checkpoints/<cycle-id>.jsonl`. Use a stable,
lowercase cycle id such as `018-map-motion-m5-interpolation-scene-cost`.

If the capture loop was active and exercised its intended phases but no material event met the
checkpoint threshold, record that fact explicitly instead of inventing a positive pattern or defect:

```sh
dotnet fsi .agents/skills/fs-gg-feedback-report/scripts/feedback-tool.fsx -- activate \
  --cycle <cycle-id> \
  --phases "scaffold-onboarding;implementation-test-evidence;verify-ship-pr" \
  --evidence "command:dotnet test;file:readiness/ship-summary.json" \
  --reason "The exercised phases completed without reusable friction, gaps, or positive patterns."
```

This writes the immutable, schema-versioned
`feedback/checkpoints/<cycle-id>.activation.json`. Its `exercisedPhases` and `evidence` arrays prove
that capture was active; `reasonNoEventQualified` explains the zero-event result without carrying
event-only `kind`, `surface`, `summary`, `cost`, or `owner` fields. A cycle must contain either event
JSONL or a zero-event activation receipt, never both.

Validate the complete state from the product root:

```sh
dotnet fsi .agents/skills/fs-gg-feedback-report/scripts/feedback-tool.fsx -- \
  validate-checkpoint-state --cycle <cycle-id>
```

The validator accepts a valid event file or a valid zero-event receipt and fails closed when state is
missing, malformed, unreadable, contradictory, or exposes private evidence material.

Allowed kinds:

- `positive-pattern`
- `defect`
- `friction`
- `capability-gap`
- `quality-gap`
- `documentation`
- `orchestration`

Allowed surface ids:

- `scaffolding`
- `onboarding-guidance`
- `skills`
- `sdd-authoring`
- `implementation-apis`
- `dependencies-build`
- `testing`
- `evidence`
- `runtime-playtest`
- `performance`
- `documentation`
- `packaging-upgrade`
- `worker-git-pr`

Strip secrets, customer data, personal data, internal hostnames, and absolute paths outside the
workspace before recording evidence. Checkpoints are committed.

## Finalize mode

Finalize only after the cycle has exercised its intended acceptance surface. Read:

1. the current cycle's checkpoint JSONL or validated zero-event activation receipt;
2. all earlier `feedback/*.md` reports for recurrence and prior dispositions;
3. `scaffold-provenance.json`, package pins, and the skill manifest;
4. `.fsgg/`, `work/`, `readiness/`, or equivalent lifecycle artifacts;
5. git history and diff for churn, reverts, generated-file replacement, and workarounds;
6. build/test/TRX/evidence outputs, screenshots, playtest notes, timing artifacts, docs, and PR
   history that exist for this cycle.

Missing both checkpoint JSONL and a valid zero-event activation receipt is itself an evidence gap.
Record one candidate finding per material claim whose facts cannot be reconstructed; the critic
decides whether it is incomplete or unsupported. Never reconstruct precise elapsed time, commands,
versions, ownership, root causes, or abandoned approaches without evidence.

## Independent actionability critic

Finalization is an explicit `draft → cold critique → evidence verification → resolution → durable
report + audit` sequence. Write the draft outside `feedback/` (for example under a temporary
workspace directory); the immutable `feedback/<date>-<workspace>.md` does not exist until every
critic result has been dispositioned.

When subagents are available, spawn a fresh-context critic that did not author the checkpoint or
draft. Give it only the draft, the workspace root, and this rubric initially—not the author's
rationale, root-cause theory, or proposed fixes. When subagents are unavailable, clear the prior
drafting context as far as the host permits, perform the same two passes separately, and record
`separated-critic-pass`; never describe that mode as independent.

The critic performs two passes:

1. **Cold read.** For every `§4.n`, decide whether the report alone identifies affected behavior,
   expected/observed delta, impact, owner/change surface, reproduction or inspection route,
   version/commit/cycle boundary, recurrence/dedupe result, and known unknowns. Judge the observation
   before any proposed remedy.
2. **Evidence verification.** Resolve each cited file inside the workspace, run safe cited
   reproduction commands, compare versions and artifacts, and search prior reports plus open/closed
   issues for recurrence. A missing file, stale digest/version, non-reproducing command,
   contradictory artifact, or citation that only repeats the claim is not verified evidence.

The critic must not invent facts or upgrade a claim by confidence. It returns each stable finding id
as exactly one of `actionable`, `incomplete`, `unsupported`, `duplicate`, or `positive-pattern`.
While the collector is live, request missing facts at most twice per finding, then preserve the
honest reduced disposition. A required finding left `incomplete` or `unsupported` may remain as an
observation, but the skill and driver must not claim an actionable handoff.

After resolving the audit, write `feedback/audits/<report-stem>.audit.json`:

```json
{
  "auditSchema": 1,
  "report": "feedback/2026-07-26-example.md",
  "reportSha256": "<lowercase SHA-256 of LF-normalized report text>",
  "criticMode": "fresh-context-subagent",
  "criticPromptVersion": "actionability-v1",
  "findings": [{
    "id": "§4.1",
    "status": "actionable",
    "missingFacts": [],
    "checkedEvidence": [{
      "locator": "file:readiness/build.log",
      "result": "verified",
      "sha256": "<lowercase SHA-256 of LF-normalized file text>"
    }],
    "confidenceLimits": []
  }]
}
```

Use workspace-relative `file:` locators and strip secrets, customer data, personal data, internal
hostnames, and excluded absolute paths from critic prompts and audits. A `file:` locator is valid only
when it is a regular tracked file in the Git commit named by the report's `commit:` frontmatter. A file
created by the current run—even when it exists locally—does not become reviewable evidence. The validator
fails closed for an untracked, ignored, absent, or unknown-Git locator and tells you to commit it, cite a
stable committed receipt, or use a `command:` locator.

For generated render or performance evidence, prefer a command locator when the artifact is intentionally
ephemeral. Run and inspect the command before the critic records it; then record the exact command without
machine-local paths or secrets. For example, this clean-checkout-safe pair keeps the committed generator
and cites its reproducible output without committing the generated JSON:

```markdown
- **Evidence:** command:dotnet fsi readiness/generate-render-performance.fsx && inspect readiness/generated-performance.json
```

```json
{
  "locator": "command:dotnet fsi readiness/generate-render-performance.fsx && inspect readiness/generated-performance.json",
  "result": "verified"
}
```

For non-file evidence, use a specific locator such as `command:dotnet test ...` or
`issue:<owner>/<repo>#<number>`, record the checked result, and omit `sha256`. Evidence result vocabulary
is `verified`, `missing`, `stale`, `non-reproducing`, `contradictory`, or `claim-only`.

Compute the report and text-evidence digests with the bundled helper so newline normalization is
identical to validation:

```sh
dotnet fsi .agents/skills/fs-gg-feedback-report/scripts/feedback-tool.fsx -- digest <text-file>
```

## Audit-binding exceptions

Feedback reports and `feedback/audits/*.audit.json` are immutable. When cited production evidence is
deliberately superseded, the only mutable disposition surface is
the optional workspace-root file scripts/audit-binding-exceptions.json. Omitting that file preserves the ordinary fail-closed
invalidation behavior. When present, it is a strict schema-version-1 document:

```json
{
  "schemaVersion": 1,
  "exceptions": [{
    "id": "replacement-001",
    "audit": "feedback/audits/example.audit.json",
    "findingId": "§4.1",
    "locator": "file:readiness/old.json",
    "path": "readiness/old.json",
    "priorSha256": "<digest recorded by the immutable audit>",
    "replacementPath": "readiness/current.json",
    "replacementSha256": "<current lowercase SHA-256 of LF-normalized replacement text>",
    "evidenceLocator": "command:dotnet test ... && inspect readiness/current.json"
  }]
}
```

Every property is required and unknown properties fail. Each entry binds one exact audit, finding,
file locator, workspace path, and prior digest. Paths are exact workspace-relative file paths; glob,
directory, absolute, and traversal spellings are rejected. The replacement must exist in the
candidate tree as UTF-8 text with the declared LF-normalized digest. Its evidence locator must be a non-private `command:`
locator that explicitly names the replacement path; the checker validates that locator but never
executes ledger-controlled command text.

The replacement path must be one regular file in the selected subject: a regular working-tree file
for `--changed`, or a mode-100644/mode-100755 Git blob in the candidate head for `--base/--head`.
Symbolic links (including dangling links), directories/trees, submodules/gitlinks, missing paths, and
unreadable files are rejected before digest comparison.

Entries are durable while their immutable audit binding exists. Duplicate ids or bindings, entries
that match no immutable audit binding, stale replacement digests, mismatched fields, malformed JSON,
and unsupported schemas all fail closed. A valid entry dispositions only its exact invalidation, and
`check-invalidation` prints the applied entry id so the exception remains observable.

## Output contract

Write `feedback/<YYYY-MM-DD>-<workspace>.md`. Never overwrite or rename an earlier report; add
`-2`, `-3`, and so on for multiple runs on one day.

Use this frontmatter:

```yaml
---
feedbackSchema: 2
date: <YYYY-MM-DD>
workspace: <directory name>
cycle: <stable cycle id>
lane: <sdd | none | legacy-spec-kit | other>
toolVersion: <fsgg-sdd version or n/a>
commit: <described commit>
---
```

Keep sections §1 through §12 in order. Write `None observed.` when a section has no content.

## Report structure

### §1 Provenance and confidence

Record scaffold parameters, package pins, lifecycle/tool versions, commit, cycle boundaries,
checkpoint path and count, missing evidence, and confidence limits. Prefer versions embedded in
artifacts over rerunning a tool.

### §2 What worked

Name concrete components and the outcome they enabled. Preserve reusable positive patterns, not
generic praise.

### §3 What did not

Describe failed approaches, rework, local product corrections, and scope changes even when fixed
before ship. A fixed product problem can still reveal whether the scaffold prevented, detected, or
cheaply repaired it.

### §4 Findings

Use a structured record for every finding:

```markdown
#### §4.1 <one-line finding>

- **Kind:** positive-pattern | defect | friction | capability-gap | quality-gap | documentation | orchestration
- **Impact:** <who/what was affected and severity>
- **Expected:** <documented, designed, or reasonably required behavior>
- **Observed:** <what happened>
- **Evidence:** <exact locator; separate multiple locators with `;`>
- **Version:** <reproduced package/tool version and current version checked, or n/a>
- **Owner:** <FS-GG repo plus component/change surface>
- **Recurrence:** new | first seen <report/ref> | seen again <report/ref>; <existing issue/ref>
- **Avoidable cost:** <retries, manual edits, lifecycle reruns, elapsed time, or none>
- **Disposition:** issue | existing issue | ADR | doc fix | skill fix | product fix | accepted
```

Evidence need not be a command, but it must let another person inspect or reproduce the observation.
Each semicolon-separated value is an exact locator copied into the audit's `checkedEvidence`; the
validator rejects an unchecked report locator or a substituted audit locator.
For versioned defects, check the latest available release and say when re-verification was not
possible. Search prior reports and open/closed issues before filing. Add new evidence to an existing
issue instead of duplicating it.

Separate the observation from the proposed remedy. Route ownership to the repository that can fix
the root cause, not automatically to the product where it was observed.

### §5 Did not exercise

List intended or relevant surfaces not exercised. This is distinct from the complete §12 matrix.

### §6 Doc-versus-behavior contradictions

Quote both sides and identify the owning documentation or skill.

### §7 Workarounds still in the tree

Name files, removal conditions, and the risk of allowing each workaround to become permanent.

### §8 Friction and avoidable cost

Aggregate retries, manual YAML/code edits, lifecycle reruns, generated files replaced, worker
restarts, and elapsed developer time. Keep command duration separate from wall-clock time.

### §9 Skill value and gaps

Use the scaffolded skill manifest as the inventory. Record skills invoked with evidence, relevant
skills not invoked and why, wanted skills that were absent, misleading skill guidance, and overlap
between skills. Do not list unrelated globally available tools or connectors.

### §10 Outcome markers

Record comparable outcomes: time to first build, first meaningful test, first render/playable
state, first green verification, ship readiness, and merge. Mark estimates as estimates. Include
test counts and command duration only as supporting measures, not substitutes for elapsed time.

### §11 Falsifiable improvements

For each proposal, cite the observed finding or friction it would have prevented, name the owner and
change surface, and state a measurable acceptance condition. Do not propose unrelated preferences.

### §12 Development-surface coverage

Include every row exactly once:

```markdown
| Surface | Status | Evidence and result |
|---|---|---|
| scaffolding | exercised | ... |
| onboarding-guidance | partial | ... |
| skills | exercised | ... |
| sdd-authoring | exercised | ... |
| implementation-apis | exercised | ... |
| dependencies-build | exercised | ... |
| testing | exercised | ... |
| evidence | exercised | ... |
| runtime-playtest | not-exercised | ... |
| performance | partial | ... |
| documentation | exercised | ... |
| packaging-upgrade | not-exercised | ... |
| worker-git-pr | exercised | ... |
```

Allowed statuses are `exercised`, `partial`, and `not-exercised`. Evaluate the development process,
not merely whether a file or command existed.

## Validate before handoff

Run:

```sh
dotnet fsi .agents/skills/fs-gg-feedback-report/scripts/feedback-tool.fsx -- \
  validate feedback/<report>.md --audit feedback/audits/<report>.audit.json
```

Fix every reported error. The validator checks schema-v2 structure plus exact audit/report binding,
complete stable finding coverage, critic vocabulary/mode, unresolved actionability, file existence
and evidence digests. Changing or deleting cited evidence invalidates a previously green audit.
It intentionally does not validate old schema-v1 reports.

## Commit-time audit invalidation check

Before a commit lands, use the commit-aware base/head form:

```sh
dotnet fsi .agents/skills/fs-gg-feedback-report/scripts/feedback-tool.fsx -- \
  check-invalidation --base origin/main --head HEAD
```

It derives paths with `git diff --name-status --find-renames --find-copies`: rename and copy records
contribute both old and new sides, while deletions contribute their removed source path. It indexes only
digest-bearing `file:` citations in `feedback/audits/*.audit.json` and fails with the audit path, merged
report, finding ID, and locator for each touched citation. It does not run the full historical validator
or read cited files.

**The audit index is the tree of the ref you pass as `--base`, and never the working tree.** That is what
makes "merged" mean merged. An audit your own candidate introduces is not in the base tree, so it cannot
refuse the candidate that adds it — a repair round that changes evidence cited only by its own unmerged
audit stays green. An audit that *is* in the base tree keeps guarding the evidence it cites even if the
candidate deletes, renames or rewrites the audit file, so the refusal cannot be cleared by editing the
durable record. The index and the changed-path set are both taken from the same `--base` ref, so the two
halves of the verdict describe one state. Pass `origin/main`, not `git merge-base` output: an audit merged
after your branch point is still merged and must still guard.

Every verdict names the tree it indexed — `audit index: base ref origin/main` on the pass line and in the
failure header — so a green result states what it examined. Malformed, unreadable, or unresolvable index
input fails closed with its own diagnostic (`could not read the audit index at <ref>`, `unreadable audit
<path>`, `malformed audit <path>`), because a broken index must not be able to look like an empty one.

`--changed` is an advanced input only, and its audit index is the **working tree** — it carries no ref to
index from, and it says so in its own verdict (`audit index: the working tree`). Callers must supply the
complete name-status path set, including old rename/copy sides and deletions; do not feed it ordinary
`git diff --name-only`. For a commit-time check against a candidate, use `--base/--head`.

## Final roll-up

When a roadmap contains multiple cycle reports, aggregate rather than concatenate:

- count recurrence by root cause and owner;
- distinguish new findings from known findings with new evidence;
- rank improvements by recurrence, avoidable cost, and affected surfaces;
- preserve positive patterns worth promoting into templates or skills;
- state coverage gaps that no cycle exercised.

The cycle report remains immutable. Put cross-cycle synthesis in the roadmap completion report or a
separate timestamped report.
