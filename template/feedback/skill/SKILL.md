---
name: fs-skia-feedback-capture
description: Capture per-phase fs-skia-ui / Spec Kit feedback (process friction, generalizable-code candidates, severity) into specs/<feature>/feedback/ after each phase.
compatibility: Authoring command skill, shipped only when `dotnet new fs-skia-ui --feedback true`; no product runtime.
metadata:
  author: fs-skia-ui
  source: specs/058-skills-quality-feedback/contracts/feedback-capture.md
---

# fs-skia-feedback-capture

An authoring **command** skill invoked by the generated project's `after_*` Spec Kit
hooks (only present under `--feedback true`). On phase completion it surfaces four exact
prompts and writes one dated, phase-identified feedback record.

## When to use

- Automatically, via the `after_specify` / `after_clarify` / `after_plan` / `after_tasks`
  / `after_analyze` / `after_implement` hooks wired into `.specify/extensions/feedback/`.
- Fires only on phase **completion** — an aborted or failed phase runs no `after_*` hook
  and writes nothing (FR-016).

## Driven-library API

This is a process/authoring command skill — **no backing library**. It produces a
Markdown record; it calls no shipped `.fsi` surface.

## The four prompts (exact wording, `{phase}` substituted)

1. "During the *{phase}* phase, did anything go wrong or cause friction in the
   fs-skia-ui / Spec Kit process — and what would have helped you?"
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
date: 2026-06-03
severity: minor            # none | minor | major | blocker
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

One record per phase (FR-014). Severity is mandatory (FR-015). A record naming
generalizable code MUST capture the skill family/topic + candidate helper so it can be
triaged into `FS.Skia.UI.SkillSupport` (FR-015 → US2).

## Persistent problems

When a problem outlasts reasonable in-repo attempts, extensive external research is
**mandatory** — consult **official online docs first** (the F#/.NET docs and the driven
library's own documentation/API reference), then community sources (forums, Reddit, Q&A
sites, issue trackers and changelogs). Record the findings and resolving links in the
feature's `specs/<feature>/feedback/` folder and, for durable lessons, in this skill's
**Sources** line. Offline, the mandate degrades to recording "research blocked — <why>"
rather than hard-failing the phase.

## Related

[[fs-skia-project]], [[fsharp-build-orchestration]]

## Sources / links

- Spec Kit hooks & extensions model: <https://github.com/github/spec-kit>
- F# docs: <https://learn.microsoft.com/en-us/dotnet/fsharp/>
