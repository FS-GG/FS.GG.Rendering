# Feature Specification: fs-gg-ui template must not write UI skills into orchestrator-owned skill trees

**Feature Branch**: `228-fix-scaffold-skill-leak`

**Created**: 2026-07-01

**Status**: Draft

**Input**: User description: "start the next unblocked rendering item on the coordination board." → resolved to the next unblocked rendering-scoped item: **Templates#47 / contract `fs-gg-ui-template`** — the `fs-gg-ui` template writes its UI product skills into the SDD-owned `.claude/skills/` tree, so an SDD-orchestrated scaffold is rejected with `scaffold.providerWroteSddTree` and aborts. Its only board blocker (SDD#55) is now Done, so the item is unblocked and the fix lives in this repository.

## Context (non-normative)

When a product is scaffolded **through SDD** (`fsgg-sdd scaffold --provider rendering …`, e.g. via `new-sdd-fullstack`), SDD acts as the orchestrator and **owns** the process-skill trees `.claude/skills/` and `.codex/skills/` (it seeds them with its 15 `fs-gg-sdd-*` process skills). The `fs-gg-ui` template is a **provider**, and a provider owns only the `.agents/skills/` tree.

Today the template emits its UI product skills (for the requested profile — up to 8 for `app`/`game`) into **both** `.agents/skills/` (correct) **and** `.claude/skills/` (an intrusion into an orchestrator-owned tree). The template's own `dotnet new` exits `0`, so the intrusion is caught only afterward by SDD's boundary check, which reports `scaffold.providerWroteSddTree` (severity `error`). The scaffold report comes back `outcome: "blocked"` / `scaffold.outcome: "providerFailed"`, and the `new-sdd-fullstack` script aborts (`set -e`) **before** the governance overlay and `doctor` steps ever run. This blocks the full-stack composition path and the TestSpec tutorial (Part A step 2).

This is a **content/configuration** fix inside the template's scaffold source map. It changes no runtime source, no public API surface, and no package version.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - SDD-orchestrated scaffold completes cleanly (Priority: P1)

A developer runs the full-stack SDD scaffold against the pinned `fs-gg-ui` template (`new-sdd-fullstack ./SpaceInvaders Spaceinvaders`, or `fsgg-sdd scaffold --provider rendering --param productName=Spaceinvaders`). The scaffold completes and is certified: the provider writes nothing into any orchestrator-owned tree, so SDD's boundary check passes and the downstream governance-overlay and `doctor` steps run to completion.

**Why this priority**: This is the reported defect. Until it is fixed, **every** SDD-orchestrated scaffold of a UI product fails at the boundary check and the whole full-stack path is unusable. Fixing it restores the primary cross-repo composition scenario.

**Independent Test**: Scaffold an `app`/`game` product under the SDD lifecycle and confirm the scaffold report returns a success outcome (no `providerWroteSddTree`, no `providerFailed`) and that the orchestrator-owned trees contain **zero** provider-written UI skill files.

**Acceptance Scenarios**:

1. **Given** the fixed template, **When** `fsgg-sdd scaffold --provider rendering --param productName=X --profile game` runs, **Then** the scaffold report shows a success outcome with **no** `scaffold.providerWroteSddTree` diagnostic.
2. **Given** a product scaffolded under the SDD lifecycle, **When** its skill trees are inspected, **Then** `.claude/skills/` and `.codex/skills/` contain **only** SDD's own `fs-gg-sdd-*` process skills (no `fs-gg-*` UI skills), and every UI skill for the profile is present under `.agents/skills/`.
3. **Given** the fixed template, **When** `new-sdd-fullstack ./SpaceInvaders Spaceinvaders` runs end to end, **Then** the script proceeds past the scaffold step into the governance-overlay and `doctor` steps instead of aborting.

---

### User Story 2 - Standalone Spec Kit product keeps its skills; provider tree never shrinks (Priority: P2)

The fix must be surgical along the **lifecycle** axis and must never reduce the **provider-owned** skill tree. Under the standalone `spec-kit` lifecycle — where there is no external orchestrator to re-supply agent-context — the product keeps receiving exactly the skills it receives today, in both `.agents/skills/` and its `.claude/skills/` mirror. Under **every** lifecycle, the provider-owned `.agents/skills/` UI-skill set is unchanged: the fix removes only the orchestrator-owned `.claude/skills/` duplicates, and only where an orchestrator owns them.

**Why this priority**: A too-broad change that also stripped `.agents/skills/`, or that stripped `.claude/skills/` under `spec-kit`, would trade the reported leak for a fresh regression (a standalone product with no discoverable skills). The `spec-kit` product and the provider-owned tree are the invariants that must not move.

**Independent Test**: Scaffold `game` under `spec-kit` and diff its full emitted skill set against the pre-fix baseline (must be identical); separately, across all three lifecycles, confirm `.agents/skills/` holds exactly the profile's UI skills in every case.

