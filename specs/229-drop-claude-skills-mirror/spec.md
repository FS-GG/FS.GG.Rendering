# Feature Specification: fs-gg-ui template emits UI skills to the provider-owned tree only (drop the `.claude/skills/` mirror)

**Feature Branch**: `229-drop-claude-skills-mirror`

**Created**: 2026-07-01

**Status**: Draft

**Input**: User description: "start the next rendering item on the coord board." → resolved to the next Ready rendering-scoped item: **FS.GG.Rendering#42 / contract `fs-gg-ui-template`** — re-release the template so it materializes its UI product skills **only** into `.agents/skills/` and drops the `.claude/skills/` copies entirely, per **ADR-0011**. Unblocks `FS-GG/FS.GG.Templates#47` (`scaffold.providerWroteSddTree`).

## Context (non-normative)

Per **ADR-0011** (Accepted 2026-07-01, `FS-GG/.github/docs/adr/0011-agent-skill-roots-full-union-orchestrator-owned-mirror.md`), a scaffolded product carries three interchangeable agent-skill roots — `.claude/skills/` (Claude Code), `.codex/skills/` (Codex), `.agents/skills/` (generic). Each root MUST hold the **byte-identical union** of every skill produced for the product (SDD's `fs-gg-sdd-*` process skills ∪ the provider's `fs-gg-*` UI skills). The ADR assigns the **mirror authority to the `fsgg-sdd` orchestrator** (ADR-0008): after invoking the provider, the CLI computes the union and materializes real files into all three roots. Consequently:

- **Providers are confined to `.agents/skills/`.** A provider MUST NOT write `.claude/skills/` or `.codex/skills/` (ADR-0011 §3). SDD's `isSddTree` intrusion guard stays strict — it is correct once providers stop writing those roots.
- **The `fs-gg-ui` template drops its `.claude/` UI-skill emission** (ADR-0011 §4): it changes from "emit each UI skill to `.agents/` **and** `.claude/`" to "emit to `.agents/` only"; the orchestrator fans them out.

**Relationship to Feature 228 (this repo, already merged).** Feature 228 *gated* the 9 per-profile `.claude/skills/fs-gg-*/` product-skill copies to `lifecycle == "spec-kit"` — removing them under `sdd`/`none` but **keeping** them under `spec-kit` (its FR-003 declared the `spec-kit` skill set byte-identical). ADR-0011 §3/§4 make the provider confinement **unconditional**: the `.claude/skills/` UI-skill copies must be removed under **every** lifecycle, `spec-kit` included. This feature therefore **supersedes** Feature 228's `spec-kit`-keeps-the-mirror invariant; the drop under `spec-kit` is the deliberate, ADR-mandated change, not a regression.

