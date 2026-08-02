---
name: fs-gg-sdd-authoring-contracts
description: Reference for the load-bearing FS.GG SDD authoring grammars that silently block a stage if mis-formatted — the FR→AC checklist coverage line, the evidence.yml kind/result/synthetic satisfaction rule, the specify --input intent facts, the clarify [AMB:AMB-###] decision-tag resolution rule, the per-stage front-matter required-field sets, and the plan framework:/blocked-on-framework: API-reference grammar. Use when a checklist/evidence/specify/clarify/plan stage blocks unexpectedly or a front-matter block reports "incomplete".
---

# Authoring Contracts (the gating grammars)

Several SDD inputs are **load-bearing**: a small grammar decides whether the tool
accepts what you authored, and a subtly wrong form produces a blocking gate. This
skill is the quick reference; the durable, drift-guarded source is
`docs/reference/authoring-contracts.md` (every example below is run through the
**live parser** on each build by `AuthoringDocsContractTests`, so it cannot drift).

The three body grammars — the checklist **coverage line** (§1), the `evidence.yml`
**satisfaction rule** (§2), and the `specify --input` **intent facts** (§3) — plus
three cross-cutting ones: the **clarify decision-tag** resolution (§4), the
**per-stage front matter** required-field sets (§5), and the plan
**framework-API reference** grammar (§6). If a stage blocks and you
"know" the content is right, it is almost always one of these.

## 1. Checklist coverage line (used by `checklist`)

A functional requirement is **covered** only when a strict-scan parser finds a list
item that leads with `- FR-###:` and carries its acceptance reference **on the same
physical line**:

- literal `- `, then `FR-` + **three or more digits** (case-insensitive), then a
  literal `:`, then prose, with `AC-###` (optionally `US-###`) on the same physical
  line. The scan reads one physical line and does not join continuation lines, so a
  **soft-wrapped** bullet whose `(covers AC-###)` marker wrapped to the next line
  reads as uncovered — keep the whole line unwrapped. Fix a "missing acceptance
  coverage" verdict in `spec.md`, not the regenerated `checklist.md`.

**Accepted:**

```text
- FR-001: W/S move the left paddle. (covers AC-002)
- FR-014: Ball serves toward the loser. (Stories: US-003; Acceptance: AC-009)
```

**Counted but NOT covered** (a loose scan `\bFR-\d{3,}\b` lists it, so it looks
present but establishes no coverage):

```text
**FR-001** W/S move the left paddle. (AC-002)     ← bold id, not a "- FR-001:" item
- FR-001 — moves the paddle (AC-002)              ← no colon after the id
(covers AC-002)                                    ← AC ref on its own line
- FR-001: a long requirement whose marker         ← soft-wrapped: marker on line two
  wrapped to the next line. (covers AC-002)
```

See [[fs-gg-sdd-checklist]].

## 2. `evidence.yml` satisfaction (used by `evidence`/`verify`)

> An obligation is **satisfied** only by a matching declaration whose `result` is
> `pass` **and** whose `synthetic` is `false`.

- `synthetic: true` + `result: pass` → disposition **synthetic**, does not satisfy.
- `result: deferred` (or `kind: deferral`) → accepted deferral, does not satisfy.
- `result: fail | missing | stale | blocked` → not satisfied.

**`kind`:** `implementation · verification · review · generated-view · synthetic ·
deferral · note · missing` (`generatedview` also accepted). **An unrecognized
`kind` silently becomes `verification`** — spell it exactly.
**`result`:** `pass · fail · deferred · missing · stale · advisory · blocked`.

**Satisfies:**

```yaml
schemaVersion: 1
evidence:
  - id: EV001
    kind: verification
    subject: { type: task, id: T001 }
    artifacts: [tests/Product.Tests/InputMapTests.fs]
    result: pass
    synthetic: false
```

**Free-text prose (`notes`, `rationale`): mind the quotes.** `evidence.yml` is the
lifecycle artifact most often machine-generated with prose, so it collects apostrophes and
quotes — and an unescaped one is a **YAML syntax error** that blocks the stage, not a
satisfaction failure. Inside a **single-quoted** scalar, **double** an apostrophe
(`'RM1''s shell'`, not `'RM1's shell'`); inside a **double-quoted** scalar, backslash-escape
an inner double-quote (`"he said \"hi\""`); a plain (unquoted) scalar needs neither. When the
parse fails on a line that carries a quote, `evidence` names the unescaped quote as the likely
cause rather than only echoing the parser's position.

See [[fs-gg-sdd-evidence]].

## 3. `specify --input` intent facts (used by `specify`)

`specify` drafts a spec only when `--input` supplies three labeled facts; an
unlabeled line is read as the user value only, so free prose reports `scope` and
`measurable requirement` missing.

```text
value: A player can rally against a second human at one keyboard.
scope: Two-player local volley; no AI opponent, no online play.
requirement: A rally of 20 consecutive volleys completes without a dropped frame.
```

See [[fs-gg-sdd-specify]].

## 4. Clarify decision-tag resolution (used by `clarify`)

A carried `AMB-###` ambiguity is resolved only when **both** hold:

- its id sits on a `DEC-###` line under `## Decisions` **or** `## Accepted
  Deferrals` — authored as the canonical tagged form `[AMB:AMB-001]` (the bracket
  is a convention; the parser needs only the bare `AMB-001` token on the line);
- it is **not** left as a blocking bullet under `## Remaining Ambiguity` (write a
  `None.`/disclaimer there).

An **answer** under `## Answers` does **not** resolve — resolution is the decision
**tag**, not the answer. And each `DEC-###` may be **declared once**: a line whose
leading id is a `DEC-###` under `## Decisions` or `## Accepted Deferrals` is a
declaration, the two sections are pooled, and a second declaration raises
`duplicateClarificationId`. See [[fs-gg-sdd-clarify]].

## 5. Per-stage front matter (used by every authored stage)

Each stage blocks with an *incomplete/malformed front matter* diagnostic only when
a field it **gates on** is absent; other template fields are **defaulted**.

| Stage | Gating fields | Defaulted (not gating) |
|---|---|---|
| charter | `schemaVersion, workId, title, stage, changeTier, status` | — (strict) |
| specify | `schemaVersion, workId, stage` | `title`, `changeTier`→`tier1`, `status`→`draft` |
| clarify | `schemaVersion, workId, stage, sourceSpec` | `title`, `changeTier`, `status`→`needsAnswers` |
| checklist | `schemaVersion, workId, stage, sourceSpec, sourceClarifications` | `title`, `changeTier`, `status`→`needsReview` |
| plan | `schemaVersion, workId, stage, sourceSpec, sourceClarifications, sourceChecklist` | `title`, `changeTier`, `status`→`planned` |
| tasks (`tasks.yml`) | `schemaVersion` (workId derivable) | `stage`→`Tasks`, `status`→`tasksReady` |
| evidence (`evidence.yml`) | `schemaVersion`, valid `workId` | `status`→`draft` |

- `stage` is the only **closed** vocabulary: `charter · specify · clarify ·
  checklist · plan · tasks · analyze · implement · evidence · verify · ship`.
  `schemaVersion` major must be `1`.
- `changeTier` and `status` are **free strings** — the parser does not validate them
  (`tier1`/`tier2` and the status words are conventions/defaults, not an enforced
  vocabulary); expect no "bad tier"/"bad status" rejection.
- **`Source Snapshot`/`sha256` is not a clarify concept.** It belongs to
  `checklist`/`plan` (and `tasks`/`evidence` `sources[].digest`); the digest is
  **optional**, format-checked (64 hex) only when present, used only for staleness
  detection — a placeholder is silently ignored and a real digest is never required
  to author.

## 6. Framework-API reference (used by `plan`/`analyze`)

A plan may cite a framework package's API in a **resolvable** form (feature 105,
ADR-0004) rather than an un-checked backtick token. On a `## Contract Impact` line a
`framework:` token declares a **use**; on an `## Accepted Deferrals` line a
`blocked-on-framework:` token declares an **absence claim**:

