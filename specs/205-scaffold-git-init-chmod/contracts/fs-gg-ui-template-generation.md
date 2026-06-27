# Contract: `fs-gg-ui` template generation behavior (Git init / chmod)

**Contract name (cross-repo registry):** `fs-gg-ui-template`
**Owner repo:** FS.GG.Rendering (template + manifest)
**Consumers:** SDD scaffold path (`fsgg-sdd scaffold --provider rendering`); direct
`dotnet new fs-gg-ui` callers; CI/validation tooling.
**Change class:** Tier 1 (contracted) — option surface + observable behavior change.
**Status:** Accepted (Feature 205) — shipped `.template.config/template.json` matches §2–§4; live
behavioral evidence in `specs/205-scaffold-git-init-chmod/readiness/smoke-after.md`.

## 1. Generation is side-effect-free by default

`dotnet new fs-gg-ui` (any `--profile`, `--lifecycle`, `--designSystem`), with **no** Git-related
option, MUST:

- emit the same file set as before this feature (no file-content diff) — **GUARANTEE G1**;
- spawn **zero** external processes as a side effect of generation — **G2**;
- create **zero** Git repositories and **zero** commits — **G3**;
- complete promptly (no process to block/hang on) — **G4**;
- require **no** defensive opt-out flag to be CI-safe — **G5**.

## 2. Option surface

| Option | Type | Default | Meaning |
|---|---|---|---|
| `initGit` | bool | `false` | When `true`, after emitting files: mark generated shell scripts executable (Unix; no-op elsewhere/when none) **and** initialize a Git repository with one `[Spec Kit] Initial commit`, unless already inside a repository. |

- **Removed:** `skipGitInit` (previously bool, default `false`). It no longer exists. Callers that
  passed `--skipGitInit true` MUST drop the flag (default is now side-effect-free).
- `initGit` MUST be discoverable and self-describing via `dotnet new fs-gg-ui --help` (FR-012).

## 3. Opt-in behavior (`--initGit true`, direct caller)

When set, generation MUST reach the **initial repository state**:

- **C1** Not inside an existing repo + git present ⇒ repo initialized, one initial commit,
  generated scripts executable.
- **C2** Inside an existing repo ⇒ **no nested repo**, no commit into the parent; chmod still
  applied. (existing-repo detection via `git rev-parse --is-inside-work-tree`).
- **C3** git absent ⇒ **non-fatal** skip with a clear message; chmod still applied; generation
  still succeeds; **no half-initialized repo** left behind.
- **C4** No shell scripts emitted (e.g. non-`spec-kit` lifecycle) ⇒ chmod is a **no-op**, never an
  error.
- **C5** Host has no executable bit ⇒ chmod step is unnecessary; same end state otherwise (parity).
- The opt-in post-action MUST keep `continueOnError: true` — the opt-in MUST NOT be able to fail a
  generation.
- **C6 (non-interactive)** The opt-in runs a Run post-action, which `dotnet new` guards with the
  allow-scripts confirmation prompt. A non-interactive/headless caller MUST pass `--allow-scripts
  yes` alongside `--initGit true`; otherwise the prompt blocks. The **default** path fires no
  post-action and needs no such flag (this is what makes generation CI-safe, G4/G5).

## 4. Manual instructions (always available)

Regardless of `initGit`, the template MUST surface manual instructions for performing chmod + git
init by hand:

- **durably in the generated product's `README.md`/docs** (the primary, always-present surface), **and**
- in the opt-in post-action's `manualInstructions` (shown when `--initGit true` is used), **and**
- in the package `README.md` ("Manual setup").

**Engine constraint (resolved live, research R3 / T005).** An *always-on* instructions-only
post-action is **not** used: the `net10.0` template engine rejects a post-action with no `actionId`
(`CONFIG0202`), and a non-running `actionId` would surface as a failed/unrun action line. A truly
side-effect-free default (G2) also forbids spawning an `echo` process. The manual instructions are
therefore guaranteed by the generated tree (durable) rather than an always-on console line.

Exact wording mirrors today's `manualInstructions`:
`find . -type f \( -name "*.sh" -o -name "fake.sh" \) -exec chmod +x {} +` then
`git init && git add . && git commit -m "[Spec Kit] Initial commit"` (skip if already in a repo).

## 5. Scaffold-path obligation (consumer-side, SDD repo)

This contract states the division of responsibility; the rendering repo does **not** implement it:

- **S1** The scaffold path (`fsgg-sdd scaffold --provider rendering`) OWNS repository initialization
  and making scripts executable for composed products, performed as **explicit, observable** steps
  **after** template instantiation — not via template post-actions (ADR-0002).
- **S2** The scaffold path MUST honor the same existing-repo / git-absent / no-scripts safeguards
  (C2–C4) so an orchestrated product reaches the initial repository state in 100% of
  not-inside-a-repo cases (SC-003) and never nests a repo (SC-006).
- **S3** The scaffold path MUST NOT rely on the template auto-initializing; it MUST assume
  side-effect-free generation (G2/G3).

## 6. Migration notes for consumers

- **In-repo tooling/tests:** remove every `--skipGitInit true` argument and any
  generation-hang defenses (e.g. the wait/`Kill` loop in
  `scripts/validate-lifecycle-template.fsx`). Generation no longer spawns a process to defend
  against (SC-002).
- **SDD scaffold path:** stop expecting the template to init the repo; perform init/chmod as own
  steps (S1). Record the behavioral break in the `fs-gg-ui-template` compatibility registry so
  versioned consumers pin/adapt.
- **External direct callers:** if you previously relied on auto-init, pass `--initGit true`; if you
  previously passed `--skipGitInit true`, just remove it.

## 7. Verification (acceptance ↔ guarantee map)

| Guarantee / clause | Spec acceptance | Success criterion |
|---|---|---|
| G1 (file-set unchanged) | US1/FR-008 | SC-005 |
| G2/G3/G4 (no process/repo, prompt) | US1-1, US1-3 | SC-001 |
| G5 (no defensive flag) | US1-2 | SC-002 |
| C1 (opt-in end state) | US3-1 | SC-004 |
| C2 (existing repo safe) | US3 / Edge | SC-006 |
| C3 (git-absent non-fatal) | Edge | — |
| C4/C5 (no-op chmod, parity) | Edge / FR-007/FR-011 | — |
| §4 (manual instructions) | US3-3/FR-009 | — |
| S1–S2 (scaffold path) | US2-1..3 | SC-003 |