**Observed today.** At the current pin `FS.GG.UI.Template::0.1.58-preview.1` the template writes UI skills into **both** `.agents/skills/` (correct) and `.claude/skills/` (the leak) under every lifecycle; `.codex/skills/` is untouched — the asymmetry confirms the `.claude/` write is unintended. (Feature 228's `spec-kit`-only gating is merged but unreleased, so the shipped pin still leaks everywhere.)

**Ordering (publish-before-flip).** Both halves of ADR-0011 must ship before a full-stack scaffold is clean end-to-end: the SDD orchestrator fan-out (`FS-GG/FS.GG.SDD#57`, `fsgg-sdd 0.4.0`) and this template change, landing in parallel. This item delivers the **Rendering half** — the template stops writing `.claude/skills/` and re-releases the coherent set. The registry flip and the `FS.GG.Templates` provider re-pin are cross-repo follow-ons (out of this repo's implement scope).

This is a **content/configuration** fix inside the template's scaffold source map plus the repo-owned emission gates. It changes no runtime source and no public F# API surface.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Provider writes only its own skill tree, under every lifecycle (Priority: P1)

A developer runs the full-stack SDD scaffold against the re-released `fs-gg-ui` template (`fsgg-sdd scaffold --provider rendering --param productName=Spaceinvaders`, e.g. via `new-sdd-fullstack`). The provider writes its UI skills **only** into `.agents/skills/`; it writes nothing into `.claude/skills/` or `.codex/skills/`. SDD's boundary check passes, the orchestrator's fan-out (SDD#57) materializes the full union into all three roots, and the full-stack path proceeds past the scaffold step into the governance-overlay and `doctor` steps.

**Why this priority**: This is the reported defect and the ADR-0011 §4 consequence for this repo. Until the provider stops writing `.claude/skills/`, every SDD-orchestrated scaffold of a UI product fails the `isSddTree` boundary check (`scaffold.providerWroteSddTree`) and the full-stack composition path is unusable. Delivering it unblocks the Rendering half of Templates#47.

**Independent Test**: Scaffold an `app`/`game` product under the `sdd` lifecycle and confirm the scaffold report returns a success outcome (no `providerWroteSddTree`, no `providerFailed`) and that `.claude/skills/` and `.codex/skills/` contain **zero** provider-written `fs-gg-*` UI skill files, while every UI skill for the profile is present under `.agents/skills/`.

**Acceptance Scenarios**:

1. **Given** the re-released template, **When** `fsgg-sdd scaffold --provider rendering --param productName=X --profile game` runs, **Then** the scaffold report shows a success outcome with **no** `scaffold.providerWroteSddTree` diagnostic.
2. **Given** a product scaffolded under the `sdd` lifecycle, **When** its skill trees are inspected, **Then** `.claude/skills/` and `.codex/skills/` contain **only** SDD's own `fs-gg-sdd-*` process skills (plus whatever the orchestrator fan-out mirrors), and **no** provider-written `fs-gg-*` UI skill was authored by the template into either tree.
3. **Given** the re-released template, **When** `new-sdd-fullstack ./SpaceInvaders Spaceinvaders` runs end to end, **Then** the script proceeds past the scaffold step into the governance-overlay and `doctor` steps instead of aborting.

---

### User Story 2 - The provider-owned `.agents/skills/` tree still carries the full UI-skill set, every lifecycle (Priority: P2)

The change must remove **only** the `.claude/skills/` UI-skill copies and must never reduce the **provider-owned** `.agents/skills/` tree. Under **every** lifecycle (`spec-kit`, `sdd`, `none`) the profile's complete UI-skill set MUST still land under `.agents/skills/`, byte-identical to today. The `.agents/skills/` tree is the single canonical provider surface the orchestrator reads to build the union.

**Why this priority**: A change that also stripped `.agents/skills/` would trade the reported leak for a worse regression — a product (and an orchestrator fan-out) with no UI skills to surface at all. The provider-owned tree is the invariant that must not move.

**Independent Test**: Across all three lifecycles, scaffold `app`/`game` and confirm `.agents/skills/` holds exactly the profile's UI skills (identical set to the pre-change baseline) in every case; separately confirm no `.claude/skills/fs-gg-*` UI skill is authored by the template under any lifecycle.

**Acceptance Scenarios**:

1. **Given** the re-released template, **When** a product is scaffolded under **any** lifecycle (`spec-kit`, `sdd`, `none`), **Then** `.agents/skills/` contains exactly the profile's UI skills — the provider-owned tree is byte-identical to the pre-change baseline.
2. **Given** the re-released template, **When** a product is scaffolded under **any** lifecycle, **Then** the template authors **zero** `.claude/skills/fs-gg-*/` UI-skill files (the `spec-kit` lane no longer mirrors UI skills into `.claude/skills/`, superseding Feature 228 FR-003).
3. **Given** the re-released template, **When** products are scaffolded under `sdd` and under `none`, **Then** they produce **identical** skill-tree output (neither authors any `.claude/skills/` UI skill), consistent with the lifecycle contract's "`none` = same template-level output as `sdd`."

---

### User Story 3 - The corrected emission gates prove the provider boundary holds (Priority: P3)

A maintainer needs assurance that the template will not silently reacquire a `.claude/skills/` UI-skill write when a future feature adds another product skill. The repo-owned emission gates (Feature 219 emission matrix, Feature 204 lifecycle audit) — which currently encode Feature 228's `spec-kit`-gated `.claude/skills/` shape — MUST be corrected to encode the ADR-0011 invariant: **no** product-skill source targets `.claude/skills/` (or `.codex/skills/`) under **any** lifecycle. A guard that failed on the old shape and passes on the new one keeps the boundary from regressing.

**Why this priority**: The Feature 219/204 gates assert the pre-ADR shape; left unchanged they would fail on the corrected template. Correcting them both unblocks the build and turns them into the standing regression guard (they would have caught Feature 227's `.claude/skills/` addition). The product value (P1) is delivered even before the guard is re-encoded, but the guard is what keeps it from silently returning.

**Independent Test**: Corrected gates fail on the pre-fix template (a product-skill source still targets `.claude/skills/`) and pass on the fixed template; add a live observation that `.claude/skills/` holds zero template-authored UI skills under `spec-kit`, `sdd`, and `none`.

**Acceptance Scenarios**:

1. **Given** the corrected Feature 219 gate, **When** any product-skill source targets `.claude/skills/` or `.codex/skills/` (under any lifecycle), **Then** the gate fails and names the offending source.
2. **Given** the corrected Feature 204 gate, **When** the template is audited, **Then** every product-skill source targets `.agents/skills/` only and the lifecycle report reflects zero `.claude/skills/` UI-skill destinations.
3. **Given** the re-released template, **When** the live scaffold observation runs under `spec-kit`, `sdd`, and `none`, **Then** all three record **zero** template-authored `fs-gg-*` UI skills under `.claude/skills/` and the full profile UI-skill set under `.agents/skills/`.

---

### Edge Cases

- **Every profile that ships product skills** (`app`, `game`, `governed`, `sample-pack`, `headless-scene`) must have zero template-authored `.claude/skills/` UI skills under every lifecycle — the count of removed copies varies by profile (`app`/`game` ship the most), so the gate must cover all profiles.
- **Standalone `spec-kit` discoverability**: dropping the `.claude/skills/` UI skills under `spec-kit` is deliberate (ADR-0011 §3, unconditional provider confinement). Under the standalone lane a product's Claude Code discovers UI skills in `.agents/skills/` only; the `.claude/skills/` tree carries no UI product skills (only the base `fs-gg-project` workspace skill from `template/base/.claude/`). Surfacing provider UI skills into all three consumer roots is the orchestrator's concern (SDD fan-out), not this template's.
- **All provider `.claude/skills/` writes removed** — the base `.agents/skills/`→`.claude/skills/` mirror, the sample-pack skill, and the feedback-capture skill (all previously `spec-kit`-gated) are removed together with the 8 UI product-skill destinations. None fired under `sdd`, but leaving the base mirror kept UI skills in `.claude/skills/` under `spec-kit` — so full confinement removes all of them. The only surviving `.claude/skills/` entry is the base `fs-gg-project` skill that ships inside the base `.claude/` workspace tree (kept as workspace infrastructure).
- **The `.agents/skills/` tree is unchanged**: the change removes only the `.claude/skills/` UI-skill copies; it must never reduce what lands under `.agents/skills/` for any profile or lifecycle.
- **Re-scaffolding a manually-cleaned product**: a product scaffolded on the leaky pin and manually cleaned (delete leaked `.claude/skills/fs-gg-*`, re-run `doctor`) must not be broken by re-scaffolding with the re-released template.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Under **every** lifecycle (`spec-kit`, `sdd`, `none`), the `fs-gg-ui` template MUST author its `fs-gg-*` UI product skills **only** into the provider-owned `.agents/skills/` tree; it MUST NOT author any UI product skill into `.claude/skills/` or `.codex/skills/` (ADR-0011 §3/§4).
- **FR-002**: For every profile that ships UI product skills, the complete UI-skill set MUST continue to be delivered into `.agents/skills/` under **all** lifecycles — byte-identical to the current behavior; no reduction of the provider-owned tree.
- **FR-003**: The `.claude/skills/` UI-skill copies MUST be removed under **all** lifecycles including `spec-kit` — this supersedes Feature 228 FR-003 ("`spec-kit` skill set byte-identical"). Under `sdd` and `none` the template MUST continue to author zero `.claude/skills/` UI skills (unchanged from Feature 228). `sdd` and `none` MUST produce identical skill-tree output.
- **FR-004**: An SDD-orchestrated scaffold of a UI product (`fsgg-sdd scaffold --provider rendering …`) MUST return a **success** outcome — not `blocked` / `providerFailed` — with no `scaffold.providerWroteSddTree` diagnostic attributable to the template.
- **FR-005**: The full-stack composition path (`new-sdd-fullstack`) MUST proceed past the scaffold step into the governance-overlay and `doctor` steps.
- **FR-006**: The repo-owned emission gates (Feature 219 emission matrix, Feature 204 lifecycle audit) MUST be corrected to assert that **no** product-skill source targets `.claude/skills/` (or `.codex/skills/`) under any lifecycle, and MUST fail if one does — across every profile that ships product skills.
- **FR-007**: The template MUST author **nothing** under `.claude/skills/` — no per-profile UI product skill, no base `.agents/skills/`→`.claude/skills/` mirror, and no sample-pack / feedback-capture skill (full provider confinement, ADR-0011 §3). The base `.claude/` **workspace** itself (`template/base/.claude/` — settings, hooks, and the standalone `fs-gg-project` authoring skill) is retained under `spec-kit` as Spec Kit workspace infrastructure, not a UI product-skill mirror. The change MUST NOT reduce the `.agents/skills/` sources (they carry the full skill set) and MUST NOT alter `.codex/skills/` (never written). *(Full confinement chosen over the narrower "Feature 219 per-skill only" scope because deleting the per-skill `.claude` sources alone left `spec-kit`'s `.claude/skills/` inconsistent — 7 of 8 UI skills still mirrored via the base mirror — which met neither SC-002 nor the "no `.claude/skills/` destination" DoD.)*
- **FR-008**: The `fs-gg-ui-template` coherent set MUST be **re-released** (version bumped and packed to the local/org feed) because the change alters observable scaffold-emission behavior that consumers pin — enabling the publish-before-flip sequence (registry flip + `FS.GG.Templates` provider re-pin are cross-repo follow-ons, not in this repo's implement scope).
- **FR-009**: Delivery MUST be confirmed by an **observed scaffold** (before/after, per lifecycle), not by a passing deterministic test alone (the Feature 175 lesson: a green test can accompany a wrong artifact).
- **FR-010**: The change MUST be delivered without `src/**` change and without any public F# API/`.fsi` surface change (content/configuration + test-logic correction).

### Key Entities *(include if feature involves data)*

- **Scaffold source map**: the template's declared set of source→target copy rules gated by `profile`, `lifecycle`, and other switches. The change removes **every** rule that targets `.claude/skills/…` — the 9 per-profile product-skill rules, the base `.agents/skills/`→`.claude/skills/` mirror, and the sample/feedback rules; the matching `.agents/skills/…` rules and the base `.claude/` workspace rule are unchanged.
- **Provider-owned skill tree**: `.agents/skills/` — the only tree the `fs-gg-ui` provider legitimately writes; the canonical surface the orchestrator reads to compute the union.
- **Orchestrator-owned skill trees**: `.claude/skills/` and `.codex/skills/` — `isSddTree`; the orchestrator (`fsgg-sdd`, SDD#57) materializes the full union into them. A provider must never write them.
- **Emission gates**: `Feature219EmitFrameworkSkillsTests.fs` (per-profile emission matrix) and `Feature204LifecycleTemplateTests.fs` (lifecycle audit) plus `scripts/validate-lifecycle-template.fsx` — the repo-owned checks that must encode the ADR-0011 invariant and provide the live before/after evidence.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An SDD-orchestrated scaffold (`fsgg-sdd scaffold --provider rendering --param productName=X`) of an `app` and of a `game` product returns a success outcome with **0** `providerWroteSddTree` intrusions attributable to the template.
- **SC-002**: A product scaffolded under **any** lifecycle has **0** template-authored `fs-gg-*` UI skill files under `.claude/skills/` and **0** under `.codex/skills/`, while **100%** of the profile's UI skills are present under `.agents/skills/`.
- **SC-003**: The `.agents/skills/` UI-skill set is **identical** to the pre-change baseline under all three lifecycles (0 files added, 0 removed). `sdd` and `none` produce **identical** skill-tree output to each other.
- **SC-004**: The full-stack scaffold script (`new-sdd-fullstack`) runs to completion (governance overlay + `doctor` both execute) where it previously aborted at the scaffold step.
- **SC-005**: The corrected Feature 219/204 gates fail on the pre-fix template and pass on the fixed template, covering every profile that ships product skills; the live scaffold observation records zero `.claude/skills/` UI skills under `spec-kit`, `sdd`, and `none`.
- **SC-006**: The `fs-gg-ui-template` coherent set is re-released (version bumped, packed to the feed) so the change is consumable under publish-before-flip.

## Assumptions

- **ADR-0011 is the governing decision.** Providers are confined to `.agents/skills/` unconditionally; the orchestrator owns the three-root union mirror. The template's job is to write a complete, canonical `.agents/skills/` and nothing else in the skill trees. (Accepted 2026-07-01; supersedes the #47-as-filed "`.claude/` is SDD-exclusive" framing — the provider drops `.claude/` because the orchestrator owns the mirror, not because `.claude/` is SDD-only.)
- **This supersedes Feature 228's `spec-kit` invariant.** Feature 228 kept the `.claude/skills/` UI mirror under `spec-kit`; ADR-0011 §3/§4 remove it there too. The `spec-kit` skill-set change (UI skills no longer mirrored into `.claude/skills/`) is the intended ADR consequence, explicitly disclosed here.
- **Scope = every provider `.claude/skills/` write (full confinement).** Removed: the 9 per-profile product-skill `.claude/skills/` sources (`fs-gg-scene`, `fs-gg-symbology`, `fs-gg-skiaviewer`, `fs-gg-elmish`, `fs-gg-keyboard-input`, `fs-gg-ui-widgets`, `fs-gg-styling`, `fs-gg-layout`, `fs-gg-testing`); the base `.agents/skills/`→`.claude/skills/` mirror; and the sample-pack/feedback `.claude/skills/` sources. Kept: the matching `.agents/skills/` sources; the base `.claude/` workspace tree (`template/base/.claude/` — settings, hooks, `fs-gg-project`). `.codex/skills/` is never written. *(This scope was expanded from the initial "9 per-skill sources only" after live evidence showed the base mirror kept 7/8 UI skills in `spec-kit`'s `.claude/skills/`; the maintainer chose full confinement.)*
- **The base `fs-gg-project` skill is workspace infrastructure, not a UI product-skill mirror.** It ships inside the base `.claude/` workspace tree (`template/base/.claude/`) and is the standalone Spec Kit workspace's own authoring skill. It is exempt from the "0 UI product skills in `.claude/skills/`" invariant (which counts the UI product / sample / feedback set). Under `sdd`/`none` even `fs-gg-project` is absent (the whole base `.claude/` workspace is `spec-kit`-gated).
- **Standalone `spec-kit` discoverability is the orchestrator's concern.** With the provider confined to `.agents/skills/`, surfacing UI skills into `.claude/skills/`/`.codex/skills/` is the fan-out's job. This feature does not add an in-template three-root mirror for the standalone `spec-kit` lane; if standalone `spec-kit` needs the union in `.claude/skills/`, that is a separate orchestrator/template concern tracked elsewhere.
- **A re-release is required.** Unlike Feature 228 (merged unreleased), issue #42's definition of done requires the coherent set to be re-released and published so `FS.GG.Templates` can re-pin under publish-before-flip. The version bump + local pack happens in this repo; the org-feed publish, registry flip, and Templates re-pin are cross-repo follow-ons.
- **No src/behavior change.** Tier 2 content/config change to the scaffold source map + the two repo-owned gates + the validation script; adds no runtime module, no `.fsi`. Mirrors the delivery shape of Features 226/227/228.
- **Verification rides existing gates plus a live scaffold observation.** Evidence (before/after scaffold reports, per-lifecycle skill-set diffs, gate transcripts) is recorded under `specs/229-drop-claude-skills-mirror/readiness/`.
- **Dependency posture.** This item is Ready (no open board blocker). The SDD half (SDD#57) ships in parallel under publish-before-flip; a clean end-to-end full-stack scaffold requires both halves, but this repo's deliverable — the template no longer writing `.claude/skills/` — stands on its own and is independently verifiable.
