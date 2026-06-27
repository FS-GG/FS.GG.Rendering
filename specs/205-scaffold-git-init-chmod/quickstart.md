# Quickstart: Validate side-effect-free generation + opt-in init

Runnable validation scenarios proving Feature 205 end-to-end. These map to the spec's Success
Criteria and the contract guarantees. Run from the repo root after the template is installed/packed.

## Prerequisites

- .NET `net10.0` SDK; `git` on PATH for the opt-in scenarios.
- Pack & install the local template (or use the repo's existing pack/validate flow):
  ```bash
  dotnet pack .template.package/FS.GG.UI.Template.fsproj -o ~/.local/share/nuget-local/
  dotnet new install FS.GG.UI.Template --nuget-source ~/.local/share/nuget-local/
  ```
- A scratch directory: `WORK=$(mktemp -d)`

References: option surface & guarantees → [contracts/fs-gg-ui-template-generation.md](./contracts/fs-gg-ui-template-generation.md);
behavior matrix → [data-model.md](./data-model.md).

## Scenario A — Default generation is side-effect-free (SC-001, G2/G3/G4)

For each profile (`app`, `headless-scene`, `governed`, `sample-pack`):

```bash
out="$WORK/A-app"
dotnet new fs-gg-ui --name Demo --profile app -o "$out"    # NO Git flag
```

**Expect:** command returns promptly; `test ! -d "$out/.git"` (no repository); no `git`/`bash`
child process spawned by generation; manual chmod/git instructions printed to console.
**Fail if:** a `.git` directory exists, or generation blocks/hangs.

## Scenario B — No defensive flag needed in CI (SC-002, G5)

Run Scenario A inside a headless/CI context **without** `--skipGitInit` (which no longer exists)
and confirm completion. Then confirm the repo's own tooling no longer threads the flag:

```bash
grep -rn -- "--skipGitInit" scripts/ tests/ ; echo "exit=$?"   # expect: no matches (exit=1)
grep -n "proc.Kill" scripts/validate-lifecycle-template.fsx ; echo "exit=$?"  # expect: removed/relaxed
```

**Expect:** zero `--skipGitInit` occurrences; the 300 s wait/`Kill` defensive loop removed or
reduced to a short sanity timeout.

## Scenario C — Explicit opt-in reaches the initial repository state (SC-004, C1)

```bash
out="$WORK/C-optin"
dotnet new fs-gg-ui --name Demo --profile app --initGit true -o "$out"
( cd "$out" && git rev-parse --is-inside-work-tree && git log --oneline -1 )
test -x "$out/fake.sh"            # Unix: generated script is executable
```

**Expect:** repository initialized; one `[Spec Kit] Initial commit`; `fake.sh` executable (Unix).
On Windows the commit exists; the executable-bit check is N/A (parity, FR-011).

## Scenario D — No opt-in ⇒ side-effect-free (SC-004 second half)

```bash
out="$WORK/D-noopt"
dotnet new fs-gg-ui --name Demo --profile app -o "$out"
test ! -d "$out/.git"
```

**Expect:** matches Scenario A — no repo, no process.

## Scenario E — Generation inside an existing repo never nests (SC-006, C2)

```bash
parent="$WORK/E-parent"; mkdir -p "$parent" && ( cd "$parent" && git init -q )
dotnet new fs-gg-ui --name Demo --profile app --initGit true -o "$parent/child"
# the only repo is the parent; no nested .git under child
test ! -d "$parent/child/.git"
( cd "$parent" && git status --porcelain >/dev/null )   # parent intact, no surprise commit
```

**Expect:** no nested `.git`; surrounding repo untouched. Repeat with the scaffold path for US2-3.

## Scenario F — git absent is non-fatal (C3, FR-006)

```bash
out="$WORK/F-nogit"
PATH="/usr/bin/nonexistent-only" dotnet new fs-gg-ui --name Demo --profile app --initGit true -o "$out" || true
```

**Expect:** generation succeeds; clear "git not found; skipped repository initialization" message;
no half-initialized repo; chmod still applied where applicable.

## Scenario G — Emitted file set unchanged (SC-005, G1)

Compare the emitted tree fingerprint for each profile against the pre-feature baseline (the
validation scripts already expose `treeFingerprint`). The Feature-205 test asserts equality.

**Expect:** identical file set/fingerprint per profile/lifecycle/designSystem combination — the
change is behavioral only.

## Scenario H — Scaffold path reaches the same end state (SC-003, S1/S2) — cross-repo

Driven from the SDD repo, not asserted here:

```bash
fsgg-sdd scaffold --provider rendering --name Demo --profile app -o "$WORK/H-scaffold"
( cd "$WORK/H-scaffold" && git log --oneline -1 ) && test -x "$WORK/H-scaffold/fake.sh"
```

**Expect:** initialized repo + initial commit + executable scripts, achieved by the scaffold path's
own explicit steps (not template post-actions). Tracked via the `fs-gg-ui-template` contract; see
research R5.

## Pass criteria

| Scenario | Proves |
|---|---|
| A, D | SC-001 — side-effect-free default |
| B | SC-002 — no defensive flag / no hang |
| C | SC-004 — opt-in end state |
| E | SC-006 — no nested repo |
| F | FR-006 — git-absent non-fatal |
| G | SC-005 — emitted files unchanged |
| H | SC-003 — scaffold-path parity (cross-repo) |
