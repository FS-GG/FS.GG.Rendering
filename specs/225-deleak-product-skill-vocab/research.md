# Phase 0 Research: De-leak Product Skill Vocabulary

All NEEDS CLARIFICATION from the Technical Context are resolved below. Findings are numbered
**R0–R4**; each records the **Finding / Decision / Rationale / Alternatives rejected**.

---

## R0 — Leak inventory (ground truth)

**Finding**: A read-only scan of `template/product-skills/` confirms **exactly 7** shipped
`SKILL.md` files (`fs-gg-elmish`, `fs-gg-keyboard-input`, `fs-gg-scene`, `fs-gg-skiaviewer`,
`fs-gg-symbology`, `fs-gg-testing`, `fs-gg-ui-widgets`) and the following leak sites, grouped by the
three spec classes:

**Class A — framework-repo evidence process** (forbidden outright; reframe to product-local):

| File | Heading | Tokens (line) |
|---|---|---|
| `fs-gg-testing/SKILL.md` | `## Feature 168 Evidence Rules` (53) | `refresh-local-feed-and-samples.fsx` (56), `package-feed` (57), `specs/*/readiness/` (59), `.gitignore` allowlist (60), `BaseOutputPath` (63) — the densest block; only file with the last three |
| `fs-gg-ui-widgets/SKILL.md` | `## Feature 168 Control Evidence Rules` (111) | `refresh-local-feed-and-samples.fsx` + `package-feed` (113–115) |
| `fs-gg-skiaviewer/SKILL.md` | `## Feature 168 Viewer Evidence Rules` (53) | `refresh-local-feed-and-samples.fsx` + `package-feed` (55–57) |

**Class B — unconditional `specs/<feature>/feedback/`** (make conditional; present in **all 7**):
`fs-gg-elmish:65`, `fs-gg-keyboard-input:101`, `fs-gg-scene:82`, `fs-gg-skiaviewer:106`,
`fs-gg-testing:89`, `fs-gg-ui-widgets:150`, `fs-gg-symbology:200`. The block is verbatim-identical in
6 skills (a "Persistent problems" paragraph) and a shortened variant in symbology.

**Class C — framework feature/spec-number stamps in prose** (forbidden outright; retitle/reword):
the three `## Feature 168 …` headings above (which Class-A rewrite already removes), plus symbology
inline stamps `feature 199` (`fs-gg-symbology:96`), `feature 200` (`:122`, `:137`), and `spec-196`
(`:63`). No other `[Ff]eature \d+` / `spec-\d+` occurrences exist in the tree.

**Decision**: The de-leak touches 4 bodies with substantive rewrites (testing, ui-widgets,
skiaviewer, symbology) and 3 with a single conditional-feedback edit (elmish, keyboard-input, scene).
Class-A and Class-C headings collapse into one retitle each (e.g. `## Evidence Rules`).

**Rationale**: A precise, line-anchored inventory lets each edit be verified as **reframing, not
removal** (SC-004) and gives the guard its exact token set.

**Alternatives rejected**: Editing only the 3 named Class-A files — rejected: Class B spans all 7 and
Class C lives in symbology, so the guard must scan the whole set, not a fixed list (spec edge case).

---

## R1 — Produced-surface verification (LIVE RUN REQUIRED)

**Finding**: US2's premise — that these 7 skills reach `app`/`game`/`sdd`/`none` products, so their
prose must not assume spec-kit — rests on Feature 219 / #30 (skills follow the product, not the
lifecycle). Per the standing assumption, this is unverified until a real scaffold confirms the
produced surface, and the guard must enumerate **exactly** the skills a product carries.

**Decision**: Defer to an **early produced-surface run** scheduled by `/speckit-tasks` in the
Foundational phase, before any prose edit: enumerate the shipped product-skill set the way the guard
will (`SkillParity` discovery filtered to `template/product-skills`) and confirm it equals the 7
expected skills; where feasible, scaffold a non-spec-kit product
(`FS_GG_RUN_LIFECYCLE_VALIDATION=1 dotnet fsi scripts/validate-lifecycle-template.fsx`) and confirm
the 7 skills are vendored and a `specs/<feature>/feedback/` folder is **absent** (proving the Class-B
leak actually dead-ends there). Record under `specs/225-.../readiness/produced-surface.md`.

**Rationale**: The whole feature's value (US2) and the guard's correctness depend on the produced
surface matching the assumption; confirming it first prevents building edits on a wrong premise
(Feature 175 lesson).

