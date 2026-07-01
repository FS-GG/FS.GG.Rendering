# T012 — Provider tree intact + spec-kit unchanged (SC-003)

## `.agents/skills/` == S(profile) under all three lifecycles (provider surface never shrinks)

`game` — the 8 UI product skills { scene, symbology, skiaviewer, elmish, keyboard-input, ui-widgets,
styling, layout } present under `.agents/skills/` in **every** lifecycle:

| lifecycle | `.agents/skills/` UI product skills |
|---|---|
| sdd | all 8 present |
| none | all 8 present (identical to sdd) |
| spec-kit | all 8 present (+ the spec-kit-only authoring/product-guidance skills — `fs-gg-product-*`, `fs-gg-testing`, etc. — which are the lifecycle-workspace set, correctly present only under spec-kit) |

`app` mirrors `game` (same S). `headless-scene`=2, `governed`=3, `sample-pack`=4 (the env-gated report's
`framework-skills-present=ok` lines). No assertion over `.agents/skills/` membership was loosened — the
provider surface is fully checked and unchanged by the fix.

## spec-kit emits S(profile) into BOTH surfaces (diff-vs-today = none)

`game/spec-kit`: the 8 UI product skills present under **both** `.agents/skills/` (8/8) and
`.claude/skills/` (8/8) — the standalone Spec Kit lane is unchanged. Feature 204 **GV-3**
(`spec-kit/<p>: generate=pass diff-vs-today=none`, every profile) proves byte-identity of the spec-kit
output vs the same template's no-flag default (C-5). The fix touched only the `lifecycle == "spec-kit"`
gate on the `.claude/skills/` product mirror, so under spec-kit those sources still emit exactly as before.

## Invariant summary (data-model emission matrix, confirmed live)

| Lifecycle | `.agents/skills/` (provider) | `.claude/skills/` (mirror) |
|---|---|---|
| spec-kit | S(profile) | S(profile) — unchanged |
| sdd | S(profile) | **∅** (was S — the leak) |
| none | S(profile) | **∅** (was S — the leak) |

`sdd` and `none` columns are identical (C-4).
