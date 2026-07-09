# Spec lifecycle

`specs/` is a build journal. It has real archaeological value — but a reader (human or agent)
opening a spec needs to know, in one line, whether it describes **current behaviour** or behaviour
that was replaced eighteen features ago. Before feature 187 nothing carried that signal: 150 of the
155 `spec.md` files read `**Status**: Draft`, including specs whose implementation had shipped and
whose packages had been released.

This document fixes the vocabulary and the one gate that keeps it honest.

## The `Status` line

Every `specs/<feature>/spec.md` carries exactly one line of the form:

```
**Status**: <value>
```

`<value>` is one of:

| Value | Meaning |
|---|---|
| `Draft` | Specified, not yet fully implemented. The starting value; `.specify/templates/spec-template.md` emits it. |
| `Shipped` | The implementation merged to `main`. The spec describes behaviour the repo has. |
| `Superseded by #N` | Replaced by a later feature. `#N` is the GitHub issue that replaced it. |
| `Abandoned` | Specified, deliberately not built. |

`Final` was an earlier, undocumented spelling of `Shipped` on four specs (091, 093, 095, 096). It is
retired — `Shipped` is the only spelling.

## The gate

`tests/Build.Tests/SpecLifecycleTests.fs` asserts, on every PR:

1. Every `spec.md` carries exactly one `**Status**:` line, and its value is from the table above.
   A typo or a new coinage fails loudly rather than rotting silently.
2. **No spec whose `tasks.md` is fully checked may still be `Draft`.** A task list with at least one
   `- [x]` and zero `- [ ]` means the work finished; a `Draft` on top of it is stale metadata. This
   is the "`Draft` outlived its merged implementation" check.
3. `Superseded by #N` names a positive issue number.

### What the gate deliberately does not catch

The signal in (2) is **sufficient, not necessary**. A spec can ship with task boxes left unticked —
091 was marked `Final` while still carrying one `- [ ]`. Roughly twenty specs are in that shape
(e.g. 147, 148, 150), and the gate leaves them `Draft` rather than guessing.

This is a deliberate one-directional gate: it never flags a spec that did not ship, so it can be
trusted, and it closes the class **going forward** — any feature that finishes its task list must
declare a terminal status. Reclassifying the historical residual is a per-spec judgement call and is
not something a filesystem check can make. Set those by hand as you touch them.

## Regenerable readiness evidence

Per-scenario readiness ledgers are **regenerable per-run output**, not durable fixtures, and are
ignored by role (ADR-0018) rather than deleted periodically. See the `specs/*/readiness/promotion/`
rule in `.gitignore`.

The distinction that decides whether a readiness tree may be ignored is **who reads it**:

- Read back from disk by a test (`File.ReadAllText` + asserts) → **durable pin**, must stay committed,
  because a clean checkout without it fails.
- Asserted only in-memory against the emitter that produces it → **regenerable**, ignore it.

Feature 159's `readiness/promotion/**` was 65 committed files of the second kind: the tests that
cover it (`Feature159PromotionEvidenceTests`, `Feature159ReadinessPackageTests`) render the reports
from `Rendering.Harness` and assert on the rendered strings — they never open the committed copies.
Its `fsi/**` transcripts and root `*.md` ledgers *are* read from disk, and stay pinned.

Regenerate the ignored trees with:

```sh
scripts/emit-harness-readiness.sh <target-dir>
```
