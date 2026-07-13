# Product skills — authoring notes

These are the skill bodies a **scaffolded product** receives. Which product receives which skill is
decided by `materializes-when` in [`../skill-manifest/skill-manifest.json`](../skill-manifest/skill-manifest.json),
which is **generated** — edit `scripts/generate-skill-manifest.fsx`, never the JSON.

Four of these bodies (`fs-gg-game-core`, `fs-gg-audio`, `fs-gg-persistence`, `fs-gg-model-swap`) are
**frozen mirrors** of skills FS.GG.Game owns (ADR-0022 §6). Editing one here does not change the
canonical, and `scripts/check-frozen-mirrors.fsx` will tell you so. Route the content upstream.

## Declaring an instrument (`instruments:`)

A skill may **mandate** something in its Evidence Rules and point at *another* skill for the
instrument that satisfies it. `fs-gg-ui-widgets` does this: it demands responsiveness evidence and
points at `[[fs-gg-elmish]]`, where `captureRespondsProof` and the `OnFrameMetrics` projection are
taught.

That delegation carries a silent hazard. A product only receives the skills its **profile**
materializes. If the instrument skill does not ship wherever the mandating skill ships, a product is
handed **a rule it has no instrument for** — and nothing about that is a type error, so every test
stays green. It nearly happened: `fs-gg-ui-widgets` ships to `[app, game]`, `fs-gg-elmish` to
`[app, sample-pack, game]`. Narrow `fs-gg-elmish` to `[app]` and the `game` product is starved.

So **declare it**, in the mandating skill's frontmatter:

```yaml
---
name: fs-gg-ui-widgets
description: ...
instruments:
  - rule: responsiveness evidence (respondsProofOf / captureRespondsProof, OnFrameMetrics)
    skill: fs-gg-elmish
---
```

`R-INST` (in `tests/Package.Tests/SkillPackageReachTests.fs`) then holds the two `materializes-when`
sets to each other and reddens if the instrument skill ever stops reaching a profile the mandating
skill reaches. It also checks that the body still links `[[the target]]`, so the declaration and the
prose cannot drift apart.

### When NOT to declare one

<!-- skill-refs: prose-ok [[link]] — the SHAPE of a ref, not a ref. This doc teaches the convention, so it must be able to write the syntax without invoking it. -->
**A `[[link]]` is not an instrument declaration.** Most links are cross-references, and one of them
crosses a profile gap *on purpose*: `fs-gg-testing` ships to `headless-scene` and `governed`, where
`fs-gg-elmish` does not, and links it anyway — because a headless product has no controls to click,
so the pointer-and-keyboard mandate is vacuous for it. That is correct guidance, and a gate that
judged every link would redden it and force an exemption for correct work.

Declare an instrument when, and only when, **this skill states a rule that a reader cannot satisfy
without the other skill**. If a reader on some profile simply does not have the rule, there is nothing
to starve, and no declaration.

### The cost, stated honestly

`instruments:` is **opt-in**, so a delegation nobody declares is a delegation nobody checks. That is
deliberate — the alternative is judging every link, whose oracle is wrong (above) — but it means this
file is the only thing standing between the next author and the bug. If you are writing "the
instrument for this rule is in `[[…]]`", you are the author it was written for.
