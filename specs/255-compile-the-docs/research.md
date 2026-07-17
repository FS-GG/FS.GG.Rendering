# Phase 0 Research: Compile the docs instead of parsing them

The spec left no `NEEDS CLARIFICATION` markers; the open questions here are design decisions, each resolved
with a decision / rationale / alternatives. The two that make or break the harness are **D2 (snippet →
compilation unit)** and **D3 (the non-compiling-fence opt-out)** — they are the fail-closed trap of #664.

## D1 — Where the harness lives

**Decision**: A new dedicated test project `tests/DocFences.Tests`. It generates a *separate* project that
`PackageReference`s the pin and shells `dotnet build`; the test project itself does not reference the pin.

**Rationale**: The gate's "Default local tier" step loops over every `tests/*.Tests.fsproj` the slnx lists,
so a new project runs automatically with no gate.yml change. Generating-then-building a pinned project (not
referencing it directly) is exactly what `runProbeBuild` already does — proven plumbing — and it keeps the
pin out of the harness assembly's own closure so the harness can restore once and reuse. Build.Tests must
stay pin-free (its `PEReader` oracle is deliberately in-box); adding a pinned reference there would violate
that.

**Alternatives**: (a) Fold into `Build.Tests` — rejected: pollutes the pin-free oracle project. (b) Make
the test project itself reference the pin and reflect — rejected: that is the `Assembly.LoadFrom`/
`MetadataLoadContext` path the existing code documents as failing on unresolved dependencies.

## D2 — Snippet → compilation unit (the crux)

**Decision**: Each F# fence is compiled as its own compilation unit wrapped in a generated module, under a
**declared per-corpus preamble** of `open`s. A fence may extend the preamble with a machine-readable
`open`-directive when it needs symbols beyond the default set. The preamble is data in the repo, reviewed
like code — it is the explicit answer to "which `open`s are in scope here", the information #683 proved is
not in the token stream.

**Rationale**: A fence is a snippet, not a program; without a defined preamble a correct doc fails to
compile (the #664 fail-closed failure). Making the preamble *declared and auditable* (rather than inferred)
means a fence that only compiles because of an implicit ambient open is visibly relying on it. Per-fence
(not per-doc concatenation) isolation gives a precise doc+line on failure and stops one fence's bindings
from leaking into another (the `let describe` homonym leakage, in reverse).

**Alternatives**: (a) Concatenate all fences of a doc into one unit — rejected: cross-fence binding leakage
and imprecise blame. (b) Infer opens from the symbols used — rejected: that is re-implementing name
resolution, i.e. the regex oracle this epic exists to delete. (c) No preamble, require every fence to be
self-contained — rejected: forces noise into shipped docs a human reads.

**Open sub-question for `/speckit-tasks`**: the exact preamble format (a per-corpus defaults file + an
in-fence directive syntax — likely an HTML comment adjacent to the fence, invisible in rendered docs). The
early live proof (quickstart) pins the format down on real fences before it is generalized.

## D3 — The non-compiling-fence opt-out (replaces the ledger)

**Decision**: A fence that is *intentionally* non-compiling (illustrative error, deprecated usage,
pseudo-code) is marked with an explicit, auditable per-fence directive that excludes it from the compile set
and records *why*. This replaces the blanket `pinned-api-doc-ledger.txt`, which goes empty.

**Rationale**: The ledger's failure was that it was a blanket suppression divorced from the fence — a line
in a separate file that outlived the reason for it (#692's one-way deletion). A per-fence, reason-carrying
marker is local, greppable, and dies with the fence. FR-005 requires the opt-out to be distinct from the
retired ledger and loud, not silent.

**Alternatives**: (a) Keep a slimmer ledger — rejected: same divorced-suppression failure mode, just
smaller. (b) Language-tag the fence non-F# (```text) to hide it — rejected: silently drops it from coverage
(the exact silent-drop `MarkdownFences` warns about) and lies about what the fence is.

## D4 — Restore, feed, and the release-pending waiver

**Decision**: Reuse the existing `runNameofProbe` restore approach — the **published** pinned packages from
**nuget.org**, package sources `<clear/>`ed down to nuget.org, restored into an isolated packages folder so
no locally-`pack`ed unreleased seam can satisfy the restore — amortized into one restore for the whole fence
project. Read the pin from the live `$(FsGgUiVersion)`; carry **no** second oracle version. Honor the
release-pending (`PinPending`) waiver so the harness does not probe a not-yet-published pin during a release
window. (Correction to an earlier draft that said "local nupkg feed": that feed is the template-consumer
mechanism; restoring the oracle from it would let a local `pack` fake a green — the exact failure the
cleared-sources isolation exists to prevent.)

**Rationale**: The `oracleVersion = "0.9.0"` hardcode existed only because the probe restored per-call and
the pin moved underneath it; a single up-front restore keyed on the live `$(FsGgUiVersion)` removes the
reason for a second oracle. The waiver is the established guard against wedging a release on a pin-probe
([[fsgg-release-window-pin-probes]] — #611, #848).

**Alternatives**: Pin a fixed oracle version — rejected outright; it is the exact smell the epic names.

## D5 — Sequencing: hold the line before deleting

**Decision**: Land P1 (harness green in CI) first and leave the old extractors running alongside it for one
transition. Only then delete, one class at a time, each removal gated on SC-006 (the historical defect cases
still caught). P2 (dedup engines/readers/oracle) and P3 (empty ledger, rebase S-DOC) follow.

**Rationale**: The epic's whole complaint is heuristics replaced by heuristics with new holes; deleting the
old gate before the new one is proven would risk a coverage gap in exactly the defect class that has shipped
five times. Belt-and-suspenders for one transition is cheap; a regression here is not.

**Alternatives**: Big-bang replace — rejected: no safety margin on a gate with a five-incident history.

## D6 — S-DOC rebasing

**Decision**: The harness emits a per-fence **symbol manifest** (which pinned symbols each fence's compiled
unit actually resolved). S-DOC redefines "cited" as "appears in a fence that compiled against the pin",
consuming that manifest instead of name-matching prose.

**Rationale**: Compilation is what makes "cited" mean *resolved to the real symbol* rather than *shares an
English word* — a local `let describe` cannot be in the manifest for `Scene.describe`, so the homonym class
(#692, #663) is structurally gone. This reuses the harness's own output rather than adding a new reader.

**Alternatives**: Keep name-matching but tighten the regex — rejected: the epic's core finding is that this
does not converge.

## Cross-repo / reference notes

- [[fsgg-release-window-pin-probes]] — the release-pending waiver D4 depends on.
- [[mirror-prose-drops-are-mostly-rewordings]] — the mirror corpus is generated from the pin (#694 closed),
  so mirror `.fsi` `///` fences are a trustworthy compile corpus, not a moving target.
