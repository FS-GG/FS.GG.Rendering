# Retained Inspection Summary

Status: `accepted`

## Result

- ✅ Retained inspection APIs are additive and package-visible.
- ✅ Retained transitions classify reused, repainted, shifted, added, removed, and first-frame nodes.
- ✅ Damage locality validation computes clipped true-union dirty area, dirty percentage, broad/full-surface status, shifted/repainted counts, and scoped exceptions.
- ✅ AntShowcase uses structured retained evidence for the selected full-shell assertion while preserving screenshot readiness count parity.
- ✅ Canonical lane `retained-inspection` passed in `00:00:37.5334492`, below the five-minute target.

## Reviewer Walkthrough

A reviewer can identify dirty area percentage, repainted node count, shifted node count, and affected visual regions from the retained summary fields and validation findings. The focused summary is small enough for a reviewer to inspect in under two minutes:

- Dirty area percentage: `DamageRegionInspection.DirtyPercentage`
- Repainted count: `DamageRegionInspection.RepaintedNodeCount`
- Shifted count: `DamageRegionInspection.ShiftedNodeCount`
- Affected regions: `DamageRegionInspection.AffectedRegionIds`
- Findings: `DamageLocalityFinding.RuleId`, `AffectedNodeIds`, `AffectedRegionIds`, `Expected`, and `Actual`

## Accepted Lane

- Run id: `validation-20260619-155116-212d54`
- Overall readiness: `ready`
- Lane status: `passed`
- Log: `specs/170-retained-damage-inspection/readiness/lanes/validation-20260619-155116-212d54/retained-inspection/log.txt`
- Result JSON: `specs/170-retained-damage-inspection/readiness/lanes/validation-20260619-155116-212d54/retained-inspection/result.json`
- Diagnostics: `specs/170-retained-damage-inspection/readiness/lanes/validation-20260619-155116-212d54/retained-inspection/diagnostics.md`

## Caveats

- `retained-inspection` is an optional focused lane and an explicit substitute for `aggregate-solution`.
- No blocking retained damage findings remain.
