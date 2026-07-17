# Implementation Plan: Compile the docs instead of parsing them

**Branch**: `255-compile-the-docs` | **Date**: 2026-07-17 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/255-compile-the-docs/spec.md` (epic #695)

## Summary

Replace the hand-rolled regex compiler front-end that guards doc-vs-pin with the real F# compiler. A new
test project generates, for every F# fence in the two fence-bearing corpora (skill `SKILL.md` and
scaffold-source `///`), a compilation unit in a project that `PackageReference`s the pinned packages
(`$(FsGgUiVersion)`), restores the **published** packages from **nuget.org** (cleared sources, isolated dir —
the `runNameofProbe` approach), and builds it in CI on every PR. A fence naming a symbol the pin does not
export then fails to compile —
the compiler models `member`/`abstract`/nested modules/DU cases/primed identifiers/`internal` correctly and
for free, and additionally proves the copied code *works*. Once that harness holds the line, the machinery
it subsumes is deleted down to one fence engine (`MarkdownFences`), one `.fsi` reader (`SurfaceSignature`),
one symbol oracle (the PE/metadata walk, kept for prose symbols AND the generated mirror's `val`/prose
check), an empty ledger, and an S-DOC coverage check rebased on compiled-fence membership. Only the two
fence-reading extractors (`skillFenceSymbols`, `scaffoldSourceDocCommentSymbols`) are retired; the mirror
extractors stay because the generated mirror has no fence to compile.

