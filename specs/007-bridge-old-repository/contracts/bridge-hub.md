# Contract: Bridge hub document

**Artifact**: `docs/bridge/README.md` · **Satisfies**: FR-001, FR-002, FR-007, FR-008, FR-012 ·
**SC**: SC-004, SC-008

The bridge hub is the canonical handoff entry point. It MUST contain the following sections, in any
order that reads well, each meeting its acceptance condition.

## Required sections

| Section | MUST contain | Acceptance condition |
|---|---|---|
| Canonical home | A statement that `FS.GG.Rendering` is the canonical home of the rendering product; the source repo `EHotwagner/FS-Skia-UI`; the pinned import commit `f759f399`. | A reader learns the home + source + commit without leaving the doc. |
| What moved | A one-paragraph summary of imported areas **and a link to `../../PROVENANCE.md`** as the authoritative lineage. | The path map is NOT restated here; lineage is a link (FR-002). |
| Directional policy | New rendering work opens in this repo; the old repo receives only bridge / archive / provenance / emergency-migration changes; governance experiments stay out of rendering work. | Answers "where does new work go?" and "what may change in the old repo?" unambiguously (SC-004). |
| Archive note | The old repo's specs, reports, and readiness artifacts are archive-only history, not a second source of truth. | A reader is told the old repo's history is read-only reference. |
| Identity status | One line: identity retained as `FS.Skia.UI.*`; rename deferred to R8; link to `package-identity-migration.md`. | No implication a rename has occurred (SC-003 supportive). |
| Links | Working links to `old-repo-redirect.md`, `package-identity-migration.md`, `../../PROVENANCE.md`, `../product/decisions/0001-package-identity.md`, and the org profile `https://github.com/FS-GG/.github`. | Every in-repo link resolves (FR-009 / SC-005). |

## Prohibited

- Restating the `PROVENANCE.md` path map (creates a drift-prone second copy).
- Any rebrand instruction or new package ID (that is R8).
- Any claim that the old-repo redirect has been applied.

## Discoverability (FR-012 / SC-008)

`README.md` (repo root) MUST link to `docs/bridge/README.md` so the hub is reachable in one hop from
the repo's entry point.
