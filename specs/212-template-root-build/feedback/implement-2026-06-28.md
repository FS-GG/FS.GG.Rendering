---
phase: implement
date: 2026-06-28
severity: minor
---

## Process friction

The one real friction was the headless-`dotnet run` assertion (research R4 / hypothesis (b)). The
dev box has a **live Wayland session**, so the first scaffold's `dotnet run` took the
interactive-window path and the app profile opened a *persistent* window that blocks until closed —
the first `Acme` run happened to exit 0 (a window-close fluke), but a second scaffold confirmed the
interactive path actually hangs until `timeout`. The deterministic exit-0 path the release gate
relies on is the genuinely-headless one, which had to be reproduced locally with
`env -u WAYLAND_DISPLAY -u DISPLAY -u XDG_RUNTIME_DIR dotnet run`. What would have helped: the plan
naming, up front, that "headless" verification on a desktop dev box requires explicitly stripping
the display env (not just trusting a single run's exit code) — the smoke step's exit-0 is misleading
otherwise. Captured the corrected nuance in `readiness/smoke.md` and `us3.md`.

Otherwise the feature went smoothly: the base `template/base/` source already copies all
non-excluded files with `sourceName` substitution, so emitting the four new root files required
**no** `template.json` edit (the desired "add nothing to exclude" outcome) — a clean, low-risk wiring.

## Generalizable code

Skill family/topic: **fsharp-build-orchestration** (the generated `build.fsx`). Candidate helpers:
the two name-agnostic locators added in `build.fsx` — `singleRootSolution ()` (find the unique
`*.slnx` in cwd) and `singleSrcProject ()` (find the unique `src/<project>`). These let pass-through
build-graph targets work for any scaffolded `--name` with no literal token. They are small and
script-local today, but the "locate the single root solution / single src project" pattern is a
reasonable candidate to triage into `FS.GG.UI.SkillSupport` if other generated-product build scripts
need the same name-agnostic discovery.

## Skill gaps

none — `cross-repo-coordination` (registry coherence) and `fs-gg-feedback-capture` covered the
cross-cutting phases; the existing template-authoring memory ([[fs-gg-ui-template-pascalcase-name]],
[[fs-gg-ui-template-authoring-gotchas]]) pre-empted the PascalCase-name and `sourceName` rewrite
gotchas.

## Research links

research not required — no hard external problem this phase; the headless-run nuance was resolved
in-repo by inspecting the entrypoint's `UnsupportedEnvironment` diagnostics and reproducing the
display-stripped environment.
</content>
