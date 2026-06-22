# Feature 188 — implementation-phase feedback

## Process friction

1. **`type X` + `module X` companion pattern does not survive a cross-file split (FS0250).**
   Severity: **medium** (blocked US1's first build). When `type Paint`/`type Scene` moved to `Types.fs`
   but `module Paint`/`module Scene` stayed in `Scene.fs`, the build failed: *"A module and a type
   definition named 'Paint' occur in namespace … in two parts of this assembly."* In a single file F#
   auto-suffixes the module's compiled name to `PaintModule`; across files it refuses unless the suffix
   is explicit. Fix: `[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]` on the
   module in **both** `.fs` and `.fsi`. This produces the *same* compiled name F# already used, so it is
   surface-neutral. **Generalizable guidance:** any future "lift the type wall" split (Pattern E) must
   add the ModuleSuffix attribute to every companion module whose type moves out — worth a note in the
   decomposition playbook.

2. **Byte-identical transcription risk when relocating fingerprint code.** Severity: **medium.** The
   `String.concat ""` unit-separator in the two fingerprint builders is easy to drop/normalize
   during a move (an editor/round-trip can turn the escape into an empty string or a raw 0x1F byte).
   Caught only because the shaping suites assert SHA-256 fingerprints. **Guidance:** for byte-identical
   relocations, diff the moved bytes against `git show HEAD:old` rather than retyping; prefer verbatim
   range extraction over hand-copy.

## Generalizable-code candidates

- **`internal` companion module as the surface-neutral relocation lever (US2).** Moving the shaped-text
  core into `module internal FS.GG.UI.Scene.Text.Shaping` and keeping `module Scene` as thin public
  delegations gave a **zero** surface diff with no version bump. This "internal implementation module +
  public delegating façade" is a reusable pattern for any future god-module split that wants to relocate
  bulk without surface churn. Candidate for the shared decomposition guidance.

- **`List.sortBy k |> List.distinctBy k` as the canonical "collapse duplicates, keep first, stay sorted"
  idiom.** Used identically on both inspection paths for FR-006. Low-risk, reads cleanly; reasonable
  house style for finding/record dedup elsewhere.

## What went well

- Risk-sequenced slices (US1 surface-neutral → US2 byte-identical → US3 behavior change) each built and
  tested green independently; the empty-surface-diff gate after every slice caught regressions early.
- Comprehensive `baseline-tests.fsx` (all 16 test projects incl. Package.Tests + samples) surfaced the
  2 pre-existing reds up front, so they were never mistaken for refactor regressions.
