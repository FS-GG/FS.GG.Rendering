# Feature Specification: Compile the docs instead of parsing them

**Feature Branch**: `255-compile-the-docs`

**Created**: 2026-07-17

**Status**: Draft

**Input**: Epic #695 — "Compile the docs instead of parsing them: retire the hand-rolled compiler front-end behind doc-vs-pin"

## Context *(why this exists)*

This repo ships documentation as a product artifact — product skills (`SKILL.md`), the api-surface
mirror (`.fsi` with `///` comments), and scaffold sources. A scaffolded product compiles against the
**published** package, so a shipped doc can name a symbol that no released package exports — a real
defect class that has shipped repeatedly (#550, #591, #592, #598, #619).

The response was to hand-roll a compiler front-end out of regexes: six doc-symbol extractors, three
markdown-fence engines, six mutually-disagreeing `.fsi` `val` readers, and two independent pinned-surface
oracles (a compile probe and a PE/metadata walk) — ~4,200 lines that does not converge. A heuristic oracle
over prose has two failure directions and this repo has hit both: fail-open (#654 credited 32 surfaces by
homonym; #683 unioned across packages) and fail-closed (#664 accused correct docs). Each fix replaces one
heuristic with a heuristic that has a new hole, because **regexes over English cannot decide symbol
identity** — the information that decides it (which `open`s are in scope here) is not in the token stream,
it is in the compiler.

The blocking work (the mirror-generator epic #694) is **closed**, and #669 has already landed a single
shared fence scanner (`MarkdownFences` in `TestSupport`). This feature does the rest: **stop parsing the
docs and compile them.**

The stakeholders are the repo maintainers who keep the doc-vs-pin gate honest, and the product authors who
copy code out of a shipped doc and expect it to build against the package they have.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A doc that names an unreleased symbol fails the build (Priority: P1)

Every F# code fence in every shipped doc is compiled against the **pinned** packages on every PR. A fence
that names a symbol the pinned package does not export fails to compile, and the PR goes red with the
compiler's own error, naming the file and line.

**Why this priority**: This is the whole thesis. It replaces the fail-open/fail-closed heuristic with the
one oracle that models `member`, `abstract`, nested modules, DU cases, primed identifiers and `internal`
correctly and for free — and it additionally verifies the thing no regex ever could: that the code a
product author copies out of the doc actually *works* against the package they will have. Every subsequent
deletion depends on this existing first.

**Independent Test**: Introduce a fence naming a symbol absent from the pin; confirm CI fails with a
compiler error citing that doc. Remove it; confirm CI is green. Delivers value on its own even before any
old machinery is removed.

**Acceptance Scenarios**:

1. **Given** a shipped doc whose F# fence names only symbols the pinned package exports, **When** the PR
   gate runs, **Then** the fence project restores against the local feed and compiles, and the gate passes.
2. **Given** a shipped doc whose F# fence names a symbol absent from the pinned package, **When** the PR
   gate runs, **Then** the fence project fails to compile and the gate fails, citing the offending doc and
   the compiler error.
3. **Given** a fence that is a legitimate partial snippet (relies on ambient `open`s or surrounding
   context), **When** the harness assembles it into a compilation unit, **Then** it compiles under the
   declared preamble rather than being reported as a false defect.

### User Story 2 - One fence engine, one `.fsi` reader, one symbol oracle (Priority: P2)

The duplicated machinery the compiler subsumes is deleted. Exactly one fence engine (`MarkdownFences`), one
`.fsi` reader (`SurfaceSignature`), and one symbol oracle (the PE/metadata walk) remain in the tree. The
compile probe and the `oracleVersion` hardcode are gone; the four fence-side extractors are gone.

**Why this priority**: The convergence the epic promises is only real if the redundant readers actually
leave the tree — otherwise the next hole still lands in whichever copy nobody looked at. Depends on P1
holding the line first.

**Independent Test**: `grep` the tree for the retired symbols (`skillFenceSymbols`, `mirrorValSymbols`,
`mirrorDocCommentSymbols`, `scaffoldSourceDocCommentSymbols`, `runProbeBuild`, `runNameofProbe`,
`oracleVersion`) and the third fence reader in `check-symbology-skill-parity.fsx`; confirm none remain and
the suites are still green.

**Acceptance Scenarios**:

1. **Given** the fence-compile harness holds the doc-vs-pin line, **When** the four fence-side extractors
   and the compile-probe oracle are removed, **Then** the remaining suites pass and no doc-vs-pin coverage
   is lost.
2. **Given** three fence readers existed, **When** the work completes, **Then** exactly one fence engine
   (`MarkdownFences`) is referenced everywhere fences are read, including `check-symbology-skill-parity.fsx`.
3. **Given** six `.fsi` `val` regexes existed, **When** the work completes, **Then** exactly one `.fsi`
   reader (`SurfaceSignature`) is referenced by every caller.

### User Story 3 - The ledger is empty and S-DOC coverage is homonym-proof (Priority: P3)

`pinned-api-doc-ledger.txt` holds no suppressions: the compiler, not a ledger, holds the line. S-DOC
coverage ("is every public surface taught somewhere") redefines "cited" to mean "appears in a fence that
compiled against the pin", which structurally dissolves the same-language-homonym class (#692, #663) — a
local `let describe` can no longer credit `Scene.describe`.

**Why this priority**: This is the residue that proves the machinery is actually retired rather than merely
duplicated. The ledger's remaining line and the homonym class are the tell that a heuristic is still load-
bearing; removing them is what closes the epic.

**Independent Test**: Confirm the ledger is empty and the build is green. Add a doc that defines a local
binding whose name collides with a public surface but does not cite that surface; confirm S-DOC does not
credit the surface as taught.

**Acceptance Scenarios**:

1. **Given** the fence-compile harness is live, **When** the ledger is emptied, **Then** the gate stays
   green, or any remaining line surfaces as a genuine defect fixed at its root rather than suppressed.
2. **Given** a doc that defines `let describe` but never cites `Scene.describe`, **When** S-DOC coverage
   runs, **Then** `Scene.describe` is not credited as documented by that doc.

### Edge Cases

- **Partial snippets**: a fence that is not a self-contained compilation unit (needs `open`s, or continues
  a module started in prose). The harness must define how a fence becomes compilable — a declared preamble
  per corpus, or a documented `open` convention — rather than reporting the snippet as a defect (the exact
  fail-closed failure of #664).
- **Intentionally-illustrative fences**: a fence that shows a compile *error*, deprecated usage, or
  pseudo-code on purpose. There must be an explicit, auditable way to mark such a fence as not-compiled,
  distinct from the retired blanket ledger.
- **Non-F# fences** (`bash`, `text`, `json`): already classified by `MarkdownFences`; must be excluded from
  the compile set without being silently dropped from coverage accounting.
- **Prose symbols**: an API named in a sentence, not a fence (e.g. `Viewer.runAppWithAudio`). The compiler
  cannot see these; they remain the job of the one retained symbol oracle.
- **Pin moves underneath the harness**: the pinned version is `$(FsGgUiVersion)`; the harness must read the
  live pin, never a second hardcoded oracle version (the `oracleVersion = "0.9.0"` smell).
- **Feed unavailability / release window**: restoring the pinned packages must respect the release-pending
  waiver so a pin-probe does not wedge a release (per prior pin-probe incidents #611, #848).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST extract every F# code fence from every shipped doc corpus — product skills
  (`SKILL.md`), the api-surface mirror `.fsi` `///` comments, and scaffold-source `///` comments — using the
  single shared fence engine (`MarkdownFences`).
- **FR-002**: The system MUST assemble the extracted fences into one or more generated F# projects that
  `PackageReference` the pinned packages at `$(FsGgUiVersion)` and restore against the local nupkg feed.
- **FR-003**: The system MUST compile the generated fence project(s) in CI on every PR, and MUST fail the
  gate when a fence names a symbol the pinned package does not export, reporting the offending doc and the
  compiler diagnostic.
- **FR-004**: The system MUST provide a defined, auditable mechanism for making a partial fence compilable
  (a declared preamble / `open` convention per corpus) so that legitimate snippets are not reported as
  defects.
- **FR-005**: The system MUST provide an explicit, auditable way to mark an intentionally non-compiling
  fence (illustrative error, pseudo-code) as excluded from the compile set — distinct from, and replacing,
  the blanket ledger.
- **FR-006**: The system MUST retire the four fence-side extractors (`skillFenceSymbols`, `mirrorValSymbols`,
  `mirrorDocCommentSymbols`, `scaffoldSourceDocCommentSymbols`) and the compile-probe oracle
  (`runProbeBuild` / `runNameofProbe`) once the fence-compile harness holds the doc-vs-pin line.
- **FR-007**: The system MUST leave exactly one fence engine (`MarkdownFences`), one `.fsi` reader
  (`SurfaceSignature`), and one symbol oracle (the PE/metadata walk) in the tree; the third fence reader in
  `check-symbology-skill-parity.fsx` and the five duplicate `.fsi` `val` regexes MUST be folded onto the
  survivors.
- **FR-008**: The retained symbol oracle MUST be exposed behind one API and MUST serve only the residue the
  compiler cannot see — prose symbols named in sentences rather than fences.
- **FR-009**: The system MUST read the pinned version from the live pin (`$(FsGgUiVersion)`) and MUST NOT
  carry a second hardcoded oracle version.
- **FR-010**: `pinned-api-doc-ledger.txt` MUST be empty on completion; any line that cannot simply be
  deleted MUST be resolved as a genuine defect at its root, not re-suppressed.
- **FR-011**: S-DOC coverage MUST define "cited" as "appears in a fence that compiled against the pin", so
  that a same-language homonym (a local `let` binding sharing a public surface's name) cannot credit that
  surface as documented.
- **FR-012**: The pinned-package restore MUST respect the release-pending waiver so the gate does not wedge
  a release when the pinned version is not yet on the feed.

### Key Entities

- **Doc corpus**: the set of shipped docs that carry F# fences — product skills, api-surface mirror `.fsi`
  `///` comments, and scaffold sources.
- **Fence**: a single F# code block in a doc, classified by `MarkdownFences`; the unit that is compiled.
- **Fence-compile harness**: the component that turns fences into a generated, pinned-referencing project
  and compiles it.
- **Pinned surface**: the API the pinned packages (`$(FsGgUiVersion)`) actually export.
- **Symbol oracle**: the one retained PE/metadata reader of the pinned surface, used only for prose symbols.
- **Ledger** (`pinned-api-doc-ledger.txt`): the suppression file to be emptied and replaced by compilation.
- **S-DOC coverage**: the separate check that every public surface is taught somewhere, rebased on
  compiled-fence membership.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of F# fences in every shipped doc corpus — except those explicitly marked
  `SkipWithReason` (FR-005) — are compiled against the pinned packages on every PR.
- **SC-002**: A doc introducing a symbol absent from the pinned package fails the gate 100% of the time,
  with a diagnostic that names the offending doc.
- **SC-003**: Exactly one fence engine, one `.fsi` reader, and one symbol oracle remain in the tree
  (verifiable by count); the four fence-side extractors, the compile probe, the third fence reader, the five
  duplicate `.fsi` regexes, and the hardcoded oracle version are all absent.
- **SC-004**: `pinned-api-doc-ledger.txt` contains zero suppression lines.
- **SC-005**: The same-language-homonym failure (a local binding crediting a public surface by name) can no
  longer occur in S-DOC coverage, demonstrated by a regression test.
- **SC-006**: No net loss of doc-vs-pin coverage relative to the retired machinery — every defect class the
  old extractors caught is still caught (by compilation or by the one prose oracle), demonstrated by the
  historical cases (#550/#591/#592/#598/#619) still being caught.

## Assumptions

- The mirror-generator epic (#694) is closed, so the api-surface mirror is generated from the pin and is a
  trustworthy corpus to compile.
- `MarkdownFences` (from #669) is the accepted single fence scanner and its classification of what an F#
  block is stands.
- The pinned packages are available on the local nupkg feed at `$(FsGgUiVersion)` during a normal PR run;
  the release-pending waiver governs the window when they are not.
- Prose symbols (APIs named in sentences) remain a real residue that a compiler cannot check, so one symbol
  oracle must survive — the epic explicitly keeps it.
- S-DOC coverage is a distinct question from doc-vs-pin and keeps its own answer, now sourced from
  compiled-fence membership rather than name matching.
- "Shipped docs" means the corpora enumerated in FR-001; docs not shipped as product artifacts are out of
  scope.