**Acceptance Scenarios**:

1. **Given** the fixed template, **When** a `game` product is scaffolded under the `spec-kit` lifecycle, **Then** its emitted skill file set (across `.agents/skills/` **and** `.claude/skills/`) is byte-for-byte the same set as before the fix.
2. **Given** the fixed template, **When** a product is scaffolded under **any** lifecycle (`spec-kit`, `sdd`, `none`), **Then** `.agents/skills/` contains exactly the profile's UI skills — the provider-owned tree is never reduced.
3. **Given** the fixed template, **When** products are scaffolded under `sdd` and under `none`, **Then** they produce **identical** skill-tree output (neither writes `.claude/skills/` UI skills), matching the lifecycle contract's "`none` = same template-level output as `sdd`."

---

### User Story 3 - A regression guard proves the boundary holds (Priority: P3)

A maintainer needs assurance that the template will not silently reacquire this leak (e.g. when a future feature adds another product skill, as Feature 227 just added `fs-gg-layout`). A repo-owned automated check asserts that, under the SDD lifecycle, the template emits nothing into the orchestrator-owned skill trees.

**Why this priority**: Feature 227 (and its predecessors) each added a `.claude/skills/` copy per new skill; without a guard, the next skill re-introduces the exact intrusion. The guard is what keeps the fix from regressing, but the product value (P1) is delivered even before the guard exists.

**Independent Test**: Add/adjust a check that fails on the pre-fix template (UI skills present in an orchestrator-owned tree under the SDD lifecycle) and passes on the fixed template.

**Acceptance Scenarios**:

1. **Given** the repo-owned check, **When** the template is regressed to emit a UI skill into `.claude/skills/` (or `.codex/skills/`) under the SDD lifecycle, **Then** the check fails and names the offending path.
2. **Given** the fixed template, **When** the check runs, **Then** it passes and reports zero orchestrator-owned-tree intrusions for every profile.

---

### Edge Cases

