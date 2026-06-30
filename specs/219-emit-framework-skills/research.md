# Phase 0 Research: Emit Framework Skills On Every Lifecycle

All NEEDS CLARIFICATION from Technical Context are resolved below. Each decision is grounded in the actual source read during planning (not the issue text alone).

## R1 — How are framework skills emitted today, and why is `sdd`/`none` empty?

**Decision**: The 6 wired framework product-skill sources in `.template.config/template.json` each carry a condition of the form `(profile == … ) && lifecycle == "spec-kit"`. Dropping the `&& lifecycle == "spec-kit"` conjunct (leaving the profile predicate) is the whole emission fix.

**Findings (verified in `template.json`)**:
- `template/product-skills/fs-gg-scene/` → `.agents/skills/fs-gg-scene/` and `.claude/skills/fs-gg-scene/` — `(profile == "app" || "headless-scene" || "governed" || "sample-pack") && lifecycle == "spec-kit"`
- `fs-gg-skiaviewer` → `(profile == "app" || "sample-pack") && lifecycle == "spec-kit"`
- `fs-gg-elmish` → `(profile == "app" || "sample-pack") && lifecycle == "spec-kit"`
- `fs-gg-keyboard-input` → `(profile == "app") && lifecycle == "spec-kit"`
- `fs-gg-ui-widgets` → `(profile == "app") && lifecycle == "spec-kit"`
- `fs-gg-testing` → `(profile == "governed") && lifecycle == "spec-kit"`

Each skill has two sources (`.agents/skills/` and `.claude/skills/` destinations) → **12 source clauses** to edit. Removing only the `lifecycle` conjunct preserves the profile mapping exactly (FR-002) and makes the sources lifecycle-independent (FR-001).

**Rationale**: Framework-usage knowledge is orthogonal to which lifecycle workspace the consumer wants. The profile already decides *which* skills are relevant to the product shape; the lifecycle was an over-broad gate.

**Alternatives considered**:
- *Add a new boolean `vendorSkills` parameter* — rejected: adds template **surface** (violates the additive/surface-neutral constraint) and pushes a decision onto consumers that should default-on. Skills following the profile is the right default.
- *Emit skills only under `sdd` (not `none`)* — rejected: `none` consumers building a product still benefit from in-product framework guidance; the issue explicitly asks for `sdd` **and** `none`.

## R2 — Will `spec-kit` output stay byte-identical after decoupling? (FR-004 / SC-003)

**Decision**: Yes. Under `lifecycle=spec-kit` the framework skills already arrive **twice today** — first via the blanket `.agents/skills/ → .agents/skills/` (and `→ .claude/skills/`) repo copy (which carries the authoring `fs-gg-*` skills plus `speckit-*` command skills), then the per-skill `template/product-skills/fs-gg-*` source **overwrites** the product-vendored variant on top. Removing the `lifecycle` conjunct leaves the per-skill condition **true under `spec-kit`** (the profile predicate is unchanged), so the same source still emits in the same order. No file is added, dropped, or reordered on the `spec-kit` path.

**Findings**: The base `template/base/` source `exclude`s `.agents/**`, `.claude/**`, `CLAUDE.md` and they are re-emitted only under `spec-kit` (the blanket copy + agent-context). The base-source comment documents the intended order: *"base agent-context first, then the repo-root `.agents/skills/` overwrites."* The per-skill overwrite runs after that. This order is condition-independent of the `lifecycle` conjunct we remove.

**Rationale & guard**: This is a *hypothesis until the real `dotnet new` byte-diff runs* (Standing Assumption). The amended Feature 204 live matrix asserts `spec-kit/<profile>: generate=pass diff-vs-today=none` for all 4 profiles — that is the proof, not this reasoning.

**Alternatives considered**: *Remove the blanket `.agents/skills/` copy and rely solely on the per-skill sources* — rejected: the blanket copy also carries the `speckit-*` command skills and the authoring skills that are correctly `spec-kit`-gated (FR-003); removing it would change `spec-kit` output (regression) and drop the command skills. Out of scope.

## R3 — How to amend the Feature 204 gate without weakening it (the central design problem)

**Decision**: Refine the validator/test classification from a **two-category** model (gated-by-target-path vs product) to a **three-category** model:

