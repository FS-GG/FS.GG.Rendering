# Quickstart / Validation: Compile the docs instead of parsing them

How to prove the harness works end to end. The **green + red** pair (steps 2–3) is the early live proof the
plan requires before any old machinery is deleted.

## Prerequisites

- .NET SDK `10.0.x`.
- The pinned packages (`FS.GG.UI.*` at `$(FsGgUiVersion)` from `Directory.Build.local.props`) present on the
  local nupkg feed. If a release is pending and the pin is not yet on the feed, the release-pending waiver
  applies (the harness must no-op, not fail) — see research.md D4.

## 1. Run the harness locally

```bash
dotnet test tests/DocFences.Tests/DocFences.Tests.fsproj -c Debug
```

Expected: every F# fence in `template/product-skills/**`, the api-surface mirror `.fsi` `///` comments, and
scaffold sources is assembled into the generated pinned project, which restores once and builds. Green.

## 2. Prove it goes RED on an unreleased symbol (fail-open guard)

Temporarily add a fence to any product skill that names a symbol the pin does not export, e.g.:

````markdown
```fsharp
Scene.thisDoesNotExistInThePin ()
```
````

Re-run step 1. Expected: **fail**, with a compiler diagnostic naming that skill file and the fence line.
Revert the edit; green returns. This is SC-002.

## 3. Prove a legitimate partial snippet still compiles (fail-closed guard)

Confirm an existing fence that relies on the corpus preamble (an ambient `open`) compiles without being
edited into a self-contained program. If it fails, the preamble (research.md D2) is wrong — fix the preamble
config, not the doc. This is FR-004 / edge case "partial snippets" and guards against the #664 failure of
accusing a correct doc.

## 4. Prove the intentional-skip opt-out (replaces the ledger)

Mark a deliberately-illustrative fence with the per-fence `SkipWithReason` directive (research.md D3).
Expected: it is excluded from the compile set, the reason is reported, and the run stays green — with **no**
entry added to `pinned-api-doc-ledger.txt`.

## 5. Prove the singletons (SC-003)

```bash
# exactly one fence engine
grep -rl 'MarkdownFences' tests scripts | grep -v obj | grep -v bin
# these must return NOTHING once P2/P3 land:
grep -rn 'skillFenceSymbols\|mirrorValSymbols\|mirrorDocCommentSymbols\|scaffoldSourceDocCommentSymbols' tests
grep -rn 'runProbeBuild\|runNameofProbe\|oracleVersion' tests
```

Expected after P2: the retired symbols are gone; `MarkdownFences` and `SurfaceSignature` are the only fence
engine and `.fsi` reader; the PE/metadata walk is the only symbol oracle.

## 6. Prove the ledger is empty (SC-004)

```bash
grep -cvE '^\s*(#|$)' tests/Build.Tests/pinned-api-doc-ledger.txt   # -> 0
```

## 7. Prove S-DOC is homonym-proof (SC-005)

Add a fence that defines `let describe = ...` locally but never cites `Scene.describe`. Run the S-DOC
coverage suite. Expected: `Scene.describe` is **not** credited as documented by that fence — because it is
not in that fence's `SymbolManifest`.

## 8. Prove no coverage regression (SC-006)

Reconstruct each historical case (#550, #591, #592, #598, #619) as a fence and confirm the harness catches
it (red) exactly where the retired extractor did. This is the gate on each deletion in P2/P3.

## CI

No new gate step is required for the harness to *run*: `.github/workflows/gate.yml`'s "Default local tier"
loop already runs every `tests/*.Tests.fsproj` the slnx lists. A named step is added only if the harness
needs a distinct restore/setup. Confirm `DocFences.Tests` appears in the slnx and runs on a PR.
