# Readiness evidence — Feature 221 (Headless Image Evidence Path)

This directory holds the readiness ledger for feature 221. `specs/*/readiness/` is gitignored by
default; this feature is allowlisted in `.gitignore` (`!specs/221-headless-image-evidence/readiness/`
+ `/**`) so the Tier-1 evidence is committed and reviewable. `git check-ignore` proof is recorded in
`closeout.md`.

| Artifact | Task | Proves |
|---|---|---|
| `baseline.md` | T002/T026 | No-regression baseline of the affected test projects (pre- and post-change). |
| `cpu-raster-sanity.md` | T003 | The existing no-`GRContext` CPU raster path builds and produces a non-blank PNG headless. |
| `root-cause-map.md` | T004 | Confirms the three root-cause facts and maps each FR to the fix that closes it. |
| `smoke-run.md` | T005 | Early live check of the GL `OffscreenReadback` route's behaviour (environment-limited here). |
| `fixture.md` | T006 | Pins the canonical "representative game scene" fixture identity (constructor + size). |
| `degradation.md` | T022 | Enumerates CPU-vs-GL fidelity gaps and how each is disclosed (not silently dropped). |
| `fr007-diff.md` | T025 | Confirms `Hash`/metadata/evidence-file surfaces are unchanged (non-regression). |
| `surface-baseline.md` | T024 | Records why the public-surface (`.fsi`) baseline needs no new type (Tier-1 obligation). |
| `closeout.md` | T027 | SC-001..SC-005 + Edge-Case validation and the final closeout summary. |

Real committable pixel/timing proofs live in the sibling `../evidence/` directory.
