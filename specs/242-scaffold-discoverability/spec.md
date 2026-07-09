# Feature Specification: Scaffold discoverability sharpening

**Feature Branch**: `242-scaffold-discoverability`

**Created**: 2026-07-04

**Status**: Shipped

**Input**: Roadmap item FS-GG/FS.GG.Rendering#75 (epic FS-GG/.github#165); Space Invaders consumer feedback §2.2/§2.3. Two additive discoverability asks in the fs-gg-ui generated-product template.

## Context

A full SDD-lifecycle consumer build of the Space Invaders TestSpec shipped ready, but ~60–70% of pre-implementation time was lost to two discoverability gaps. The scaffold *seam model* itself — the durable governance spine that survives a full game swap — is the strongest part of the template and is **not** changing. This feature only sharpens two edges the consumer stumbled on:

- **§2.2 — re-point work is prose, not a checklist.** `docs/scaffold-map.md` names `LayoutEvidence.fs` / `EvidenceCommands.fs` as "documented field re-points", but the consumer discovered *which* functions actually read a `Model` field (e.g. `mapKey`, `tick`, the LayoutEvidence active-item / HUD-text readers) only by swapping the `Model` and reading the resulting compiler errors — "compiler-error archaeology".
- **§2.3 — build-target semantics are load-bearing and hidden.** `Dev` is a completion-marker log-writer (not a real compile), `Test` is the first real `dotnet test`, and `Verify` embeds a merge-gate audit that hard-blocks until every task is `[X]`. These facts live only in `docs/product.md`; a developer running the build sees no hint, and the consumer worked around a "green `Dev` that never catches compile errors" by reaching for `dotnet build`/`dotnet test` directly.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Precise swap checklist for a model rewrite (Priority: P1)

A developer who has scaffolded a product and wants to replace the starter game/UI model opens a single generated document that enumerates exactly which files and which symbols within them read a `Model` field, so they know their complete re-point to-do list *before* touching code — without inferring it from compiler errors.

**Why this priority**: This is the highest-leverage fix per the consumer report; the wasted pre-implementation time was concentrated here, and it directly de-risks the swap the template exists to support.

**Independent Test**: Scaffold a product, open `SWAP-CHECKLIST.md`, and confirm every symbol it lists that reads a model field corresponds to a real symbol in the generated tree, and that no non-spine symbol that reads a model field is omitted — verifiable by cross-checking the checklist against the generated source without running the build.

**Acceptance Scenarios**:

1. **Given** a freshly scaffolded product, **When** the developer opens the generated `SWAP-CHECKLIST.md`, **Then** it lists the replaceable files to rewrite wholesale (`Model.fs`, `View.fs`, the replaceable behavior test) and, separately, the durable must-re-point files with the specific model-field-reading symbols in each.
2. **Given** the generated checklist, **When** its entries are cross-checked against the generated source, **Then** every listed model-field-reading symbol exists in the tree and every such symbol in the non-spine files is listed (no phantom entries, no omissions).
3. **Given** the checklist, **When** the developer reads it, **Then** it distinguishes "rewrite wholesale" from "keep the file and its evidence tokens, re-point the model-field reads" and points back to `docs/scaffold-map.md` for the durable/replaceable rationale.
4. **Given** a product scaffolded under any supported profile, **When** the checklist is generated, **Then** its symbol list is correct for that profile's actual starter model (the game, app-default, and governed/headless starters read different fields).

---

### User Story 2 - Build-target semantics visible at the build entry (Priority: P2)

A developer running the generated build asks for help (or runs it with no/invalid target) and sees a banner that states what `Dev`, `Test`, and `Verify` actually do — including that `Dev` does not compile and that `Verify`'s audit hard-blocks until the feature is complete — so they never mistake a green `Dev` for a passing compile.

**Why this priority**: Prevents a concrete, repeatable confusion the consumer hit, but is lower-leverage than the swap checklist and independent of it.

**Independent Test**: Run the generated build entry with a help request and confirm the banner names each of the three targets with its real semantics; verifiable without a full compile.

**Acceptance Scenarios**:

1. **Given** a scaffolded product, **When** the developer requests build help (e.g. `--help`/`-h`), **Then** a banner lists the available targets and, for `Dev`/`Test`/`Verify`, states the load-bearing semantics: `Dev` writes a completion marker and does **not** compile; `Test` is the first real compile + `dotnet test`; `Verify` runs the merge-gate audit that hard-blocks until every task is `[X]`, then the tests.
2. **Given** the banner text, **When** it is compared against `docs/product.md`, **Then** the two agree on the target semantics (single source of truth, no drift).
3. **Given** a help request, **When** the banner is shown, **Then** requesting help does not run any target, does not write a completion-marker log, and exits successfully.

---

### Edge Cases

