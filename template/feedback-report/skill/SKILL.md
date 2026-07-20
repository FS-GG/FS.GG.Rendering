---
name: fs-gg-feedback-report
description: Write one comprehensive retrospective report on the development experience — what worked, what did not, problems hit, improvements proposed — into a dated file under feedback/ at the end of a development cycle.
compatibility: Agent-invoked authoring skill, shipped in EVERY generated workspace — every profile and every lifecycle lane; no product runtime.
metadata:
  author: fs-gg-ui
---

# fs-gg-feedback-report

An authoring skill that synthesizes **one retrospective report per development cycle** and
writes it to `feedback/<date>-<workspace>.md`. It reads whatever evidence exists and produces a
single durable document.

This skill is agent-invoked, reads only optional and guarded evidence, and therefore ships
**unconditionally** — every profile, every lane (spec-kit, sdd, none). Among the evidence it may
read are per-phase capture records under `specs/<feature>/feedback/`, but nothing produces those
in a current scaffold: the per-phase capture machinery (the fs-gg-feedback-capture skill and its
Spec Kit after_* feedback hooks) was retired with the spec-kit lane under ADR-0056.
Grandfathered spec-kit trees may still carry such records, so this skill reads them **if present**;
a lane with no capture records is a supported case, not a broken one.

## When to use

- **Agent-invoked at cycle end** — when a feature, milestone, or scaffold evaluation is
  finished and you are about to hand the workspace back. It is not hook-invoked; nothing
  fires it automatically.
- Not mid-cycle. A report written before the work is exercised produces a "did not
  exercise" list longer than its findings, which is noise.

## Driven-library API

A process/authoring skill — **no backing library**. It reads evidence and writes Markdown;
it calls no shipped `.fsi` surface.

## Evidence sources — every one optional

Read what exists; never fail because a source is absent. A source that is missing is a
fact about the lane, not an error, and belongs in §1.

| Source | Carries | Absent when |
|---|---|---|
| `scaffold-provenance.json` | effective parameters, lifecycle lane, package pins | the scaffold predates provenance, or was hand-assembled |
| the `.fsgg/` work-item store | what was planned vs what shipped | the SDD lane owner did not supply one |
| git history | ordering, churn, revert-shaped commits | a fresh, uncommitted tree |
| per-phase capture records | phase-local friction, severity | a current scaffold (the per-phase capture skill was retired under ADR-0056); present only in grandfathered spec-kit trees |

Per-phase records live at `specs/<feature>/feedback/<phase>-<date>.md`. When they exist,
this report **synthesizes** them — it does not restate them; cite the phase and date.

## Output contract

- **Path:** `feedback/<YYYY-MM-DD>-<workspace>.md`, where `<workspace>` is the product
  directory name. Create `feedback/` at the repository root if it does not exist.
- **One file per run, append-only as a set.** Never overwrite a prior report and never
  rename one. Two runs on the same day get a `-2` suffix. Reports are only comparable
  across time if they accumulate.
- **Section numbers are stable and are never renumbered.** Findings get cited from
  immutable records as `feedback §3.2`. If a section does not apply, keep its heading and
  write "none observed" underneath. Adding a section means taking the next free number,
  never inserting.

## Report structure

`§1 Provenance` — the lane, the effective parameters, the package pins, the **toolchain
version**, and the commit the report describes. A finding is unattributable without it; if
`scaffold-provenance.json` is absent, say so explicitly and record what you could recover
instead.

Record the toolchain version as `fsgg-sdd --version`, or lift the `toolVersion` field that
any stage report already carries — prefer lifting it; the artifact has it. This is the field a
stale finding is adjudicated from, so never leave it unstated. A lane that runs no SDD
lifecycle may have no stage report and no `fsgg-sdd` at all: say so, and version the findings
against the package pins instead — exactly the degradation an absent `scaffold-provenance.json`
already gets. An absent toolchain is a fact about the lane; an unstated one is a hole in the
report. See **Check before you file**.

`§2 What worked` / `§3 What did not` — prose, not a list of adjectives. Name the component.

`§4 Findings` — the load-bearing section. Every finding is a **structured record**:

```markdown
#### §4.1 <one-line statement of the defect>

- **Expected:** <what the docs, the signature, or the skill said would happen>
- **Observed:** <what happened>
- **Evidence:** the command, and its output, verbatim
- **Version:** <the toolchain version this was reproduced on, and the latest tag you checked it against>
- **Component:** <the owning module / skill / script>
- **Disposition:** issue | ADR | doc fix | won't-fix — and why
```

A finding without expected-vs-observed is an opinion. A finding without a command and its
output is unreproducible and will be closed as such by whoever triages it. A finding without
a version cannot be told apart from one that was fixed and tagged before your run began, and
costs every reader a full re-verification pass against `main` to find that out.