| Category | Recognized by | Lifecycle rule |
|---|---|---|
| **Lifecycle workspace** | target under `.specify/` \| `.agents/` \| `.claude/` \| `CLAUDE.md` \| `AGENTS.md` **AND** source **not** under `template/product-skills/`; plus the generated tree | MUST carry `lifecycle == "spec-kit"` |
| **Framework product-skill** | source under `template/product-skills/` (targets `.agents/skills/fs-gg-*` \| `.claude/skills/fs-gg-*`) | MUST **NOT** carry `lifecycle == "spec-kit"`; MUST be profile-gated |
| **Product** | everything else (base → `./`, samples → `samples/`, ant overlay) | MUST NOT carry `lifecycle == "spec-kit"` |

`scripts/validate-lifecycle-template.fsx` `verifyGatedSources()` and the in-test mirror `gatedSourceAudit()` both add the middle branch (check `source.StartsWith "template/product-skills/"` **before** the target-path test). Count thresholds change: the gated count drops by 12 (was `>= 18`; the 12 framework-skill sources leave the gated bucket and 2 symbology sources join the framework bucket), so the assertions become `gated >= 6`, `framework-skill >= 12`, `product >= 3` (exact numbers fixed against the post-edit `template.json` during implementation).

**Live-run impact (GV-4/GV-5)**: Under `sdd`/`none` the framework `fs-gg-*` skills are now **present** under `.agents/skills/`/`.claude/skills/`. The validator's live `gatedAbsent`/`diff-vs-default` logic must treat those paths as **product** (expected present), so:
- `gated-absent=ok` keeps meaning *the lifecycle-workspace set* (`.specify/`, agent-context tree, blanket `.agents/skills/` non-`fs-gg-*` entries incl. `speckit-*`, constitution) is absent — **not** "all of `.agents` absent."
- `diff-vs-default=gated-only` still holds because the framework skills are present in **both** `spec-kit` (default) and `sdd`, so they fall out of the diff; only the lifecycle-workspace paths differ.

**Rationale**: This *re-specifies* the Feature 204 invariant to the more precise truth ("the **lifecycle workspace** is spec-kit-gated; **framework skills follow the product**") rather than relaxing it. The gate still fails loudly on any mis-gated source — it just knows about three categories now. This keeps Principle V intact (no weakened assertion).

**Alternatives considered**:
- *Leave Feature 204 alone and `xfail`/skip GV-2* — rejected: that **weakens** an assertion to green the build (forbidden by Principle V). The invariant genuinely changed; the gate must encode the new invariant.
- *Move framework skills to a non-`.agents` target so the existing path-based audit still classifies them as product* — rejected: skills MUST live at `.agents/skills/<id>/SKILL.md` / `.claude/skills/<id>/` for agents to find them (the platform convention); relocating them breaks discovery.

## R4 — Making `docs/skillist-reference.md` non-dangling (FR-005 / FR-006)

**Decision**: Gate `docs/skillist-reference.md` emission to the **lifecycle workspace** (emit only under `lifecycle == "spec-kit"`). Today it is an **ungated `copyOnly`** entry on the base source, so it ships on every lifecycle while enumerating the full ~44-id skill **registry** (including `speckit-*` command skills and authoring skills that only ship under `spec-kit`). Under `sdd`/`none` that is a fully dangling table of contents. Suppressing it under `sdd`/`none` satisfies FR-006 (the catalog is absent rather than listing absent skills); the framework skills that *do* ship under `sdd`/`none` remain discoverable directly at `.agents/skills/<id>/`.

**Mechanics**: Remove `docs/skillist-reference.md` from the base source's ungated `copyOnly` list and emit it from a `spec-kit`-gated source (preserving `copyOnly`/no-`sourceName`-substitution so its governance tokens are not rewritten — the same reason Features 062/108 copy it verbatim). Because its target (`docs/…`) is a **product** path, the Feature 204 audit would otherwise flag a `spec-kit`-gated product source as "wrongly gated"; the validator/test gains a **named exception** for `docs/skillist-reference.md` (a lifecycle-coupled reference doc) so the product-vs-gated audit stays honest. The validator already documents this file as the convention-doc that is "out of scope by design" for Feature 204 — this feature brings it in scope and makes its emission lifecycle-coupled.

