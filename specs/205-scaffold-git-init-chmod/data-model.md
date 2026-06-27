# Phase 1 Data Model: Template option surface & generation-behavior states

This feature has no runtime data entities; its "data model" is the **template option surface** and
the **generation-behavior state machine** the options drive. Entities below map to the spec's Key
Entities.

## Entity: Template option surface (`fs-gg-ui` parameters, Git-related)

| Option | Type | Default | Before this feature | After this feature |
|---|---|---|---|---|
| `skipGitInit` | bool | `false` | Opt-OUT: `false` ⇒ auto chmod+git; `true` ⇒ chmod only | **Removed** |
| `initGit` | bool | `false` | — (did not exist) | Opt-IN: `false` ⇒ nothing; `true` ⇒ chmod+git (existing-repo-safe) |

**Validation rules.**
- `initGit` is self-describing in `--help` (FR-012): description states it initializes a repo +
  initial commit and marks generated scripts executable, and that it is unnecessary under the
  scaffold path.
- Default `false` MUST yield zero spawned processes and zero repos (FR-001/FR-002).
- Removing `skipGitInit` MUST NOT change any emitted file (FR-008); it only removes behavior.

## Entity: Generation-time side effect (post-action)

Post-actions are generation *behavior*, not emitted files. State after this feature:

| Post-action | Condition | Runs | Purpose |
|---|---|---|---|
| Unix init (new) | `initGit && OS != Windows_NT` | `chmod +x` scripts; git-presence check; `--is-inside-work-tree` guard; `git init/add/commit --allow-empty` | Opt-in convenience (Unix) |
| Windows init (new) | `initGit && OS == Windows_NT` | PowerShell git-presence check + init guard + commit | Opt-in convenience (Windows) |
| Manual instructions (new) | always | *nothing* (instructions-only) | Always surface manual chmod/git steps (FR-009) |
| ~~Unix init+chmod (default)~~ | ~~`!skipGitInit && …`~~ | — | **Deleted** (auto-run removed) |
| ~~Unix chmod-only~~ | ~~`skipGitInit && …`~~ | — | **Deleted** |
| ~~Windows init (default)~~ | ~~`!skipGitInit && …`~~ | — | **Deleted** |

All retained run-post-actions keep `continueOnError: true` and the hardened guards carried over
verbatim from today's strings.

## Entity: Behavior matrix (caller × platform × repo context)

| Caller path | `initGit` | Inside existing repo? | git present? | Outcome |
|---|---|---|---|---|
| Direct `dotnet new` (default) | `false` | any | any | No process, no repo, no chmod; manual instructions printed (US1; SC-001) |
| Direct `dotnet new` (opt-in) | `true` | No | yes | Repo initialized + initial commit; scripts executable (Unix) (US3-1; SC-004) |
| Direct `dotnet new` (opt-in) | `true` | Yes | yes | No nested repo; surrounding repo untouched; chmod still applied (Edge; SC-006) |
| Direct `dotnet new` (opt-in) | `true` | No | **no** | Non-fatal skip message; chmod applied; generation succeeds (FR-006) |
| Direct `dotnet new` (opt-in) | `true` | any | any | No shell scripts emitted ⇒ chmod is a harmless no-op (FR-007) |
| Scaffold path `fsgg-sdd scaffold --provider rendering` | (n/a — owns its own step) | No | yes | Scaffold orchestrator initializes repo + commit + chmod as explicit steps (US2; SC-003) |
| Scaffold path | — | Yes | — | No nested repo; surrounding repo intact (US2-3; SC-006) |

**Cross-platform parity (FR-011):** side-effect-free default on every host; the opt-in/scaffold
path reaches the same end state per platform (chmod is simply unnecessary where there is no
executable bit — a no-op, not a divergence).

## Entity: Standalone opt-in

The single `initGit true` flag; the *only* supported direct-caller mechanism to reproduce the
pre-feature convenience. Documented in `.template.package/README.md` Options table and surfaced via
`--help`.

## Entity: Initial repository state (end state)

For an orchestrated (scaffold-path) or opted-in product not inside an existing repo: an initialized
Git repository with one `[Spec Kit] Initial commit` and executable generated shell scripts. Now
produced **outside** template post-actions (scaffold path) or via the **explicit** opt-in — never
as a hidden default effect.

## State transitions (generation)

```
generate(profile, lifecycle, designSystem, initGit=false)         ← DEFAULT
   → emit files (unchanged set)
   → [always] print manual instructions
   → DONE (no process, no repo)                                    SC-001/SC-005

generate(…, initGit=true) where not-inside-repo, git present
   → emit files
   → chmod +x emitted *.sh / fake.sh        (Unix; no-op on Windows / when none)
   → git init → git add . → git commit --allow-empty
   → DONE (initialized repo + initial commit)                      SC-004

generate(…, initGit=true) where inside-repo OR git-absent
   → emit files
   → chmod (where applicable)
   → skip git (guarded / non-fatal message)
   → DONE (surrounding repo intact; no half-init)                  FR-005/FR-006
```
