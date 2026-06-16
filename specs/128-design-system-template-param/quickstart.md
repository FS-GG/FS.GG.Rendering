# Quickstart: Design-System Template Parameter (F3)

Validation runbook proving the `--designSystem wcag|ant` parameter scaffolds, builds, and governs both
variants correctly. References `contracts/` and `data-model.md` for details; no implementation code here.

## Prerequisites

- Repo builds: `dotnet build FS.GG.Rendering.slnx`.
- F1 (`126-ant-token-taxonomy`) and F2 (`127-color-policy`) landed — `docs/reports/color-policy-wcag.md`
  and `docs/reports/color-policy-ant.md` exist and pass their drift gate.
- The template is installable: `dotnet new install .` (or the repo's template pack/install step).

## Scenario 1 — Default path is a true no-op (US1 / SC-001)

```bash
dotnet new fs-gg-ui --name Demo          -o /tmp/demo-default
dotnet new fs-gg-ui --name Demo --designSystem wcag -o /tmp/demo-wcag
diff -r /tmp/demo-default /tmp/demo-wcag   # expect: no differences
```

Expected: identical trees, and each identical to today's template output (no `design-system.json`, no new
files). **Pass** ⇒ TP-2/GV-3.

## Scenario 2 — `ant` records its policy and carries the imprint (US1 / SC-002)

```bash
dotnet new fs-gg-ui --name Demo --designSystem ant -o /tmp/demo-ant
cat /tmp/demo-ant/design-system.json                       # { "policy": "ant", "authority": "AntExpectation" }
test -f /tmp/demo-ant/docs/reports/color-policy-ant.md      # the Ant imprint, as data
```

Expected: the record exists with `policy: "ant"`; the project is self-describing. **Pass** ⇒ TP-3/GV-4.

## Scenario 3 — Unknown value is rejected, never substituted (US1 / SC-005)

```bash
dotnet new fs-gg-ui --name Demo --designSystem material -o /tmp/demo-bad   # expect: non-zero exit
```

Expected: `dotnet new` rejects `material`, lists accepted values (`wcag`, `ant`), generates nothing.
**Pass** ⇒ TP-4.

## Scenario 4 — Both variants build and govern per policy (US2 / US3)

```bash
FS_GG_RUN_DESIGN_SYSTEM_VALIDATION=1 dotnet fsi scripts/validate-design-system-template.fsx
```

Expected: for each accepted value the script runs `dotnet new` + real `dotnet build` (both pass), resolves
the recorded policy via `ColorPolicy.byName`, and compares verdicts to the committed
`docs/reports/color-policy-<value>.md`:
- `wcag` product ⇒ `overall=FAIL` (the WCAG-failing pairing), authority WCAG-certified — same verdicts as
  today.
- `ant` product ⇒ `overall=PASS`, the divergent pairing `primary-hover-fg-on-surface` is `Aa` (not
  `Fail`), with the no-overclaim note. **Pass** ⇒ GV-2/GV-4/GV-5/GV-6.

The script writes `specs/128-design-system-template-param/readiness/design-system-template-validation.md`.

## Scenario 5 — Always-on gate + neutrality (US3 / SC-006 / SC-007)

```bash
dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Feature128
dotnet fsi scripts/refresh-surface-baselines.fsx --check    # expect: zero delta
```

Expected: the gate asserts the committed validation report (coverage == every choice, both build-pass,
`wcag` ≡ today, `ant` passes its pairings, ≥1 divergence); surface baselines show **no** delta and the
existing suite's pass/skip counts are unchanged. **Pass** ⇒ GV-1/GV-7, SC-006/SC-007.

## Done when

- Scenarios 1–5 pass.
- `covered-values` in the report equals the `designSystem` choice set (no accepted value unvalidated).
- No public-surface-baseline delta; existing render/gallery output and pass/skip counts unchanged.
