# Producer-defect record — shared-build-config XML comment (Feature 213)

## What happened

Adopting the canonical `Directory.Build.props` initially failed every restore/build with
`MSB4024` — its XML header comment contained `` `--check` `` and `--` is illegal inside an XML
comment (XML 1.0 §2.5). Cascades to `NETSDK1013` / `Invalid framework identifier ''`.

## It was already fixed upstream

The producer (`FS-GG/.github`) had **independently discovered and fixed** this on `main` before this
adoption finished:

- Commit `b00433c` — PR **#30**, **closes #29**, surfaced by **FS.GG.Templates#16** (the first real
  consumer). Identical reword (`` `--check` `` → `check mode`) **and** a hardening of
  `sync-build-config.sh` to assert source `.props` XML well-formedness (xmllint / python3 fallback)
  before distributing or drift-checking — closing the gap that a byte-`diff` `--check` cannot see.

## Why the local adoption still hit it

The local `../.github` checkout was on a **pre-fix branch** (`registry-fsgg-contracts-1.0.0`; the
props file there is last touched by `236c157`, the broken version), so the adoption hit the broken
file before the `main` fix was noticed. I edited the local checkout to the fixed wording (coincidentally
identical to `b00433c`) and re-synced.

## Net result — coherent, not forked

Rendering's three managed files (`Directory.Build.props`, `Directory.Packages.props`,
`.config/dotnet-tools.json`) are **byte-identical to producer `main`** (post-fix b00433c) — verified by
`diff` against `git show main:dist/dotnet/...`. The adoption is drift-clean against the source of truth
and unforked.

## Coordination cleanup

- I opened a duplicate cross-repo issue **FS-GG/.github#31**; closed it as a duplicate of **#29**
  (already resolved by **#30**). No further producer action is required.
- The squash-merge commit message on `main` references `#31` and says "fixed at source" — superseded by
  this record: the authoritative fix is **#29 / PR #30 (`b00433c`)**; locally I only re-synced a stale
  checkout to the already-published wording.