**Rationale**: The file is a generated artifact of the `spec-kit` authoring lane (regenerated by `fake.sh build -t RefreshSurfaceBaselines`, currency-checked by `TargetMetadataDrift`) enumerating the *full* registry. It is coherent only where that full set + its tooling exist. Minimal footprint: one source moved, one validator exception.

**Alternatives considered**:
- *Regenerate a product-accurate, profile-scoped catalog listing only the vendored `fs-gg-*` skills with product-relative paths* — the "correct" long-term fix, but it requires changing the `RefreshSurfaceBaselines` generator and per-profile content variation; **deferred as a bounded follow-up** (recorded in data-model state notes). It is out of scope for this feature, which is about *emission gating*, not catalog regeneration.
- *Keep it ungated but strip it to only the vendored set* — rejected for this feature for the same generator-change reason; also still wrong under `none` if a profile vendors nothing beyond scene.

## R5 — The orphaned `fs-gg-symbology` skill (FR-007)

**Decision**: **Wire it.** `template/product-skills/fs-gg-symbology/` exists (with `SKILL.md` + `reference.fsx`) but is referenced by **no** source — it is emitted today only incidentally under `spec-kit` via the blanket `.agents/skills/` copy (which contains the repo's authoring `fs-gg-symbology`), and **never** as a product-vendored source. Add a `template/product-skills/fs-gg-symbology/` → `.agents/skills/fs-gg-symbology/` (+ `.claude/skills/`) source pair, profile-gated to the **scene-bearing** profiles (`app || headless-scene || governed || sample-pack` — the same set as `fs-gg-scene`, since symbology is scene/visual-token authoring), with **no** lifecycle clause (consistent with R1).

**Rationale**: The issue lists symbology among the seven skills "built to be vendored." Wiring it makes "skills follow the product" symmetric across `spec-kit`/`sdd`/`none`. Because the scene-profile set spans all four profiles, and symbology already ships under `spec-kit` via the blanket copy, adding the source is **presence-neutral under `spec-kit`** (still present, now via the product-vendored variant overwrite — verified by the GV-3 byte-diff) and **additive under `sdd`/`none`**.

**Alternatives considered**:
- *Record it as intentionally not-vendored* (the conservative default in the spec) — rejected after reading: it is a genuine, content-complete product skill and the issue's intent is to vendor the framework skills with the product. Leaving it orphaned would itself be a (smaller) instance of the bug being fixed.
- *Wire it to `app` only* — rejected: symbology is scene-token authoring, relevant to every scene-bearing profile; scoping to `app` would under-serve `headless-scene`/`sample-pack`.

> **Verify-on-implement**: confirm `template/product-skills/fs-gg-symbology/` content is product-appropriate (no framework-repo-only paths) and that adding it leaves `spec-kit` byte-identical for every profile (GV-3). If the content turns out framework-internal, fall back to the "record as not-vendored" alternative and note it.

### R5 verify-on-implement RESULT (2026-06-30) — **decision reversed to: record as not-vendored**

The verify-on-implement check landed on **branch (b)** of the T005 decision tree (product-appropriate but **NOT byte-equal** to the spec-kit blanket-copy variant), so symbology is **recorded as not-vendored** rather than wired. Evidence:

- `template/product-skills/fs-gg-symbology/SKILL.md` is the full **product** skill (12788 bytes; references product-relative paths `docs/api-surface/Symbology/`, no framework-repo-only paths) — content is product-appropriate.
- The spec-kit blanket-copy variant (repo-root `.agents/skills/fs-gg-symbology/SKILL.md`) is a **506-byte Codex wrapper** pointing at `../../../src/Symbology/skill/SKILL.md`. `diff -r` of the two source dirs: SKILL.md **differs** (only `reference.fsx` is byte-equal).
- The other six wired skills (scene/skiaviewer/elmish/keyboard-input/ui-widgets/testing) already overwrite their blanket-copy variant under spec-kit **today**, so dropping their lifecycle clause is spec-kit-neutral. Symbology has **no** existing overwrite source: adding one would, under `spec-kit`, replace the 506-byte wrapper with the 12788-byte product variant — **changing the spec-kit output and reding GV-3 (FR-004/SC-003)**.
- Reconciling the two to byte-equality would require editing the repo-root authoring wrapper, which itself changes what the blanket copy emits under spec-kit — also a GV-3 regression.

Per the T010 guard ("do NOT add a source that would red GV-3"), the symbology source is **not added**. The data-model symbology rows revert to **absent**; the validator/test emit `symbology: not-vendored`. A product-accurate, byte-reconciled symbology vendoring is a **bounded follow-up** (same class as the R4 catalog-regeneration deferral).

### R1/R2 live smoke RESULT (2026-06-30, T004 — PRE-change, provenance: live, template `FS.GG.UI.Template 0.1.53-preview.1`)

- `dotnet new fs-gg-ui --profile app --lifecycle sdd` → `find … SKILL.md` = **0** (the bug, SC-001 before-state) and `docs/skillist-reference.md` **present** (the FR-006 dangling-catalog bug).
- `dotnet new fs-gg-ui --profile app --lifecycle spec-kit` → 74 SKILL.md, all six framework skills present under `.agents/skills/fs-gg-*` (R2 confirmed: skills already arrive under spec-kit via blanket copy + per-skill overwrite).

## R6 — `xunit` required-skill papercut (§5.2 / FR-008)

**Decision**: **Route, don't fix-in-repo.** A repo-wide search (`grep` for `xunit`, `requiredSkills`, `tasks.yml`) found **no** match in this repository's source tree. The offending `tasks.yml`/`requiredSkills: [..., "xunit"]` is therefore generated **downstream** by the lifecycle/task tooling (SDD CLI / Spec Kit task generation), not by the Rendering template. The plan files a cross-repo request to the owning repo and records the routing; it is corrected here only if implementation surfaces a Rendering-owned source that emits it.

**Rationale**: Fixing what you don't own invites drift; the coordination protocol's job is to route the request to the owner. The papercut is P3 and non-blocking.

**Alternatives considered**: *Search harder / assume it's in a generated `.specify` template* — the search already covered `.specify` and the whole tree; no further in-repo lead exists.

### R6 routing RESULT (2026-06-30, T018/T019)

`grep -rIE 'xunit|requiredSkills|tasks\.yml'` across the repo (`.fs`/`.fsx`/`.json`/`.yml`/`.yaml`, incl. `.specify/` and `template/`) found **no** Rendering-owned source emitting an `xunit` required-skill — the only `xunit` hits are this feature's own spec docs. Confirmed: the offending task metadata is generated **downstream** (SDD CLI / Spec Kit task generation), not by the Rendering template. **FR-008 is satisfied by routing**, not an in-repo edit. The cross-repo request is filed to the owning repo (`FS.GG.SDD`) via the coordination protocol and recorded on the Coordination board; verification of US3's acceptance scenario is owned by that issue (intentional, recorded here — the metadata is generated downstream and this feature does not assert it in-repo).

## R7 — Registry / cross-repo coordination delta (FR-009)

**Decision**: Update `FS-GG/.github` `registry/dependencies.yml` `fs-gg-ui-template.parameters.lifecycle.notes`: the current text *"Gates `.specify/`, constitution, `.agents/`, `.template.config/generated/`"* is refined to clarify that **framework product-skills under `.agents/skills/fs-gg-*` / `.claude/skills/fs-gg-*` are NOT lifecycle-gated — they follow the product profile and emit under every lifecycle**; only the lifecycle *workspace* (`.specify/`, constitution, agent-context tree, `speckit-*` command skills, the blanket authoring-skills copy) is gated. Mark the change **additive / surface-neutral** (no parameter change; `spec-kit` byte-identical). Update the `docs/registry/compatibility.md` projection. If the change is exposed on the feed via a new coherent-set version, the merge/release flow fixes that version (not hard-coded here, per the Feature 204/218 precedent).

**Rationale**: ADR-0001 requires a contract-change to update the registry as part of resolution. The contract owner is `rendering`; the registry file is maintained in `FS-GG/.github`. The board item moves `In review → Done` (per the data-model state chain `Backlog → In progress → In review → Done`) and #30 closes on resolution.

**Alternatives considered**: *Treat it as Tier 2 (no registry update)* — rejected: it changes the contract's observable output and contradicts an existing registry note, so it is Tier 1 by the constitution's classification.