- **Every profile that ships product skills** (`app`, `game`, `governed`, `sample-pack`, `headless-scene`) must be clean under the SDD lifecycle — the leak count varies by profile (`app`/`game` ship the most), so the guard must cover all profiles, not just `game`.
- **Skills already scoped to the `spec-kit` lifecycle** (the base `fs-gg-project` mirror, `fs-gg-samples`, `fs-gg-feedback-capture`) never emit under `sdd` today and must remain unaffected — the fix targets only the currently un-lifecycle-gated product-skill copies.
- **Consumer-side workaround remnants**: a product that was scaffolded before the fix and manually cleaned (delete the leaked skills, re-run `doctor`) must not be broken by re-scaffolding with the fixed template.
- **The `.agents/skills/` tree is unchanged**: the fix removes only the orchestrator-owned duplicates; it must never reduce what lands under the provider-owned `.agents/skills/`.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: When scaffolding under the **SDD lifecycle**, the `fs-gg-ui` template MUST NOT write any file into an orchestrator-owned tree — specifically `.claude/skills/`, `.codex/skills/`, `.fsgg/`, `work/`, and `readiness/` (per the `fs-gg-ui-template` contract's FR-008 / FR-011).
- **FR-002**: For every profile that ships UI product skills, those skills MUST continue to be delivered into the provider-owned `.agents/skills/` tree under **all** lifecycles (`spec-kit`, `sdd`, `none`) — no reduction versus today.
- **FR-003**: Under the **`spec-kit`** lifecycle, the emitted skill file set MUST be unchanged from the current behavior (no skills added or removed, including the `.claude/skills/` mirror). Under **`sdd`** and **`none`**, the template MUST produce identical skill-tree output (the `.claude/skills/` UI-skill copies are removed in both, per the lifecycle contract's "`none` = same template-level output as `sdd`"); `none` changing here is a deliberate correction, not a regression.
- **FR-004**: An SDD-orchestrated scaffold of a UI product (`fsgg-sdd scaffold --provider rendering …`) MUST return a **success** outcome — not `blocked` / `providerFailed` — with no `scaffold.providerWroteSddTree` diagnostic.
- **FR-005**: The full-stack composition path (`new-sdd-fullstack`) and the TestSpec tutorial Part A step 2 MUST proceed past the scaffold step into the governance-overlay and `doctor` steps.
- **FR-006**: A repo-owned automated check MUST assert that, under the SDD lifecycle, the template emits **zero** provider files into the orchestrator-owned skill trees, across every profile that ships product skills — so the leak cannot silently return when a future skill is added.
- **FR-007**: The fix MUST be delivered as **content/configuration only** — no `src/**` change, no public API/`.fsi` surface change, and no package version bump (Tier 2).
- **FR-008**: Delivery MUST be confirmed by an **observed scaffold** (before/after), not by a passing deterministic test alone (the Feature 175 lesson: a green test can accompany a wrong artifact).

### Key Entities *(include if feature involves data)*

- **Scaffold source map**: the template's declared set of source→target copy rules that decide which files land in a generated product, gated by `profile`, `lifecycle`, and other switches. The defect lives here: the per-product-skill copies into the orchestrator-owned tree are gated by `profile` only, missing the lifecycle guard the base-tree copies already have.
- **Orchestrator-owned skill trees**: `.claude/skills/` and `.codex/skills/` (plus `.fsgg/`, `work/`, `readiness/`) — trees SDD seeds and certifies when it orchestrates a scaffold; a provider must never write into them.
- **Provider-owned skill tree**: `.agents/skills/` — the tree the `fs-gg-ui` template legitimately owns and where its UI product skills belong.
- **Scaffold report / boundary check**: SDD's post-write certification that flags `scaffold.providerWroteSddTree` and sets the `blocked` / `providerFailed` outcome.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An SDD-orchestrated scaffold (`fsgg-sdd scaffold --provider rendering --param productName=X`) of an `app` and of a `game` product returns a success outcome with **0** `providerWroteSddTree` intrusions reported (today: reported for 8 files on `game` — of the 9 profile-gated `.claude/skills/` product-skill sources being fixed, the `game` profile triggers 8; the 9th does not apply to `game`, so "9 sources fixed" and "8 files leaked on `game`" are consistent, not a mismatch).
- **SC-002**: A product scaffolded under the SDD lifecycle has **0** `fs-gg-*` UI skill files under `.claude/skills/` and **0** under `.codex/skills/`, while **100%** of the profile's UI skills are present under `.agents/skills/`.
- **SC-003**: Under `spec-kit`, the emitted skill file set is **identical** to the pre-fix baseline (0 files added, 0 removed). The provider-owned `.agents/skills/` UI-skill set is **identical** to the pre-fix baseline under all three lifecycles. `sdd` and `none` produce **identical** skill-tree output to each other.
- **SC-004**: The full-stack scaffold script (`new-sdd-fullstack`) runs to completion (governance overlay + `doctor` both execute) where it previously aborted at the scaffold step.
- **SC-005**: The repo-owned regression guard fails on the pre-fix template and passes on the fixed template, covering every profile that ships product skills.

## Assumptions

- **`lifecycle == "spec-kit"` is the correct gate (not "sdd only").** The template already gates its base `.claude/` agent-context (and the `.agents/skills/`→`.claude/skills/` base mirror, and the sample-pack/feedback skills) on `lifecycle == "spec-kit"`; the per-profile product-skill `.claude/skills/` copies simply **missed** that gate. Aligning them to `lifecycle == "spec-kit"` is the smallest change consistent with the existing pattern. Consequence: `none` also stops emitting `.claude/skills/` copies — which is **correct**, because the lifecycle contract defines `none` as producing the same template-level output as `sdd` (both suppress the gated agent-context set for an external owner to re-supply). Only `spec-kit`, the standalone lane with no external orchestrator, keeps the `.claude/skills/` mirror. (Chosen over a narrower "`lifecycle != sdd`" gate, which would leave `none` inconsistent with its own contract.)
- **The provider-owned `.agents/skills/` copies stay profile-gated (all lifecycles).** `.agents/skills/` is the provider's canonical tree; under `sdd` the SDD orchestrator's skill fan-out mirrors it into the consumer agent trees, so the provider must write **only** there. The fix touches **only** the 9 `.claude/skills/fs-gg-*/` per-profile sources; the 9 matching `.agents/skills/fs-gg-*/` sources are unchanged. The template never writes `.codex/skills/` at all, so no `.codex` change is needed.
- **SDD is responsible for surfacing provider skills under the SDD lifecycle.** Once the provider confines its UI skills to `.agents/skills/`, making those skills discoverable to the generated product's agent tooling under the SDD lifecycle is SDD's orchestrator concern (its skill fan-out), tracked in the SDD repo — out of scope here.
- **The `.agents/skills/` delivery is already correct.** Evidence on disk in the report shows `.agents/skills/` already has exactly the profile's UI skills; only the orchestrator-owned duplicates are removed.
- **No src/behavior change.** This is a Tier 2 content/config change to the template's scaffold source map plus a repo-owned guard; it adds no runtime module, no `.fsi`, and no version bump (mirrors the delivery shape of Features 226/227).
- **Verification rides existing gates plus a live scaffold observation.** Evidence (before/after scaffold reports, per-lifecycle skill-set diffs, guard transcript) is recorded under `specs/228-fix-scaffold-skill-leak/readiness/`.
- **Dependency now satisfied.** The item's board blocker (SDD#55, the scaffold-guard over-match) is Done/closed, so this work is unblocked; the sibling nuget.org-publish rendering item (#40) remains blocked on `.github#103` and is intentionally not addressed here.
