# Leak evidence — before / after (`template.json` static scan + live scaffold)

## T004 — Static before-scan (pre-fix)

quickstart §0 scan over `.template.config/template.json`:

```
ungated .claude/skills product sources: 9
```

The 9 ungated `.claude/skills/fs-gg-*/` product sources (each `profile`-gated, **missing** `lifecycle == "spec-kit"`):

- `.claude/skills/fs-gg-scene/`
- `.claude/skills/fs-gg-symbology/`
- `.claude/skills/fs-gg-skiaviewer/`
- `.claude/skills/fs-gg-elmish/`
- `.claude/skills/fs-gg-keyboard-input/`
- `.claude/skills/fs-gg-ui-widgets/`
- `.claude/skills/fs-gg-styling/`
- `.claude/skills/fs-gg-layout/`
- `.claude/skills/fs-gg-testing/`

Pre-fix expectation (quickstart): **9**. Confirmed.

## T005 — Live smoke run (leak reproduced under `sdd`)

Direct scaffold of the reported failing combination (the env-gated `validate-lifecycle-template.fsx`
live loop does **not** cover the `game` profile and is structurally blind to `.claude/skills/`
product-mirror files — `workspaceAbsent` only checks `.specify/`, agent-context, and
`fs-gg-project`; `frameworkSkillCount` only counts `.agents/skills/` — which is exactly why the leak
went uncaught). So the reproduction is a direct scaffold:

```sh
dotnet new fs-gg-ui --name LeakRepro --profile game --lifecycle sdd -o <scratch>
```

Observed **PRESENT** under `.claude/skills/` (the intrusion SDD flags as `providerWroteSddTree`):

```
count(.claude/skills/fs-gg-*) = 8
  fs-gg-elmish  fs-gg-keyboard-input  fs-gg-layout  fs-gg-scene
  fs-gg-skiaviewer  fs-gg-styling  fs-gg-symbology  fs-gg-ui-widgets
```

`.agents/skills/fs-gg-*` = the same 8 (provider surface, correct). Leak reproduced: under `sdd`,
`game` writes 8 UI product skills into the orchestrator-owned `.claude/skills/` tree. (8 not 9 —
`fs-gg-testing` is `governed`-only; the `game` profile triggers 8 of the 9 fixed sources.)

## T008 — Static after-scan (post-fix)

quickstart §0 scan over the fixed `.template.config/template.json`:

```
ungated .claude/skills product sources: 0
.claude/skills product sources: 9 | spec-kit-gated: 9
.agents/skills product sources: 9 | spec-kit-gated: 0   (siblings untouched)
total sources: 33 (unchanged)  ·  JSON valid
```

Before → after: **9 → 0** ungated `.claude/skills/` product sources. Only the 9 `.claude/skills/`
conditions gained `&& lifecycle == "spec-kit"`; the 9 `.agents/skills/` provider sources are byte-unchanged.
