# Leak-guard regression evidence (FR-007 / SC-005)

The guard `tests/Package.Tests/Feature225ProductSkillVocabularyTests.fs` reds on today's leaks and
greens on the corrected set. Failing-before / passing-after recorded here per Constitution V.

Run: `dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Feature225ProductSkillVocabulary`

---

## Failing-before (T006) — guard authored, before any prose edit

`Failed: 1, Passed: 5, Total: 6` — the live-set test fails with **23 leak findings**, naming each
skill, leak class, matched token, and `file:line`. The other 5 tests (discovery-surface coverage,
synthetic inject = 3, conditional-pass both directions, finding-shape) pass.

The 23 findings (matches research R0 inventory exactly — 9 Class A, 7 Class B, 7 Class C):

| Skill | Class | Token | file:line |
|---|---|---|---|
| fs-gg-testing | A | refresh-local-feed-and-samples | fs-gg-testing/SKILL.md:56 |
| fs-gg-testing | A | package-feed | fs-gg-testing/SKILL.md:57 |
| fs-gg-testing | A | specs/*/readiness | fs-gg-testing/SKILL.md:59 |
| fs-gg-testing | A | .gitignore | fs-gg-testing/SKILL.md:60 |
| fs-gg-testing | A | BaseOutputPath | fs-gg-testing/SKILL.md:63 |
| fs-gg-ui-widgets | A | refresh-local-feed-and-samples | fs-gg-ui-widgets/SKILL.md:114 |
| fs-gg-ui-widgets | A | package-feed | fs-gg-ui-widgets/SKILL.md:114 |
| fs-gg-skiaviewer | A | refresh-local-feed-and-samples | fs-gg-skiaviewer/SKILL.md:56 |
| fs-gg-skiaviewer | A | package-feed | fs-gg-skiaviewer/SKILL.md:56 |
| fs-gg-testing | B | specs/<feature>/feedback | fs-gg-testing/SKILL.md:89 |
| fs-gg-ui-widgets | B | specs/<feature>/feedback | fs-gg-ui-widgets/SKILL.md:150 |
| fs-gg-skiaviewer | B | specs/<feature>/feedback | fs-gg-skiaviewer/SKILL.md:106 |
| fs-gg-elmish | B | specs/<feature>/feedback | fs-gg-elmish/SKILL.md:65 |
| fs-gg-keyboard-input | B | specs/<feature>/feedback | fs-gg-keyboard-input/SKILL.md:101 |
| fs-gg-scene | B | specs/<feature>/feedback | fs-gg-scene/SKILL.md:82 |
| fs-gg-symbology | B | specs/<feature>/feedback | fs-gg-symbology/SKILL.md:200 |
| fs-gg-testing | C | Feature 168 | fs-gg-testing/SKILL.md:53 |
| fs-gg-ui-widgets | C | Feature 168 | fs-gg-ui-widgets/SKILL.md:111 |
| fs-gg-skiaviewer | C | Feature 168 | fs-gg-skiaviewer/SKILL.md:53 |
| fs-gg-symbology | C | spec-196 | fs-gg-symbology/SKILL.md:63 |
| fs-gg-symbology | C | feature 199 | fs-gg-symbology/SKILL.md:96 |
| fs-gg-symbology | C | feature 200 | fs-gg-symbology/SKILL.md:122 |
| fs-gg-symbology | C | feature 200 | fs-gg-symbology/SKILL.md:137 |

## Passing-after (T021) — all three stories landed

`Passed: 6, Failed: 0, Total: 6` — every guard test green:

- **real shipped set → zero findings** (the 23 leaks above are all gone; SC-001/002/003/005)
- **synthetic inject → exactly three findings**, one per class (SC-005 negative)
- **conditional spec-kit path preserved** — a gated `specs/<feature>/feedback/` paragraph passes;
  the same path ungated is still one Class-B finding (FR-002 both directions)
- **discovery surface did not narrow** — the scan still covers all 7 product-skill ids (FR-007 edge)
- **finding shape** names skill + class + token + file:line (FR-007 / Principle VI)

## Wrapper-vs-canonical parity (T022 / FR-006 / SC-006)

`dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj` → `Passed: 212, Failed: 0`
(matches the T002 baseline green for this project).

Parity required one canonical-side change (the FR-006 parity exception): Feature 168's framework
guidance rules `package-pin-drift` and `readiness-allowlisting` *required* the removed framework-
evidence tokens on the **product-skill** surface (`template/product-skills/fs-gg-testing`) — i.e. the
rules were the leak's enforcement mechanism. The de-leak is completed by rescoping those two rules off
the produced product-skill surface in `tools/Rendering.Harness/SkillParity.fs`; both rules still
govern the framework's own command/source skills (`speckit-*`, `src/testing`, `src/controls`,
`src/skiaviewer`, `template/fragments/*`, `fs-gg-project`), which legitimately still carry the
references. No public surface changed (the `defaultGuidanceRules` signature is unchanged — data-only
edit), so no `.fsi` / surface-area baseline delta.

