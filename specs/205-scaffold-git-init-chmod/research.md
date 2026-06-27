# Phase 0 Research: Move git-init / chmod out of fs-gg-ui template post-actions

All NEEDS CLARIFICATION from Technical Context are resolved below. Each item: **Decision /
Rationale / Alternatives considered.**

## R1 — Option shape: opt-out → opt-in (rename `skipGitInit` → `initGit`)

**Decision.** Remove the `skipGitInit` parameter (bool, default `false`, meaning "auto-init unless
set"). Add a single opt-in parameter **`initGit`** (bool, default `false`) that, when `true`, runs
the convenience steps for a direct caller: `chmod +x` the emitted shell scripts (Unix only) **and**
`git init` + `git add .` + initial commit (unless already inside a repo). Default `false` ⇒ no
process, no repo.

**Rationale.** FR-001/FR-002 require generation to spawn no process and create no repo by default;
a flag literally named "skip" defaulting to "don't skip" cannot express a side-effect-free default
without inverting its own meaning. The spec assumption ("reshapes the default and the option's
meaning so the default is side-effect-free and initialization is explicit") points to repurposing
the one Git toggle into an opt-in rather than adding an unrelated mechanism. A single boolean keeps
the option surface self-describing (FR-012) and covers both chmod and git (the spec's "single
explicit opt-in" in US3 explicitly bundles repo init + initial commit + executable scripts).

**Alternatives considered.**
- *Keep `skipGitInit` but default it to `true`.* Rejected: a default-`true` "skip" flag is a
  confusing double-negative and still advertises auto-init as the intent; FR-012 (self-describing)
  fails.
- *Keep `skipGitInit` as a deprecated alias alongside `initGit`.* Rejected for now: adds a
  permanent footgun and two-sources-of-truth for the same behavior; FR-010 already mandates
  updating the only in-repo callers (the two validation `.fsx`), so no external compatibility debt
  remains. A clean rename is simpler (Principle III). Migration is documented in the contract.
- *Two separate options (`initGit` + `makeScriptsExecutable`).* Rejected: US3 asks for **one**
  explicit opt-in; chmod is a harmless no-op where it's not needed (FR-007), so bundling it under
  the same opt-in is simplest and matches today's coupling.

**Naming note.** `initGit` chosen over `gitInit`/`initializeGit` for brevity + camelCase parity with
the removed `skipGitInit`. Cheap to rename before merge if cross-repo review prefers another token;
recorded in the contract as the canonical option name.

## R2 — Removing the auto-run post-actions vs. the moved capability

**Decision.** Delete all three current `postActions` (Unix init+chmod, Unix chmod-only, Windows
init). Replace with: (a) an **`initGit`-gated** Unix post-action (chmod + git) and an
**`initGit`-gated** Windows post-action (git), reusing today's hardened argument strings
(git-presence check, `--is-inside-work-tree` guard, `--allow-empty`, `continueOnError: true`); and
(b) an **always-on instructions-only** post-action that prints the manual chmod/git steps so a
default caller still learns how to do it by hand.

**Rationale.** Reusing the existing guarded command strings preserves the already-correct
edge-case handling (FR-005 existing-repo detection, FR-006 git-absent non-fatal skip, FR-007
no-op when no scripts) — we change *when* they run (only on opt-in), not *what* they do.
`continueOnError: true` stays so even the opt-in path can't fail a generation.

**Alternatives considered.**
- *Delete post-actions entirely and rely only on README/docs for the opt-in.* Rejected: US3/FR-003
  require the opt-in to actually perform the steps via a single template option, not just document
  them.

## R3 — Always surfacing manual instructions (FR-009)

**Decision.** Add a post-action whose condition is always true, with `manualInstructions` describing
the chmod + git steps and **no auto-run executable**, so `dotnet new` prints the steps on every
generation regardless of `initGit`. Additionally keep the manual steps in the **generated
product's `README`/docs** so they survive outside the console transcript.

**Rationale.** `dotnet new` surfaces a post-action's `manualInstructions` to the console; the
gated init post-actions only match when `initGit true`, so without an always-on instructions action
a default caller would see nothing. Documenting in the generated tree (durable) + the console
(immediate) covers both "inspect the generated output" and "inspect the template's guidance"
(US3 scenario 3 / FR-009).

**Verification owed in implementation.** Confirm on the real `net10.0` template engine that an
instructions-only post-action (manualInstructions present, no recognized run processor +
`continueOnError: true`) prints cleanly without being reported as a hard failure. If the engine
treats an unrunnable action as an error line, fall back to surfacing the manual steps **only** via
the generated README/docs and a one-line post-generation message, and drop the always-on action.
This is the one engine-behavior unknown; the early live smoke in `/speckit-tasks` resolves it.

## R4 — Updating in-repo callers and removing the defensive scaffolding (FR-010)

**Decision.** In `scripts/validate-lifecycle-template.fsx` and
`scripts/validate-design-system-template.fsx`: drop every `--skipGitInit true` argument and remove
(or sharply relax) the 300-second `while … proc.Kill` stabilization loop, since default generation
now exits promptly on its own. Update `tests/Package.Tests/*` that assumed the auto-init default.

**Rationale.** SC-002 requires that no defensive opt-out flag remains anywhere in the repo's own
tooling/tests and that generation completes without hanging. The kill-loop exists *only* to defend
against the spinning post-action (`validate-lifecycle-template.fsx:222–230`); once generation
spawns no process it is dead code. Removing it is the measurable proof the risk is gone.

**Alternatives considered.**
- *Leave the kill-loop as belt-and-suspenders.* Rejected: SC-002 is explicit that the defensive
  machinery must be removable; leaving it would mask a regression where a process re-enters
  generation.

## R5 — Cross-repo boundary: who owns the scaffold-path execution

**Decision.** This rendering-repo feature delivers: (1) the side-effect-free template, (2) the
`initGit` opt-in, and (3) a **published contract** (`contracts/fs-gg-ui-template-generation.md`)
stating that generation is side-effect-free and that repo-init + chmod are the *scaffold path's*
responsibility. It does **not** implement `fsgg-sdd scaffold`'s init step. Coordinate via the
`fs-gg-ui-template` contract entry in the cross-repo registry and, if the scaffold path's
expectations shift, file/track under `cross-repo-coordination`.

**Rationale.** ADR-0002 puts lifecycle/orchestration ownership on the scaffold path; the spec
(Assumptions) scopes the SDD-side execution out of this feature. US2's rendering-repo deliverable
is precisely "make the template stop owning these effects and define the contract the scaffold path
fulfills." Verifying the full scaffold-path end state (US2 acceptance) is a cross-repo integration
that the contract enables but that this repo cannot assert alone.

**Action.** Add/refresh a `fs-gg-ui-template` compatibility-registry note recording the behavioral
break (auto-init removed; `skipGitInit` gone; `initGit` added) so SDD-side consumers pin/adapt.
Flag for the merge/coordination step — not a code task in this repo beyond the contract doc.

## R6 — No emitted-file change (SC-005) guard

**Decision.** Treat "zero file-content diff for emitted products" as an invariant verified by the
Feature-205 test comparing the emitted file *set/fingerprint* before vs. after the change for each
profile (the validation scripts already compute `treeFingerprint`). Post-actions and symbols are
generation *behavior*, not `sources`, so removing/altering them cannot change emitted files — the
test makes that explicit and regression-proof.

**Rationale.** SC-005/FR-008 are central guarantees; the existing `treeFingerprint` helper makes
this cheap to assert deterministically without a window/GL surface.

## Resolved unknowns summary

| Unknown | Resolution |
|---|---|
| Option name & polarity | `initGit` bool, default `false`, opt-in (R1) |
| `skipGitInit` fate | Removed; clean rename, migration documented (R1) |
| chmod vs git split | Bundled under the one `initGit` opt-in (R1) |
| How manual steps stay visible | Always-on instructions post-action + generated README/docs (R3) |
| Defensive flag / kill-loop | Removed from both validation scripts (R4) |
| Scaffold-path execution | Out of scope here; published contract + registry note (R5) |
| File-set invariance | Fingerprint test per profile (R6) |
