# Research: Deliver the Symbology Product Skill to Consumers

All findings below are grounded in source on `main` / the current branch and are cited by file:line.

## R1 — GV-3 ("spec-kit byte-identical") is **not** a blocker; the 219 deferral rested on a misread

**Decision**: Wire the symbology product-skill source. It does **not** red GV-3.

**Rationale**: GV-3 does not compare the current template against a frozen historical snapshot. The
env-gated validator scaffolds each profile twice — once with no `--lifecycle` flag (the default) and
once with explicit `--lifecycle spec-kit` — and asserts the two trees are byte-for-byte identical
(`scripts/validate-lifecycle-template.fsx:318-322`: *"explicit spec-kit == no-value default, byte
for byte"*; surfaced as `spec-kit/<profile>: generate=pass diff-vs-today=none` and asserted at
`tests/Package.Tests/Feature204LifecycleTemplateTests.fs:172-175`). A new ungated, profile-gated
source emits **identically** under both invocations (the default value of `lifecycle` *is*
`spec-kit`), so the explicit==default invariant is preserved.

What genuinely changes is the **content** of `.claude/skills/fs-gg-symbology/SKILL.md` (and the
`.agents` twin) inside a generated spec-kit app: today it is the 506/507-byte framework wrapper
blanket-copied from repo root, a dangling pointer to `../../../src/Symbology/skill/SKILL.md`; after
wiring it is overwritten by the 12788-byte product skill. No test gates that file against a frozen
baseline (the `readiness/early-scaffold.md` byte record is an artifact, not an enforced assertion —
`validate-lifecycle-template.fsx:26`). The change is the **intended deliverable**.

**Evidence the 219 rationale was a misread**: 219 R5 claimed the six wired skills "already overwrite
their blanket-copy variant under spec-kit today, so dropping their lifecycle clause is
spec-kit-neutral," implying byte-equality between repo-root wrapper and product-vendored content.
Direct check disproves the byte-equality reading: `.claude/skills/fs-gg-scene/SKILL.md` is 353 bytes
while `template/product-skills/fs-gg-scene/SKILL.md` is 3703 bytes — they **differ**. The six stay
spec-kit-neutral not because the two are byte-equal, but because their product sources *already
emitted under spec-kit before* 219 (219 only removed the `lifecycle` conjunct). Symbology has no
prior source, so it has nothing to be "neutral" against — and the only invariant that exists
(explicit==default) is unaffected.

**Alternatives considered**:
- *Keep symbology not-vendored (219's conservative landing)* — rejected: it is the exact bug #35
  describes; the skill is content-complete and product-appropriate (R2).
- *Gate the new source to `lifecycle != spec-kit` to "protect" spec-kit output* — rejected:
  unnecessary (GV-3 is unaffected) and it would diverge from the six-skill pattern, leaving spec-kit
  consumers with the broken stub.

**Verify-on-implement**: This is a read-derived conclusion. Run
`FS_GG_RUN_LIFECYCLE_VALIDATION=1 dotnet fsi scripts/validate-lifecycle-template.fsx` after wiring
and confirm `spec-kit/<profile>: generate=pass diff-vs-today=none` for every profile and
`framework-skills-present=ok` under `sdd`/`none`. (Scheduled as the Foundational live run.)

## R2 — Profile set: ship symbology to the `fs-gg-scene` profile set (FR-002 decision)

**Decision**: Source symbology with the **same** profile predicate as `fs-gg-scene`:
`(profile == "app" || profile == "headless-scene" || profile == "governed" || profile == "sample-pack" || profile == "game")`,
with no `lifecycle` clause, dual-emitted to `.agents/skills/` and `.claude/skills/`.

**Rationale**: Symbology is scene-token authoring — its grammar is *Token → Scene*
(`template/product-skills/fs-gg-symbology/SKILL.md`). It has no value without scene, and `fs-gg-scene`
ships to exactly this set (`.template.config/template.json:253-262`, condition at :254/:258 — verified to include `game`). This decision:
- satisfies US1-P1 ("`game` profile at minimum") and the spec's default assumption to include `app`;
- matches Feature 219 R5's original intended landing (the scene-bearing set) plus `game` (which did
  not exist at 219 time, added in Feature 220);
- makes every profile's inclusion intentional and uniformly testable.

**Alternatives considered**:
- *`game` + `app` only* — narrower; defensible against the literal spec text, but under-serves
  `headless-scene`/`governed`/`sample-pack` which are scene-bearing and the natural home for
  visual-token authoring. Rejected for inconsistency with `fs-gg-scene`. (If the user prefers the
  narrower scope at `/speckit-tasks` time, only the condition string and the emit-test matrix rows
  change — a 1-line swap.)
- *All profiles unconditionally* — rejected: no non-scene profile exists today, so it would be
  identical in effect to the scene set but less self-documenting.

**Note on the 219 emit matrix**: `Feature219EmitFrameworkSkillsTests.expectedFrameworkSkills`
(`:42-46`) has **no `game` row** (219 predates the `game` profile), so `game` emit is not covered
there. The new emit assertions (FR-005) must cover `game` explicitly (it is the P1 profile).

## R3 — Parity fix: narrow the product-skill wrapper-satisfaction rule (FR-004)

**Decision**: In `SkillParity.missingWrapperFindings`
(`tools/Rendering.Harness/SkillParity.fs:824-861`; the satisfaction `if` is at :847), the bare-canonical-name match
(`names.Contains canonicalName`) MUST NOT satisfy a **product-skill** canonical's wrapper
requirement. Guard it so that for entries whose path is under `template/product-skills`, only the
product alias (`exposedAsAlias`) — or an explicit recorded exception — counts.

