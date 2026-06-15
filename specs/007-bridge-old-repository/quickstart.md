# Quickstart: Validate the Bridge (Stage R7)

R7 ships documentation, so "validation" is three mechanical checks that prove the handoff is
**complete**, **internally linked**, and **non-interfering** with the product. All run headless in the
default local tier (shell + git only — no build, no DISPLAY/GL). Run them from the repo root after the
bridge artifacts exist.

## Prerequisites

- Branch `007-bridge-old-repository` checked out.
- Bridge artifacts present: `docs/bridge/README.md`, `docs/bridge/old-repo-redirect.md`,
  `docs/bridge/package-identity-migration.md`; updated `PROVENANCE.md`; `README.md` link added.

## Check 1 — Link integrity (FR-009 / SC-005)

Every in-repo Markdown link in the bridge artifacts must resolve to an existing path.

- **Scope**: links in `docs/bridge/*.md`, the `PROVENANCE.md` cross-references, and the new
  `README.md` bridge link. External `http(s)://` links (GitHub/NuGet) are out of scope.
- **Procedure**: extract relative link targets from those files; for each, resolve it relative to the
  containing file and assert the target exists.
- **Expected**: zero unresolved in-repo links.
- **Discriminating power**: introduce a deliberately broken relative link → the check MUST report it.

## Check 2 — Provenance coverage (FR-003 / SC-002)

Every imported top-level area is accounted for in `PROVENANCE.md`.

- **Procedure**: for each area in the `IMPORTED` set defined in
  [`contracts/provenance-coverage.md`](./contracts/provenance-coverage.md), confirm a path-map row
  covers it **or** it is listed under *Named gaps*.
- **Expected**: zero areas neither mapped nor named.
- **Discriminating power**: remove a path-map row for an area that has no named-gap entry → the check
  MUST flag that area as unaccounted.

## Check 3 — No-product-change guard (FR-010 / SC-007)

R7 changes documentation only.

- **Procedure**: list changed files on the branch vs. `main`:

  ```sh
  git fetch origin main 2>/dev/null; git diff --name-only main...HEAD
  ```

- **Expected (allowlist)**: only `docs/bridge/**`, `PROVENANCE.md`, `README.md`, `CLAUDE.md` (SpecKit
  marker), and `specs/007-bridge-old-repository/**`.
- **Forbidden (must be empty)**: any `src/**`, `tests/**` `.fs`/`.fsi`, `*.props`, `*.slnx`,
  `template/**`, `.template.config/**`, `.template.package/**`.
- **Discriminating power**: touch any `src/**` file → the guard MUST fail.

## Check 4 — No-overclaim / no-rebrand grep (FR-006, FR-011 / SC-003, SC-006)

Honesty guards over the wording.

- **Recorded-action present**: `docs/bridge/old-repo-redirect.md` contains a "NOT yet applied" recorded
  action header; no bridge artifact describes the redirect as already applied.
- **No rebrand bleed**: no bridge artifact instructs a rename or introduces a new package ID (e.g.,
  `FS.GG.UI.*`); the migration note links decision `0001` and states identity is retained.
- **Expected**: recorded-action header found; zero "applied" claims; zero rename instructions.

## Done when

- Checks 1–4 pass, **and** each has been shown to fail under its discriminating-power perturbation
  (Constitution Principle V — a check that cannot go red proves nothing).
- The bridge hub is reachable in one hop from `README.md` (SC-008).

Detailed content requirements live in the contracts
([`bridge-hub.md`](./contracts/bridge-hub.md), [`old-repo-redirect.md`](./contracts/old-repo-redirect.md),
[`provenance-coverage.md`](./contracts/provenance-coverage.md)) and [`data-model.md`](./data-model.md);
this guide is the run/validate surface only.