**Alternatives rejected**: Treating the 7-skill list as a static fixture — rejected: the guard must
scan whatever the package actually ships (spec edge case: "a skill not in scope today gains the leaky
boilerplate later"), so discovery, not a hardcoded list, is ground truth.

---

## R2 — Conditional-feedback recognition (how the guard tells conditional from unconditional)

**Finding**: FR-002 keeps the `specs/<feature>/feedback/` path as a **spec-kit-conditional** option
while forbidding it as the unconditional instruction. The guard therefore cannot simply ban the
token; it must distinguish a gated mention from an ungated one. Class-A framework paths
(`refresh-local-feed-and-samples`, `package-feed`, `specs/*/readiness/`, `.gitignore` allowlist,
`BaseOutputPath`) and Class-C stamps have **no** conditional form — they are banned outright.

**Decision**: A `specs/<feature>/feedback/` (or `specs/.../feedback/`) reference is treated as
**conditional** iff its enclosing Markdown paragraph contains a spec-kit gating phrase — a
case-insensitive match for `spec kit` or `spec-kit`. Otherwise it is an **unconditional** finding.
The de-leaked prose phrases it: *"If your product uses Spec Kit, record findings under
`specs/<feature>/feedback/`; otherwise record them in this skill's **Sources** / durable-lessons line
(and any product-local `docs/` location)."* `specs/*/readiness/` stays banned with no conditional
(it is framework-only output, never a product author location).

**Rationale**: Paragraph-scoped gating-phrase co-occurrence is simple, total over file text, and
matches how the corrected prose actually reads; it lets spec-kit authors keep the path (edge case)
while every other lifecycle gets a resolvable location.

**Alternatives rejected**: (a) Banning the feedback token outright — rejected: removes the genuinely
useful spec-kit path, violating FR-002 and the "spec-kit-only path is genuinely useful" edge case.
(b) A structured front-matter flag — rejected: over-engineered for one conditional sentence; the
prose itself is the contract a reader sees.

---

## R3 — Guard home and scan mechanics

**Finding**: Repo-owned checks are **Expecto tests under `tests/<Project>.Tests/`** named
`Feature<NNN><Name>Tests.fs`, run by `dotnet test`. The nearest precedent is
`tests/Package.Tests/Feature224SkillCatalogCurrencyTests.fs`, which reuses `SkillParity` discovery
(`defaultRequest` → `discoverDefaultSurfaces` → `inventorySkills`, all public in `SkillParity.fsi`)
and emits one finding per violation with doc + line. `SkillEntry` already carries `Content`, `Path`,
and `SkillName`, so a leak scan runs directly over `entry.Content` with no re-reading.

**Decision**: Add `tests/Package.Tests/Feature225ProductSkillVocabularyTests.fs`. It enumerates via
`SkillParity.inventorySkills (defaultRequest root) (discoverDefaultSurfaces root)`, filters to
`entry.Path.Contains("template/product-skills")`, and scans each `entry.Content` line-by-line for the
three leak classes (R2 rule for Class B). It keeps the scan **self-contained in the test** — consuming
only existing public discovery — so **no new public surface, no `.fsi`/baseline delta** (mirrors
Feature 224). Findings carry `{ Skill; Class; Token; File; Line }` and the assertion fails with one
formatted line per finding. A unit test injects a synthetic leaky body (one per class) → reds; the
real shipped set → green (SC-005).

**Rationale**: Reusing the one authoritative enumerator means the guard scans exactly the produced
surface and cannot drift from parity discovery; housing it as a test puts it in the existing gate
(`dotnet test`) with zero new public API to baseline.

**Alternatives rejected**: (a) A standalone `.fsx` script — rejected (as in 224): it would not run in
the test/pack gate and could be skipped silently. (b) Promoting the scan into `SkillParity.fs` as a
public helper — rejected as the default: it adds public surface requiring `.fsi` + baseline churn for
no reuse benefit; kept only as the Principle II conditional hedge if a second consumer appears.

---

## R4 — Parity safety and cross-repo sequencing

**Finding**: Several product skills are vendored copies of canonical framework skills; the
wrapper-vs-canonical parity check (`tools/Rendering.Harness/SkillParity.fs`, asserted by
`tests/Rendering.Harness.Tests/Feature168*` + `Feature223SymbologyParityTests.fs`) compares wrapper
`name:`/`description:` front-matter and body coverage against the canonical. Editing **body prose**
(not front-matter) must keep those green (FR-006). Delivery is package-content only: the corrected
skills reach consumers via a republished `FS.GG.UI.Template` coherent set + the downstream pin bump
FS-GG/FS.GG.Templates#8 (FR-008), and on completion the board item #37 and epic #34 are updated per
the coordination protocol (FR-009).

**Decision**: Confine edits to skill **bodies** below front-matter, leaving `name:`/`description:`
untouched, and re-run the parity test suite after each skill is edited to prove FR-006. Where a
product skill is a vendored copy, apply the same de-leak to the canonical source if the parity check
requires body coverage to match (verify during the parity run, not assumed). On completion, use the
`cross-repo-coordination` skill to: record the delivery dependency (republish vehicle + Templates#8
pin), update the `fs-gg-ui-template` contract/registry coherence, and comment the result on #37 +
epic #34. This feature produces the **content and guard**, not the publish.

**Rationale**: Body-only edits keep the parity invariant intact by construction; deferring the
publish to a coherent republish matches the precedent set by siblings #35 (Feature 223) and #36
(Feature 224), which carried package-content skill fixes on the shared republish tag.

**Alternatives rejected**: (a) Editing front-matter to retitle — rejected: risks `MetadataDrift`
parity failures (FR-006); retitles target body `## ` headings only. (b) Shipping a standalone release
— rejected: epic #34 sequences its children onto one coherent republish.

---

## Resolved unknowns summary

| Unknown | Resolution |
|---|---|
| Exact leak sites and token set | R0 — 7 skills; Class A in testing/ui-widgets/skiaviewer, Class B in all 7, Class C in symbology (+ the 3 Class-A headings) |
| Do these skills reach non-spec-kit products? | R1 — verified by an early produced-surface scaffold run (standing assumption), not assumed |
| How does the guard tell conditional from unconditional feedback paths? | R2 — paragraph-scoped `spec kit`/`spec-kit` gating phrase; Class A & `readiness/` banned outright |
| Where does the guard live and how does it scan? | R3 — `tests/Package.Tests/Feature225ProductSkillVocabularyTests.fs`, reuse `SkillParity` discovery, scan `entry.Content`, self-contained test (no `.fsi`/baseline delta) |
| How is parity kept green and delivery sequenced? | R4 — body-only edits + re-run `Feature168*`/`Feature223` parity; republish + Templates#8 pin; update #37/#34 via cross-repo-coordination |
