# 0106 — Governance is generated, not compared: a fact has one home, and a checker is not a generator

**Status**: Proposed · **Date**: 2026-07-13 · **Issues**: [#694](https://github.com/FS-GG/FS.GG.Rendering/issues/694), [#695](https://github.com/FS-GG/FS.GG.Rendering/issues/695), [#696](https://github.com/FS-GG/FS.GG.Rendering/issues/696)

> **Numbering.** Repo-local ADRs resume at `0100` (see [`../README.md`](../README.md)). This one
> follows [ADR-0105](./0105-a-required-gate-reads-only-the-commit.md).

> **Applies, in this repo's substrate, a decision the org has already made twice.**
> [ADR-0014](https://github.com/FS-GG/.github/blob/main/docs/adr/0014-skill-vendoring-one-manifest-one-materialize-verify.md)
> (Accepted, 2026-07-01): *"A skill has **exactly one canonical body**; the roots are copies of it"* —
> one manifest, one shared `materialize-and-verify`, content-addressed.
> [ADR-0034](https://github.com/FS-GG/.github/blob/main/docs/adr/0034-typed-coordination-engine.md)
> (Accepted): a rule *"computed in five places and agrees in none"*, living in *"54 vendored copies"*,
> replaced by a typed core whose docs are **generated projections** — *"retiring the drift class by
> construction."*
>
> ADR-0034 states its own limit: *"explicitly **not** a fix for the build/publish/pin substrate."*
> **That substrate is this ADR's subject.**

## Context

### The measurement

| | lines |
|---|---|
| product source (`src/`) | 54,853 |
| **governance code** (`scripts/` + `tests/Build.Tests` + `tests/Package.Tests`) | **27,289** |
| committed baselines, mirrors, evidence, ledgers | ~15,000 across 94+ files |

Half a product's worth of code exists to check that hand-maintained copies of the same fact agree with
each other. In the 30 hours to 2026-07-13T16:00Z this repo merged 85 PRs and closed 72 issues, at a
ratio of **42 `fix` commits to 9 `feat`**. Nearly all of it was governance repairing governance.

### The one public surface, in four hand-maintained copies

| # | representation | maintained how | size |
|---|---|---|---|
| 1 | `src/**/*.fsi` | authored | — |
| 2 | `template/base/docs/api-surface/**` | **hand-copied** from `src/` | 61 `.fsi`, 7,717 lines |
| 3 | `readiness/surface-baselines/**` | reflection-generated, **committed**, diffed as text | 32 files, 6,738 lines |
| 4 | the published NuGet package | packed from `src/` | — |

**No generator exists between any pair.** Every `.fsi` in (2) carries a header saying *"regenerate when
`$(FsGgGameVersion)` moves"* — and the regenerator was asked for and never written
([#694](https://github.com/FS-GG/FS.GG.Rendering/issues/694)).

### Why a checker cannot substitute for a generator

**[#657](https://github.com/FS-GG/FS.GG.Rendering/issues/657) / [#661](https://github.com/FS-GG/FS.GG.Rendering/issues/661) are the proof, and they are not hypothetical.** The
surface renderer was hand-mirrored into `refresh-surface-baselines.fsx` and `SurfaceAreaTests`. A bug
landed in **both** copies. The gate compared one copy against the other, **agreed with itself, and
stayed green while recording something that was not the public surface.** #661 states the general form:

> *A shared bug is invisible to a comparison between two copies of it.*

The greenness of the gate is the evidence you would otherwise use to check. This failure mode is not
detectable from inside the scheme, at any level of effort, ever.

### Why the doc gate does not converge

A shipped doc that names a symbol the pinned package does not export is a **real** defect class — it
has shipped repeatedly (#550, #591, #592, #598, #619), and no tool in the .NET/F# ecosystem checks for
it. The need is legitimate. The response was to hand-roll a compiler front-end out of regexes:
**six doc-symbol extractors, three markdown-fence engines that disagree about what an F# block is
(#669), six mutually-disagreeing `val` regexes, and two independent package oracles** — ~4,200 lines,
of which `TemplateConsumesPinnedApiTests.fs` alone is **3,581**.

The gate was widened **six times in 48 hours** (#550 → #589 → #591 → #598 → #608 → #611), and **four of
those widenings were each announced as "the last unjudged surface."** It fails in both directions:
fail-open (#654 credited 32 public surfaces to any English sentence using their name; #648 was blind to
`Module.Submodule.member`; #683 unions across packages) and fail-closed (#664 reported a correct doc as
a defect). After #654 was tightened to "cited as code", the collision reappeared *inside code fences*:
a re-mirrored skill defining a local `let describe` falsely credited `Scene.describe`, and commit
`a9e3850c` **deleted a blessed ledger line** to make it green — a one-way door, filed as #692. And
`TemplateConsumesPinnedApiTests.fs:2015` now hardcodes `oracleVersion = "0.9.0"` because the live pin
kept moving underneath the oracle: **the oracle needed its own oracle.**

Each fix replaces one heuristic with a heuristic that has a new hole, because **regexes over English
cannot decide symbol identity** (#695). There is no version of this that converges.

## Decision

**A fact has exactly one home. Where a second copy exists, the gate that compares them is deleted along
with the copy — not improved.**

Four executions. Each is scoped by an epic; each **must land its deletion in the same PR as its
generator**, or it has added a fifth copy and a gate to compare it.

**1 — Compile the docs; do not parse them** ([#695](https://github.com/FS-GG/FS.GG.Rendering/issues/695)).
Extract every F# fence from every shipped skill into a real project; restore it against the **pinned**
package; build it. That is the compiler currently being approximated in regex, and it is *strictly
stronger* — it catches wrong arity and wrong argument types, which no extractor can.

**2 — The surface baselines keep their subject; they lose their second renderer.**
**Epic [#694](https://github.com/FS-GG/FS.GG.Rendering/issues/694) claims ApiCompat subsumes the
6,738-line baseline set and that `apicompat-check.sh:16-17` "acknowledges the overlap and keeps both
anyway." That claim is false, and this ADR does not act on it.** What those lines actually record is a
**deliberate division of labour**: `Microsoft.CodeAnalysis.PublicApiAnalyzers` — the C# half of the org
shared build config — **does not analyze F#**, so for these packables the operative binary detector is
the language-agnostic SDK ApiCompat, *"and the source-level public-surface record **stays** the
committed `.fsi` baselines in `readiness/surface-baselines/`."* The split is registered in the org
coherence registry as `apicompat-publicapi-gate` (Governance spec 088 research D1).

**Source-level surface and packed-artifact compatibility are different subjects.** ApiCompat answers
*"is the shipped assembly a breaking change against the feed?"* The baselines answer *"what is the
public surface of this source tree?"* — a question ApiCompat cannot answer for F# at all. Deleting them
would remove a governance-registered mechanism and leave the question unanswered, and it is a one-way
door.

What #657/#661 actually proved is narrower and still true: **the two hand-copies of the surface
*renderer* were the defect, not the baseline's existence.** That duplication was already closed
(`fix(#661)`: *"state the surface renderer once, so a shared bug cannot hide"*). The residue this ADR
takes as an **open question**, deliberately not decided here: *must the derived record be **committed**,
or can it be derived at gate time from `src/**/*.fsi` by the one renderer?* A record that is generated
by one renderer and diffed against its own committed output is a weaker instance of the pattern this
ADR names — but it is not a duplicate *fact*, and the case for changing it has not been made. It is
carried to #694 to argue on its merits, with the false premise removed.

**3 — Generate the api-surface mirror, or stop shipping it** ([#694](https://github.com/FS-GG/FS.GG.Rendering/issues/694)).
Generate the 61 `.fsi` from the pinned nupkg at scaffold time, or ship them *inside* the package. This
also retires `mirror-pending-release-ledger.txt`, which exists **solely** because `M-MIR/TYPE` (the
mirror must *equal* `src/`) contradicts doc-vs-pin (a doc must not *exceed* the pin) during the window
where `src/` is ahead of the published package. **A ledger that exists to reconcile two rules is
evidence that one of the rules is wrong.** #594 says so in its title: `M-MIR/TYPE` *compels* the #550
bug for any mirrored type that grows, because a `val` can wait for a release and a union case cannot.

**4 — Execute ADR-0014 for the four Game-owned bodies** ([#696](https://github.com/FS-GG/FS.GG.Rendering/issues/696)).
This is **not a new decision.** ADR-0014 §1 already ratified one canonical body, content-addressed, with
one shared materialize-and-verify. What shipped instead was a 458-line CI guard, no regenerator in
either direction, and ~40 commits of hand-copy toil with no downward trend (`fs-gg-persistence`
re-frozen **10** times, `fs-gg-audio` 9, `fs-gg-game-core` 9, `fs-gg-model-swap` 8). #696 is an
*execution* of ADR-0014, and should be argued as one.

### The closing rule

**A governance issue may be closed in exactly three ways:**

1. **The duplicate is deleted** — the fact now has one home.
2. **The checker is deleted** — something else already covers it, named in the PR.
3. **An ADR records why the check is genuinely the cheapest option, and names what would retire it.**

**"Added a gate" is not a close.** This is the operative clause. Every gate is new hand-written surface
that generates its own defects at roughly the rate of the code it polices: #698 landed a widened
skill-refs gate at 15:16 on 2026-07-13; **#733 and #734 — bugs in that gate, one of them fail-open —
were filed within 25 minutes of it.** The loop (audit → find duplicate → add checker → checker is a
duplicate → audit) terminates only if the closing move is removal.

## Consequences

**Deletable, once (1), (3) and (4) land** — an obligation, not an estimate. Every count was taken from
the tree at `889b555`, not inherited from an epic:

| artifact | lines | retired by |
|---|---|---|
| `template/base/docs/api-surface/**` (61 `.fsi`) | 7,717 | (3) |
| `tests/Build.Tests/TemplateConsumesPinnedApiTests.fs` | 3,581 | (1) |
| `tests/Package.Tests/ApiSurfaceMirrorTests.fs` | 1,004 | (3) |
| `scripts/check-frozen-mirrors.fsx` | 458 | (4) |
| `tests/Package.Tests/mirror-pending-release-ledger.txt` | 114 | (3) |
| `tests/Build.Tests/pinned-api-doc-ledger.txt` | 103 | (1) |
| **total** | **~13,000** | |

**What this table used to say, and why it is smaller now.** A draft of this ADR booked a further ~7,800
lines — `readiness/surface-baselines/**` (6,738), `refresh-surface-baselines.fsx` (185),
`SurfaceAreaTests.fs` (244), and all 690 lines of `SurfaceDocCoverageTests.fs` — against Decision (2).
Review found the premise false (see Decision 2) and the S-DOC row double-counted against this
document's own Consequences. **Those rows are removed rather than defended.** An ADR that overstates its
own deletion table is committing the exact sin it was written to stop, and the number that survives
scrutiny is worth more than the number that does not.

**What does *not* die, stated honestly.** S-DOC asks two different questions and only one of them is a
parsing problem. *"Does this doc name something that does not exist?"* is answered by compiling the
fences (1). *"Is every shipped public symbol taught somewhere?"* — **coverage** — is a real question that
survives, and `surface-doc-ledger.txt` (402 lines) survives with it as the list of deliberately-untaught
symbols. But it is then computed from the **symbol table of the compiled doc project** against the
**public surface of the pinned package**, not from regexes over prose — so it stops being a hand-
reconciled casualty of every re-freeze (#692, #649), which is what it is today.

**The trap this ADR exists to prevent.** #694 and #695 propose *building generators*, which is more
code. If the doc-compiler lands and the 3,581-line regex file stays "as a second opinion", **nothing has
been fixed** — there will be five copies instead of four, and a new gate comparing the generator to the
thing it replaced. The deletion is not the cleanup after the work. The deletion **is** the work.

**Budget.** Governance is 27,289 lines against 54,853 of product. That ratio is the metric this ADR is
accountable to, and it must fall.