> **Standing assumption — root-cause hypotheses are unverified until exercised.** The claim that the
> compiler subsumes each extractor is provisional. `/speckit-tasks` MUST schedule an **early live proof** in
> the Foundational phase (before any deletion): stand up the harness on a handful of real fences —
> including one known-good and one deliberately-unreleased symbol — and confirm green/red end to end. No
> deletion (P2/P3) is built on the unproven assumption that P1 catches what the extractor caught; SC-006
> (no coverage loss vs. the historical cases #550/#591/#592/#598/#619) is the gate on each removal.

## Technical Context

**Language/Version**: F# on .NET `net10.0` (`templateTfm = "net10.0"`); tests are **Expecto** `.fsproj`
under `tests/` (`YoloDev.Expecto.TestSdk`, `Tests.runTestsInAssemblyWithCLIArgs`), matching the repo.

**Primary Dependencies**: The F# compiler (`dotnet build`) as the oracle; `FS.GG.TestSupport.MarkdownFences`
(the single fence scanner, #669); `SurfaceSignature` (`.fsi` reader); the pinned `FS.GG.UI.*` packages at
`$(FsGgUiVersion)`, restored **published** from nuget.org (cleared sources, isolated `RestorePackagesPath`).

**Storage**: Filesystem only — generated fence projects in a temp/obj dir; the doc corpora in-tree; the
ledger file `tests/Build.Tests/pinned-api-doc-ledger.txt` (to be emptied).

**Testing**: Expecto via `dotnet test`; the gate runs every `tests/*.Tests.fsproj` the slnx lists (gate.yml
"Default local tier" loop), so a new test project is picked up automatically — no gate.yml edit needed to
run it, only to name it if it must run in a distinct step.

**Target Platform**: CI (ubuntu-latest, `dotnet-version: 10.0.x`) on every PR, plus local `dotnet test`.

**Project Type**: Internal developer-tooling / CI-gate over the repo's own shipped documentation.

**Performance Goals**: The fence-compile step must fit the existing gate budget. One restore of the pinned
packages amortized across all fences (a single generated project, many compilation units) rather than the
probe's per-invocation isolated restore.

**Constraints**: Must read the *live* pin (`$(FsGgUiVersion)`) — no second hardcoded oracle version. Must
respect the release-pending waiver so a pin-probe cannot wedge a release (cf. #611, #848). Must not silently
drop a fence from coverage accounting.

**Scale/Scope**: Corpora — `template/product-skills/**/*.md`, `template/base/docs/api-surface/**/*.fsi`
(`///`), scaffold sources under `template/base/src` + `template/fragments` (`///`). ~4,200 lines of existing
regex machinery to retire across `TemplateConsumesPinnedApiTests.fs`, `SurfaceDocCoverageTests.fs`,
`ApiSurfaceMirrorTests.fs`, `SurfaceSignature.fs`, `Issue496FSharpCoreShadowingTests.fs`, and
`scripts/check-symbology-skill-parity.fsx`.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Spec → FSI → Semantic Tests → Implementation** — This feature IS a test/tooling change; there is no
  new product `.fsi` surface. The harness's own small helper surface (fence → compilation-unit assembly,
  the symbol-manifest emitter) follows the same order: sketch the helper signatures, test them against real
  fences, then implement. **PASS.**
- **II. Visibility in `.fsi`** — Any new library helper (if placed in `TestSupport`) carries a `.fsi`. Test
  projects themselves do not. No `private`/`internal` on top-level `.fs` bindings. **PASS.**
- **III. Idiomatic Simplicity** — The whole point is to *remove* clever regex machinery and let the compiler
  do the work. Net simplification. No SRTP/reflection/type-providers introduced. **PASS.**
- **IV. Elmish/MVU boundary** — N/A (no stateful/product runtime surface). **PASS.**
- **V. Test Evidence Is Mandatory** — The evidence is real by construction: a real `dotnet build` of real
  fences against the real pin. The early live proof (Summary) supplies the end-to-end evidence before any
  deletion. **PASS.**
- **VI. Observability & Safe Failure** — A fence failure surfaces the compiler diagnostic with doc+line
  (clickable). The non-compiling-fence marker and any dropped-from-coverage fence must be *loud*, never
  silent (FR-005, edge cases). **PASS.**

No violations → Complexity Tracking left empty.

## Project Structure

### Documentation (this feature)

```text
specs/255-compile-the-docs/
├── plan.md              # This file
├── research.md          # Phase 0 — the design decisions (harness placement, snippet wrapping, opt-out, sequencing)
├── data-model.md        # Phase 1 — entities (corpus, fence, compilation unit, symbol manifest, pinned surface)
├── quickstart.md        # Phase 1 — how to prove the harness end to end (green + red)
└── checklists/
    └── requirements.md  # spec quality checklist (from /speckit-specify)
```

No `contracts/`: this feature exposes no external interface — it is internal CI tooling over the repo's own
docs. The "contract" it enforces (fences compile against the pin) is the harness behavior itself, captured
in quickstart.md.

### Source Code (repository root)

```text
tests/
├── DocFences.Tests/                 # NEW — the fence-compile harness (P1). Picked up by the gate's slnx loop.
│   ├── DocFences.Tests.fsproj
│   └── DocFencesCompileTests.fs      # extract → assemble → generate pinned project → dotnet build → assert
├── TestSupport/
│   ├── MarkdownFences.fs             # the ONE fence engine (exists, #669) — reused, not re-added
│   └── SurfaceSignature.fs           # the ONE .fsi reader — the five duplicate val-regexes fold onto this (P2)
├── Build.Tests/
│   ├── TemplateConsumesPinnedApiTests.fs   # DELETE 2 fence-reading extractors + compile probe + oracleVersion (P2);
│   │                                       # KEEP mirrorValSymbols/mirrorDocCommentSymbols (generated fence-less mirror)
│   │                                       # KEEP the one PE/metadata oracle (readSurfaceAt) behind one API for prose
│   └── pinned-api-doc-ledger.txt           # EMPTY on completion (P3)
└── Package.Tests/
    └── SurfaceDocCoverageTests.fs    # S-DOC: "cited" := "appears in a fence that compiled against the pin" (P3)

scripts/
└── check-symbology-skill-parity.fsx  # fold its third fence reader onto MarkdownFences (P2)

.github/workflows/
└── gate.yml                          # touch ONLY if DocFences.Tests needs a named step (e.g. distinct restore); the
                                      # slnx loop already runs any tests/*.Tests.fsproj
```

**Structure Decision**: A dedicated `tests/DocFences.Tests` project owns the harness. It does **not** itself
`PackageReference` the pin — it *generates* a project that does (exactly as `runProbeBuild` does today) and
shells `dotnet build`, so the pin stays out of the test assembly's own closure. This keeps the pin-restore
localized, lets the harness batch all fences into one restore, and is automatically run by the gate's
slnx-derived loop. The metadata oracle stays in `Build.Tests` (in-box `PEReader`, no PackageReference) where
it already lives, now behind a single API and used only for the prose residue.

## Complexity Tracking

*No constitution violations — section intentionally empty.*