```
- framework: <PackageId>[@<version>]#<symbol>              (a USE, on Contract Impact)
- CR-003 blocked-on-framework: <PackageId>[@<version>]#<symbol>   (an ABSENCE claim, on a deferral)
```

- `<PackageId>` is the NuGet id (e.g. `FS.GG.UI.SkiaViewer`); `#<symbol>` is the
  module-qualified `val`/member (e.g. `SkiaViewer.runAppWithPersistence`); `@<version>`
  is **optional** and defaults to the Central Package Management pin
  (`Directory.Packages*.props`), so a reference never duplicates the pinned version.
- A keyword present with a token that is **not** this grammar (no `#symbol`, empty
  `PackageId`) **blocks** at `plan` with `malformedFrameworkReference` — never a silent
  non-match. (A mis-typed reference reading as "no reference" is exactly the failure this
  defeats, one level up.)
- `analyze` resolves each reference against the pinned package's **committed captured
  surface** — `docs/dependency-surface/<PackageId>/<version>.json`, produced by
  `fsgg-sdd dependency-surface --update` (never a vendored `.fsi` snapshot):
  - a **use** whose symbol is **absent** from the real surface → **blocks**
    (`frameworkApiDangling`), surfaced at plan time as blocked-on-a-framework-change;
  - a **`blocked-on`** deferral whose symbol **is present** → **blocks**
    (`frameworkApiDeferralContradicted`) — the deferral's premise is contradicted;
  - a use present / a deferral absent → passes;
  - no capture committed, or the version cannot be resolved → **advisory**
    (`frameworkApiSurfaceUnavailable`, exit 0) — "could not look" is never a negative
    verdict.
