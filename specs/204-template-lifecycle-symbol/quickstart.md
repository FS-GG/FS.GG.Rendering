# Quickstart: Validate the `lifecycle` template option

Runnable validation scenarios proving the feature end-to-end. See
[contracts/lifecycle-symbol.contract.md](./contracts/lifecycle-symbol.contract.md) for the
option surface and [data-model.md](./data-model.md) for the gated set.

## Prerequisites

- .NET SDK (the repo's `net10.0` toolchain) with `dotnet new`.
- The `fs-gg-ui` template installed from this working tree:
  ```bash
  dotnet new install /home/developer/projects/FS.GG.Rendering
  ```
  (Re-run after editing `.template.config/template.json`; `dotnet new uninstall` lists the
  current install path.)
- `git` (used only to produce the pre-feature reference tree for the byte-identical check).

## Scenario 1 — Default is byte-identical (US1 / SC-001 / FR-002)

```bash
out=$(mktemp -d)
dotnet new fs-gg-ui --name Demo --profile app           -o "$out/default"
dotnet new fs-gg-ui --name Demo --profile app --lifecycle spec-kit -o "$out/explicit"
diff -r "$out/default" "$out/explicit"        # expect: no output
```
Expected: empty diff (explicit `spec-kit` == no value). For the full non-regression proof,
compare against a scaffold from the pre-feature template (e.g. `git stash` the change or
scaffold from a clean checkout) and confirm an empty recursive diff for each profile.

## Scenario 2 — `sdd` suppresses the gated set, keeps the product (US2 / FR-004 / SC-003)

```bash
dotnet new fs-gg-ui --name Demo --profile app --lifecycle sdd -o "$out/sdd"
# gated set ABSENT:
test ! -e "$out/sdd/.specify"        && echo "ok: no .specify"
test ! -e "$out/sdd/.claude"         && echo "ok: no .claude"
test ! -e "$out/sdd/.agents"         && echo "ok: no .agents"
test ! -e "$out/sdd/CLAUDE.md"       && echo "ok: no generated CLAUDE.md"
test ! -e "$out/sdd/AGENTS.md"       && echo "ok: no AGENTS.md"
# product PRESENT + builds:
test -e  "$out/sdd/Demo.fsproj" -o -d "$out/sdd/src" && echo "ok: product present"
( cd "$out/sdd" && dotnet build )    # expect: build succeeds
```
Expected: every gated artifact absent; the product is present and builds. No emitted file
references a suppressed path (the dangling-reference check, research CC-1).

## Scenario 3 — `none` suppresses the gated set (US3 / FR-004)

```bash
dotnet new fs-gg-ui --name Demo --profile app --lifecycle none -o "$out/none"
diff -r "$out/sdd" "$out/none"       # expect: no output (same template-level suppression)
```
Expected: `none` and `sdd` produce the identical template-level file set (research CC-3).

## Scenario 4 — Unknown value fails fast (FR-006 / SC-004)

```bash
dotnet new fs-gg-ui --name Demo --lifecycle bogus -o "$out/bogus"; echo "exit=$?"
```
Expected: non-zero exit, a clear "invalid value" error, and no `"$out/bogus"` tree.

## Scenario 5 — All combinations generate; composition holds (SC-004 / FR-007 / FR-008)

```bash
for lc in spec-kit sdd none; do
  for p in app headless-scene governed sample-pack; do
    dotnet new fs-gg-ui --name Demo --profile "$p" --lifecycle "$lc" \
      --designSystem ant -o "$out/$lc-$p-ant" && echo "ok: $lc/$p/ant"
  done
done
```
Expected: all 12 combinations generate; the ant overlay (ungated product content) is present
in every case, proving lifecycle composes with `designSystem` without override.

## Automated gates (the regenerated proof)

- **Always-on deterministic gate** (no env flag, GL-free):
  ```bash
  dotnet test tests/Package.Tests --filter Feature204
  ```
  Self-provisions (env-free `--emit-report` path) and asserts the **gitignored** validation
  report under `readiness/` — regenerated, never committed (covered-values == enumerated
  `lifecycle` choices; default byte-identical; gated-absent under `sdd`/`none`; product
  present; unknown rejected; `result: pass`).
- **Env-gated live regenerator** (real `dotnet new` per combo; writes the report):
  ```bash
  FS_GG_RUN_LIFECYCLE_VALIDATION=1 dotnet fsi scripts/validate-lifecycle-template.fsx
  ```

## Done when

- Scenarios 1–5 behave as described against the working-tree template.
- `Feature204LifecycleTemplateTests` is green and the existing profile/template suites pass
  unmodified (SC-002).