- A developer's swap is purely additive (adds a model field without changing the fields the durable files read): the checklist must make clear the durable re-point files can be left untouched in that case, matching the existing scaffold-map guidance.
- The checklist must not become a governance trap: it is developer guidance, not a must-survive evidence artifact whose specific wording a scan pins (that would make an ordinary swap fail the gate). Its *presence and discoverability* may be asserted; its per-symbol body is advisory.
- Help output must not be mistaken for a target run by any log-scanning readiness check (no `readiness/logs/<target>.txt` side effect from asking for help).
- An unknown/invalid target should guide the developer toward the help banner rather than failing silently.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The generated product MUST ship a `SWAP-CHECKLIST.md` that enumerates, as an actionable to-do list, the files a model swap touches — separating "rewrite wholesale" (replaceable) files from "keep + re-point model-field reads" (durable) files.
- **FR-002**: For each durable must-re-point file, the checklist MUST name the specific symbols that read a `Model` field (the re-point targets), so the developer does not have to discover them from compiler errors.
- **FR-003**: The checklist content MUST be correct for the profile the product was scaffolded under (game / app-default / governed-or-headless), reflecting that profile's actual starter model fields and reader symbols.
- **FR-004**: The checklist MUST cross-reference `docs/scaffold-map.md` for the durable-vs-replaceable rationale and MUST preserve the existing rule that the must-survive evidence tokens stay present across a swap.
- **FR-005**: The checklist MUST NOT introduce a new governance gate that a legitimate model swap would fail; any automated check over it MUST be limited to its presence/discoverability, not the exact per-symbol prose.
- **FR-006**: The generated build entry MUST expose a help request that prints a banner listing the build targets. The shell wrapper (`build.sh`) MUST accept `--help`/`-h`/`help`; the F# script entry (`build.fsx`, and `fake.sh` which forwards to it) MUST accept the bare token `help` — because `dotnet fsi` reserves `--help`/`-h` for itself and they never reach the script (confirmed by the T004 live check).
- **FR-007**: The help banner MUST state the load-bearing semantics of `Dev` (completion-marker log-writer; does not compile), `Test` (first real compile + `dotnet test`), and `Verify` (merge-gate audit that hard-blocks until every task is `[X]`, then tests).
- **FR-008**: Requesting help MUST NOT execute any build target and MUST NOT write a target completion-marker log; it MUST exit successfully.
- **FR-009**: The banner semantics MUST agree with `docs/product.md`; the two MUST NOT drift (one is derived from or checked against the other).
- **FR-010**: All changes MUST be additive with respect to the scaffold seam model, the durable governance spine, and versioned cross-repo contracts — no change to `scaffold-provider`/`scaffold-provenance`/`fs-gg-ui-template` surfaces, the six scanned scaffold files' order, or the must-survive evidence-token set.

### Key Entities

- **SWAP-CHECKLIST.md**: A generated, developer-facing document. Attributes: per-file entries; per-file classification (rewrite-wholesale vs re-point); for re-point files, the list of model-field-reading symbols; a pointer to `docs/scaffold-map.md`. Profile-conditioned content.
- **Build help banner**: Text emitted by the generated build entry on a help request. Attributes: target list; the `Dev`/`Test`/`Verify` semantics; no side effects.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A developer preparing a model swap can produce a complete list of files-and-symbols to change from the generated `SWAP-CHECKLIST.md` alone, without compiling and reading errors, for every supported profile.
- **SC-002**: Every symbol the checklist names exists in the generated tree (zero phantoms), and every durable re-point *reader function* in the known reader set appears in the checklist — verified automatically. (Full "zero omissions" over arbitrary future edits is bounded to the known reader set, since a complete proof would require parsing F#; see the honesty caveat in `research.md`.)
- **SC-003**: A developer running the generated build with a help request learns, in one screen, that `Dev` does not compile and that `Verify`'s audit blocks until the feature is complete — without opening `docs/product.md`.
- **SC-004**: The build-target semantics stated at the build entry and in `docs/product.md` are verified to agree, with no drift permitted to merge.
- **SC-005**: A legitimate additive model swap continues to pass the durable governance gate unchanged (the new artifacts add no new failing scan for a normal swap).

## Assumptions

- The generated product's build entry is a script-based FAKE-style runner that already parses a target argument; adding a help path is an additive branch, not a new build system.
- The starter models per profile are stable enough that a per-profile authored checklist (conditioned at template-authoring time, like the other profile-conditioned files) is the pragmatic vehicle; the checklist is emitted resolved for the scaffolded profile, carrying no template conditionals in the generated tree.
- "Non-spine files that read a Model field" are, per `docs/scaffold-map.md`, the replaceable files (`Model.fs`, `View.fs`, the replaceable behavior test) plus the durable must-re-point files (`LayoutEvidence.fs`, `EvidenceCommands.fs`); the durable model-agnostic spine (`Program.fs`, `WindowOptions.fs`, `GovernanceTests.fs`) reads no model field and is out of scope for the re-point list.
- `docs/product.md` remains the prose home for the full build narrative; the banner is a concise, checked projection of its target semantics, not a replacement.
