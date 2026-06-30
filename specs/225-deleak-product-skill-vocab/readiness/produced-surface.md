# Produced-surface verification (T004 — standing assumption, research R1)

Confirms, **before any prose edit**, that the leak guard's scan surface is exactly the 7 shipped
product skills, and that under a non-spec-kit lifecycle the `specs/<feature>/feedback/` location
genuinely dead-ends (so the Class-B leak is a real defect across lifecycles, not a spec-kit-only
nicety).

## 1. Discovery surface = the 7 expected product skills

The guard enumerates via `SkillParity.inventorySkills (defaultRequest root) (discoverDefaultSurfaces root)`
filtered to `entry.Path.Contains("template/product-skills")` — the same authoritative enumerator
parity + Feature 224 consume, never a hardcoded list. Front-matter `name:` enumeration of the shipped
bodies:

| id | path |
|---|---|
| `fs-gg-elmish` | `template/product-skills/fs-gg-elmish/SKILL.md` |
| `fs-gg-keyboard-input` | `template/product-skills/fs-gg-keyboard-input/SKILL.md` |
| `fs-gg-scene` | `template/product-skills/fs-gg-scene/SKILL.md` |
| `fs-gg-skiaviewer` | `template/product-skills/fs-gg-skiaviewer/SKILL.md` |
| `fs-gg-symbology` | `template/product-skills/fs-gg-symbology/SKILL.md` |
| `fs-gg-testing` | `template/product-skills/fs-gg-testing/SKILL.md` |
| `fs-gg-ui-widgets` | `template/product-skills/fs-gg-ui-widgets/SKILL.md` |

**Count: exactly 7.** The guard test
`Feature225ProductSkillVocabulary."discovery surface did not narrow …"` asserts this discovery
surface covers all 7 ids, so a regression that drops skills from the scan is caught (not masked by a
fixed-list scan — spec edge case "a skill not in scope today gains the leaky boilerplate later").

## 2. Real-scaffold proof (env-gated live loop)

`FS_GG_RUN_LIFECYCLE_VALIDATION=1 dotnet fsi scripts/validate-lifecycle-template.fsx --emit-report`
scaffolds real `dotnet new fs-gg-ui` products per `lifecycle × profile` and reports
(`readiness/lifecycle-live.log`, captured this run):

```
covered-values: spec-kit, sdd, none
composition-matrix: 12/12 generate; ant-overlay-present=ok; feedback-gated-under-non-speckit=ok
symbology: vendored
sdd/{app,headless-scene,governed,sample-pack}: gated-absent=ok product-present=ok diff-vs-default=gated-only
none/{app,headless-scene,governed,sample-pack}: gated-absent=ok product-present=ok
sdd/none framework-skills-present=ok (per-profile subsets: 6/2/3/4 SKILL.md)
provenance: live
result: pass
```

**Findings:**
- The 7 framework product skills are **vendored** across the non-spec-kit lifecycles
  (`framework-skills-present=ok` under both `sdd` and `none`, profile-subset counts).
- `feedback-gated-under-non-speckit=ok` and `gated-absent=ok`: under `sdd`/`none` the spec-kit
  lifecycle workspace — which is where `specs/<feature>/feedback/` lives — is **absent**. So an
  unconditional `specs/<feature>/feedback/` instruction in a shipped skill resolves to **nothing** in
  those products. This confirms US2's premise (the Class-B leak is a real cross-lifecycle defect).
- `symbology: vendored` confirms the Class-C-bearing skill reaches products.

## Conclusion

The produced surface matches the assumption: **the de-leak edits and the guard are built on a
verified 7-skill surface**, and the Class-B feedback path genuinely dead-ends under non-spec-kit
lifecycles. Cleared to author the prose edits.