`§5 Did not exercise` — an explicit list of what this cycle never ran. Silence is otherwise
read as endorsement, and a report that omits this section quietly claims full coverage.

`§6 Doc-versus-behavior contradictions` — where a doc, a skill body, or a parameter
description says one thing and the code does another. Quote both sides.

`§7 Workarounds still in the tree` — anything you did to get moving that should not
survive. Name the file. An unlisted workaround becomes load-bearing by accident.

`§8 Friction log` — the small costs: a confusing error, a missing default, a flag that had
to be discovered. Individually trivial, collectively the reason a lane feels bad.

`§9 Negative space on the skill set` — which vendored skills were **never invoked**, and
which were **wanted and absent**. Both directions are signal: the first is surface that is
not paying for itself, the second is the next skill to write.

`§10 Time-to-X markers` — time to first build, first render, first passing test, first
green validation. Rough is fine; the trend across reports is the point.

`§11 Falsifiable improvements` — each proposal must name **what it would have prevented in
this run**, citing a section above. A proposal that prevents nothing observed here is a
preference, and belongs in an issue rather than a report.

## Check before you file

A defect reproduced on a stale toolchain is not a finding. It is rediscovery, and it costs
every reader of this report a re-verification pass against `main` to establish that.

Before writing §4, establish two versions and record both:

- **What you ran.** `fsgg-sdd --version`, or the `toolVersion` field carried by any stage
  report. Prefer lifting it out of the artifact over re-running the CLI — the artifact
  cannot disagree with itself about what produced it. If the lane has neither, fall back to
  the package pins recorded in §1 and say that is what you are versioning against.
- **What is current.** The latest released tag of whatever you versioned against. Check each
  candidate defect against *that*, not against whatever happens to be installed in this
  workspace.

Then state, per finding, which version it was reproduced on. A defect that survives on the
latest tag is live; one that does not is already fixed, and filing it as though it were live
is how a report stops being usable as signal — a reader who cannot tell the two apart has to
re-verify all of them.

If you cannot upgrade to re-check, file the finding anyway and say so: *"reproduced on 0.8.1;
not re-checked against 0.9.0"* is honest and still triageable. Silence is not. Where the
toolchain is pinned (`.config/dotnet-tools.json`), name the pin — it explains a version gap
that would otherwise read as carelessness.

## Redaction

These reports are **committed**. Before writing, strip absolute paths outside the
workspace, tokens, credentials, licence keys, internal hostnames, and customer or personal
data. Quote command output only after checking it for the same. When a finding cannot be
stated without a secret, state the shape of it and reference where the secret lives.

## Runnable example

```markdown
---
date: 2026-07-10
workspace: AcmeGame
lane: sdd
toolVersion: 0.9.0
---

## §1 Provenance
Scaffolded from fs-gg-ui 0.4.2, `--profile game --lifecycle sdd`.
Pins: FS.GG.UI 0.2.0. Toolchain: fsgg-sdd 0.9.0 (`toolVersion` lifted from the stage report;
latest tag at time of writing, so findings below are checked against current).
Report describes commit a1b2c3d.

## §2 What worked
The fixed-step loop and the seeded RNG composed with no glue.

## §3 What did not
`ViewerEffect` has no audio case, so the audio skill's examples do not run.

## §4 Findings

#### §4.1 fs-gg-audio documents an effect the viewer cannot dispatch
- **Expected:** `ViewerEffect.Audio` per the skill body
- **Observed:** no such case; the union has four cases
- **Evidence:** `dotnet fsi scripts/print-effects.fsx` → `Render | Resize | Close | Quit`
- **Version:** reproduced on fsgg-sdd 0.9.0, the latest tag — live, not already fixed
- **Component:** src/SkiaViewer
- **Disposition:** issue — filed as FS.GG.Rendering#245
<!-- skill-refs: closed-ok FS.GG.Rendering#245 — a worked EXAMPLE of a finished report, citing the issue it filed; closed is what a filed-and-fixed finding looks like. Not somewhere to go. -->


## §5 Did not exercise
Persistence, collision narrow-phase, the sample pack.

...
```

## Related

[[fs-gg-project]]. This skill ships on every lane and reads only optional evidence. There is no
longer a per-phase capture counterpart — the fs-gg-feedback-capture skill and its Spec Kit after_*
feedback hooks were retired with the spec-kit lane under ADR-0056 — which is why every evidence source
above is read "if present": the per-phase records exist only in grandfathered spec-kit trees.

## Sources / links

- F# docs: <https://learn.microsoft.com/en-us/dotnet/fsharp/>