- The parameter-free capture commands are the generated-product lifecycle:
  - after adding a framework reference or changing a package pin, run
    `fsgg-sdd dependency-surface --update`; it discovers targets across `work/**/plan.md`,
    resolves omitted versions from `Directory.Packages*.props`, restores the workspace, and
    commits captures from the real packages;
  - CI runs `fsgg-sdd dependency-surface --check`; a readable authored target with no capture,
    or a capture whose real package surface drifted, blocks until `--update` is committed;
  - an unavailable package remains advisory. The `frameworkApiSurfaceUnavailable` diagnostic
    names the exact targeted update command when a single package needs remediation.

That is what a plain backtick citation cannot give you: a reference the tool resolves
against the **real** package, defeating both a genuinely dangling reference and the
inverse false alarm (a real API mis-read as absent from a stale local view).

## Why these are strict

SDD's doctrine is that **structured artifacts are the machine contract** — the
strict scans exist so coverage and evidence are facts a tool can trust, not prose
a reader has to interpret. The strictness is the feature; these few forms are the
price.

## Regeneration semantics (re-running `checklist`/`tasks`)

`checklist.md` and `tasks.yml` are generated gate artifacts you also author against.
Re-running the stage **re-derives the tool-owned content from current sources and never
re-ingests its own prior output** (feature 082):

- Tool-owned rows are recomputed every run — a `CHK-###` blocking row clears once the
  source covers the FR (coverage lives in `spec.md`, not `checklist.md`); `tasks`
  re-derives the graph so a new plan decision disposition appears instead of the run
  reporting "stale" and doing nothing.
- Authored content is preserved: `checklist`'s `Advisory`/`Lifecycle Notes`, and in
  `tasks.yml` a task's `status`, `owner`, and hand-added **live** disposition refs
  (`requirements`/`decisions`/`sourceIds`, e.g. `decisions: [DEC-001]`) — plus a wholly
  hand-authored task that uniquely covers a live disposition. Stale refs/tasks (sources
  gone, or already derived) are dropped.
- Hand-edited generated rows are reclaimed (overwritten). Unchanged sources → byte-identical
  `noChange`. The only re-run that blocks is the `<!-- fsgg-sdd: unsafe-overwrite -->`
  opt-out, whose diagnostic names the file and command — never guess an `rm`.

See `docs/reference/authoring-contracts.md` → *Regeneration semantics*.

## Related

- [[fs-gg-sdd-checklist]], [[fs-gg-sdd-evidence]], [[fs-gg-sdd-specify]],
  [[fs-gg-sdd-lifecycle]].

## Sources

- `docs/reference/authoring-contracts.md` (whole file; drift-guarded by
  `AuthoringDocsContractTests`).
