---
name: fs-gg-feedback-capture
description: Capture per-phase fs-gg-ui / Spec Kit feedback (process friction, generalizable-code candidates, severity) into specs/<feature>/feedback/ after each phase.
compatibility: Authoring command skill, shipped only when `dotnet new fs-gg-ui --feedback true`; no product runtime.
metadata:
  author: fs-gg-ui
---

# fs-gg-feedback-capture

An authoring **command** skill invoked by the generated project's `after_*` Spec Kit
hooks (only present under `--feedback true`). On phase completion it surfaces four exact
prompts and writes one dated, phase-identified feedback record.

## When to use

- Automatically, via the `after_specify` / `after_clarify` / `after_plan` / `after_tasks`
  / `after_analyze` / `after_implement` hooks wired into `.specify/extensions/feedback/`.
- Fires only on phase **completion** — an aborted or failed phase runs no `after_*` hook
  and writes nothing.

## Driven-library API

This is a process/authoring command skill — **no backing library**. It produces a
Markdown record; it calls no shipped `.fsi` surface.

## The four prompts (exact wording, `{phase}` substituted)

1. "During the *{phase}* phase, did anything go wrong or cause friction in the
   fs-gg-ui / Spec Kit process — and what would have helped you?"
2. "Did you write any F# code on a skill topic this phase that could be generalized into
   the support library? If yes, name the skill family/topic and the candidate helper (and
   link any external docs/research used)."
3. "What additional or new skills would have been helpful during the *{phase}* phase? Name
   the topic and what the missing skill should have covered, or 'none'."
4. "How blocking was the friction — none / minor / major / blocker?"

## Runnable example — the record written to `specs/<feature>/feedback/<phase>-<date>.md`

```markdown
---
phase: plan
date: 2026-07-10
severity: minor            # none | minor | major | blocker
toolVersion: 0.9.0         # `fsgg-sdd --version`, or `toolVersion` from any stage report
---

## Process friction
<answer to prompt 1 — what went wrong + what would have helped>

## Generalizable code
<answer to prompt 2 — skill family/topic + candidate helper, or "none">

## Skill gaps
<answer to prompt 3 — additional/new skills that would have helped this phase
 (topic + what the missing skill should cover), or "none">

## Research links
<official-docs-first then community links, when created after a hard problem;
 offline: "research blocked — <why>">
```

One record per phase. Severity is mandatory. A record naming generalizable code MUST
capture the skill family/topic + candidate helper so it can be triaged into
`FS.GG.UI.SkillSupport`.

These rules are normative **here**: this skill body is the contract, and no separate
requirements document stands behind it. Cite it as `fs-gg-feedback-capture`.

## Version every record, and check before you file

`toolVersion` is mandatory too. Record what you actually ran — `fsgg-sdd --version`, or the
`toolVersion` field any stage report already carries (prefer lifting it out of the artifact;
it cannot disagree with itself about what produced it) — and check each candidate defect
against the **latest tag** before filing, not against whatever CLI happens to be installed.

Without it a reader cannot tell a live defect from one that was fixed and tagged before the
phase even began, so every finding costs a re-verification pass against `main` before it can
be trusted — and a record that cannot be trusted at a glance stops working as signal. State
which version each finding was reproduced on and a stale one is legible as stale, instead of
being rediscovered at full price by the next reader.

If you cannot upgrade to re-check, still file it and say so: *"reproduced on 0.8.1; not
re-checked against 0.9.0"* is honest and triageable. Silence is not. Where the toolchain is
pinned (`.config/dotnet-tools.json`), name the pin — it explains a version gap that would
otherwise read as carelessness.

The synthesis counterpart [[fs-gg-feedback-report]] carries the same rule in its §1
Provenance and its per-finding record — it ships on every lane, including the ones that never
materialize this skill.

## Persistent problems

When a problem outlasts reasonable in-repo attempts, extensive external research is
**mandatory** — consult **official online docs first** (the F#/.NET docs and the driven
library's own documentation/API reference), then community sources (forums, Reddit, Q&A
sites, issue trackers and changelogs). Record the findings and resolving links in the
feature's `specs/<feature>/feedback/` folder and, for durable lessons, in this skill's
**Sources** line. Offline, the mandate degrades to recording "research blocked — <why>"
rather than hard-failing the phase.

## Related

[[fs-gg-project]], [[fs-gg-feedback-report]] — the synthesis
counterpart that reads these records at cycle end.

## Sources / links

- Spec Kit hooks & extensions model: <https://github.com/github/spec-kit>
- F# docs: <https://learn.microsoft.com/en-us/dotnet/fsharp/>