**Current code** (the blind spot, the `if` at `:847`):
```fsharp
if names.Contains(canonicalName) || exposedAsAlias || antCanonicalSelfExposed then None else Some ...
```
For symbology, `canonicalName = "fs-gg-symbology"` is present as a **framework wrapper** on both
surfaces, so `names.Contains canonicalName` is true and the missing product wrapper is never
reported — parity is green over a real hole.

**Proposed change** (minimal, preserves every other satisfaction path; hoist `isProductSkill`
**once** and reuse it in `exposedAsAlias` rather than recomputing the `template/product-skills`
check at :843):
```fsharp
let isProductSkill = entry.Path.Contains("template/product-skills", StringComparison.OrdinalIgnoreCase)
let exposedAsAlias = isProductSkill && names.Contains productAliasName   // reuse, no duplicate check
let canonicalSatisfies = (not isProductSkill) && names.Contains(canonicalName)
if canonicalSatisfies || exposedAsAlias || antCanonicalSelfExposed then None else Some ...
```

**Rationale / why this narrowing and not removing the bare match wholesale** (spec assumption,
spec.md:115): `requiresWrapper` (`:817-822`) covers four canonical surfaces —
`package-canonical`, `ant-canonical`, `template/product-skills` paths, and `fixture-canonical`. Only
the product-skills branch is the one whose alias-shaped wrapper is the *intended* satisfier; the
package/ant/fixture canonicals legitimately satisfy via the bare name and/or the
`antCanonicalSelfExposed` path. Scoping the narrowing to `isProductSkill` fixes the hole without
touching those.

**Regression safety (FR-006)**: each of the six already-delivered product skills has its
`fs-gg-product-*` alias present on both surfaces (`ls .claude/skills/` and `.agents/skills/`
confirm), so `exposedAsAlias` is true for them — they stay green after the narrowing. The new
symbology entry becomes the only one that needs its alias added (R4 wrapper task).

## R4 — Reverse the Feature 219 "not-vendored" record (test + validator deltas)

Feature 219 hard-coded symbology as the lone intentionally-unwired product skill. Wiring it requires
reversing those records:

- **`Feature219EmitFrameworkSkillsTests.fs:42-46`** `expectedFrameworkSkills` — add `fs-gg-symbology`
  to each row of a profile that now ships it (and add the missing `game` row). The G-EMIT matrix test
  (`:120-125`) then asserts the new set.
- **`Feature219EmitFrameworkSkillsTests.fs:199-211`** `G-NODANGLE-SYMB` — currently asserts the
  unwired set equals `{ fs-gg-symbology }` and the report contains `symbology: not-vendored`. After
  wiring, the unwired set is **empty** (every `template/product-skills/<id>` dir is sourced) and the
  report token becomes `symbology: vendored`. Update both assertions; the test keeps its guard
  purpose ("no product-skill directory is silently unwired").
- **`Feature219EmitFrameworkSkillsTests.fs:131`** `sources.Length >= 12` — still holds (now 14); no
  change required, but the comment should note the new count.
- **`scripts/validate-lifecycle-template.fsx:418`** `line "symbology: not-vendored"` → `line
  "symbology: vendored"`.
- **`Feature204LifecycleTemplateTests.fs`** — GV-3 (`:172`) is **unchanged** (R1). The
  `gatedSourceAudit` framework-skill/product counts (219 research recorded `framework-skill >= 12`,
  `product >= 3`) are `>=` thresholds and still pass at 14; verify no exact-equality count breaks
  during implementation and bump the comment.
- **`Feature219EmitFrameworkSkillsTests.fs:38-41`** and **data-model.md** narrative — the
  "symbology not-vendored (research R5)" comments are reversed to "vendored (Feature 223)".

These edits must **fail before** the manifest change and **pass after** (Principle V); sequence the
manifest edit and the test edits so the red→green transition is demonstrable.

## R5 — Cross-repo delivery: republish + `fs-gg-ui-template` contract entry (FR-007/FR-008)

**Decision**: Treat producer-side wiring (this repo) and consumer delivery (republish + contract
status) as the standard two-step the repo already uses (Features 218/220/221/222).

- The change reaches consumers only on the **next** `fs-gg-ui-template` republish (spec Edge Case
  "Republish timing"; #33's prior release does not carry it).
- The `fs-gg-ui-template` contract entry lives in **`FS-GG/.github`** (`registry/dependencies.yml`
  + `docs/registry/compatibility.md`), not in this repo. Updating it, and moving Coordination board
  item **#35** to Done, is handled through the **`cross-repo-coordination`** skill per the canonical
  protocol. The change is **additive / surface-neutral at the template-parameter level** (no new
  parameter; profile predicates unchanged in shape) but it **does** change emitted product content,
  so it rides a new coherent-set version fixed by the merge/release flow (Feature 204/218
  precedent), not hard-coded here.

**Rationale**: Keeps the version/coherence bump in the release flow that owns it, and routes the
cross-repo contract + board update through the one skill that owns that protocol — avoiding an
ad-hoc registry edit from inside this feature branch.

## Open questions / NEEDS CLARIFICATION

None blocking. The single genuine product judgment (R2 profile breadth: scene-set vs. `game`+`app`)
is resolved with a documented default (scene set) and is a 1-line swap if the user prefers narrower
scope at task time.
