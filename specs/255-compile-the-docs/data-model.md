# Phase 1 Data Model: Compile the docs instead of parsing them

These are the conceptual entities the harness manipulates. This is tooling over files, not a persistent
store — "entities" are in-memory records produced during a test run, plus two on-disk artifacts (the
per-corpus preamble config and the emptied ledger).

## DocCorpus

The set of shipped docs carrying F# fences.

| Field | Meaning |
|-------|---------|
| `Kind` | `ProductSkill` \| `ApiSurfaceMirror` \| `ScaffoldSource` |
| `Root` | `template/product-skills/**/*.md` \| `template/base/docs/api-surface/**/*.fsi` \| `template/base/src` + `template/fragments` (`*.fs`) |
| `Preamble` | the declared default `open` set for this corpus (D2) |

Rule: every corpus is enumerated; a corpus that yields zero fences is reported, never silently absent
(FR-001, no-silent-drop).

## Fence

One F# code block in a doc — the unit that is compiled. Produced by `MarkdownFences.scan` (the one engine).

| Field | Meaning |
|-------|---------|
| `Doc` | repo-relative path (clickable on failure) |
| `StartLine` | 1-based line of the opening fence |
| `Lang` | language tag as classified by `MarkdownFences` (only F# tags enter the compile set) |
| `Body` | the fence lines |
| `ExtraOpens` | optional per-fence `open` directive extending the corpus preamble (D2) |
| `CompileMode` | `Compile` (default) \| `SkipWithReason("…")` (the D3 opt-out) |

State: `Lang ∉ F#` → excluded from compile set but still counted for coverage accounting.
`CompileMode = SkipWithReason` → excluded from compile set, reason recorded loudly.

## CompilationUnit

A generated `.fs` file wrapping one `Fence` for the compiler.

| Field | Meaning |
|-------|---------|
| `ModuleName` | generated, unique per fence (encodes doc+line for blame) |
| `Preamble` | corpus `Preamble` + fence `ExtraOpens` |
| `Source` | preamble + wrapped `Body` |
| `Origin` | back-reference to `Fence` (so a compiler diagnostic maps to doc+line) |

## FenceProject (generated)

The single generated project all `CompilationUnit`s compile in.

| Field | Meaning |
|-------|---------|
| `Tfm` | `net10.0` (`templateTfm`) |
| `PackageRefs` | the pinned `FS.GG.UI.*` packages at the live `$(FsGgUiVersion)` |
| `RestoreSource` | the local nupkg feed |
| `Units` | all in-scope `CompilationUnit`s |

Behavior: one restore, one build; a build failure is mapped through `Origin` to `{Doc, Line, Diagnostic}`.

## PinnedSurface

What the pinned packages export — read two ways, now consolidated:

| Consumer | Reader | Kept? |
|----------|--------|-------|
| Fence check | the F# compiler (via `FenceProject` build) | primary oracle |
| Prose residue | the PE/`MetadataReader` walk (`readSurfaceAt`) behind one API | the ONE retained symbol oracle |
| ~~Fence check (old)~~ | ~~`runProbeBuild`/`runNameofProbe` compile probe~~ | **deleted** (P2) |

## SymbolManifest

Emitted by the harness: which pinned symbols each compiled fence actually resolved.

| Field | Meaning |
|-------|---------|
| `Fence` | origin fence |
| `ResolvedSymbols` | fully-qualified pinned symbols the unit bound |

Consumer: S-DOC coverage — "cited" := a public surface appears in some fence's `ResolvedSymbols` (D6).
This is what dissolves the same-language-homonym class.

## Ledger (on disk, to be emptied)

`tests/Build.Tests/pinned-api-doc-ledger.txt`. Target state: **zero suppression lines** (FR-010). Its role
is taken over by per-fence `CompileMode = SkipWithReason` (local, reason-carrying) for the genuine
illustrative cases.

## Retirement ledger (what leaves the tree)

| Symbol / artifact | File | Fate |
|-------------------|------|------|
| `skillFenceSymbols` | `TemplateConsumesPinnedApiTests.fs` | delete (compiler subsumes) |
| `mirrorValSymbols` | `TemplateConsumesPinnedApiTests.fs` | delete |
| `mirrorDocCommentSymbols` | `TemplateConsumesPinnedApiTests.fs` | delete |
| `scaffoldSourceDocCommentSymbols` | `TemplateConsumesPinnedApiTests.fs` | delete |
| `runProbeBuild` / `runNameofProbe` | `TemplateConsumesPinnedApiTests.fs` | delete (probe oracle) |
| `oracleVersion = "0.9.0"` | `TemplateConsumesPinnedApiTests.fs` | delete (read live pin) |
| third fence reader | `scripts/check-symbology-skill-parity.fsx` | fold onto `MarkdownFences` |
| 5 duplicate `val` regexes | `TemplateConsumesPinnedApiTests.fs:144`, `SurfaceDocCoverageTests.fs:81`, `ApiSurfaceMirrorTests.fs:238`, `Issue496…:132` (+ keep `SurfaceSignature.fs`) | fold onto `SurfaceSignature` |

Invariant at completion: exactly **one** fence engine, **one** `.fsi` reader, **one** symbol oracle
(SC-003).
