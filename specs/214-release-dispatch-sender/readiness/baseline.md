# Readiness Baseline — Feature 214 (Release → Templates Dispatch Sender)

**Date**: 2026-06-28 | **Branch**: `214-release-dispatch-sender`

## Scope (adapted, honest)

This feature adds **no F# code** and touches no module surface, so the `.NET *.Tests.fsproj`
suite is **not** the regression surface. The regression anchor (FR-008) is `release.yml`
byte-equality; the test surface is `actionlint` + the `DRY_RUN=1` dispatch-script harness.

## FR-008 regression anchor (T001)

`release.yml` content hash recorded pre-change in
[`release-yml-baseline.txt`](./release-yml-baseline.txt):

```
d9cc1ae5934f770a6ac2e479c969c38994f30436
```

Re-confirmed unchanged in T018 (see below).

## Toolchain (T002 / T003)

| Tool | Version | Source | Notes |
|------|---------|--------|-------|
| `actionlint` | **1.7.7** | prebuilt release binary (`actionlint_1.7.7_linux_amd64.tar.gz`) | Pinned, not `@latest`, for reproducible lint evidence. `go` is unavailable on this box, so the `go install …@v1.7.7` form from quickstart could not be used; the **identical pinned version** was fetched from the GitHub release page instead. Installed to `~/.local/bin/actionlint`. |
| `gh` | 2.95.0 (2026-06-17) | preinstalled | Used for the `gh api … /dispatches` send (live path only). |
| `bash` | GNU bash 5.3.15(1) | preinstalled | POSIX driver for the dispatch + harness scripts. |

`actionlint -version`:

```
1.7.7
installed by downloading from release page
built with go1.23.4 compiler for linux/amd64
```

## T018 — FR-008 regression re-confirmation (Polish)

`git diff --stat origin/main -- .github/workflows/release.yml` → **no output** (unchanged).
Content hash re-checked against the T001 anchor — **match**. `release.yml` gating
(Package.Tests, template-instantiation tests, Feature 212 stock-root build/test/run) is intact.

## Readiness evidence is allowlisted (Feature 168 rule)

`specs/*/readiness/` is gitignored by default. Before staging this feature's evidence, the
`.gitignore` allowlist was extended with:

```
!specs/214-release-dispatch-sender/readiness/
!specs/214-release-dispatch-sender/readiness/**
```

`git check-ignore -q <file>` then returns non-zero (NOT ignored) for every evidence file in this
directory — confirmed for `baseline.md`, `release-yml-baseline.txt`, `contract-pin.md`,
`dry-run-us1.txt`, `dry-run-us2.txt`, `actionlint.txt`, `quickstart-run.md`,
`deferred-live-check.md`. So the evidence is intentionally tracked, not slipping in past the ignore.
