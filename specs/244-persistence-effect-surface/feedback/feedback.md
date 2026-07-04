# Feature 244 — fs-gg / Spec Kit feedback (T019)

Captured during implementation of the `fs-gg-persistence` request surface + product skill. Severity
noted. This feature is the direct analog of Feature 243 (audio), so its friction is largely the same
friction 243 already flagged — which is itself a signal.

## Process friction

- **The N+1th skill still edits the same 4 hardcoded test fixtures in lockstep (medium, RECURRING
  from 243).** Adding one real skill turned six Package.Tests assertions red again: Feature219 count
  11→12 + the per-profile matrix, Feature204 GV-2 count 12→13, and the declared-catalog lists in
  Feature231 + Feature238. These are correct guards, but the "expected exactly N" counters + the
  literal catalog lists duplicated across the generator and two test files mean each skill add is a
  ~5-file coordinated edit (generator catalog, template.json block, wrapper pair, +4 fixtures). 243's
  feedback already raised this as a candidate; 244 is the second consecutive feature to pay it.
  **Candidate (reaffirmed): derive the expected inventory from the single generator catalog** so a
  skill add touches one place and count/list drift becomes impossible.

- **The generator catalog is a hand-maintained literal separate from template.json + the wrapper
  pair (medium, RECURRING from 243).** Shipping one skill required coordinated edits in five places
  with no single "add a skill" affordance; parity is enforced after the fact by SkillParity rather
  than generated. **Candidate (reaffirmed): an "add-product-skill" scaffold/target** that emits the
  canonical body stub, the wrapper pair, the template.json copy block, and the catalog row together.

- **Stale-template-cache trap did NOT recur this run, but remains latent (info).** 243 reported the
  engine serving a cached `template.json` so the first live scaffold under-materialized until
  `dotnet new install . --force`. This run the first `dotnet new` already emitted the new skill with
  the correct current sha256 (the install resolved the live repo path), and a `--force` reinstall
  re-confirmed it. So the trap is environment/timing-dependent, not deterministic — which is exactly
  why the standing live-scaffold-check + a `--force` reinstall belief-check (done here) stays
  mandatory rather than optional.

## What went smoothly (confirmed-good, worth keeping)

- **Mirroring the 243 audio surface wholesale was the right call (positive).** Reusing audio's
  placement (module in dependency-light Canvas, skill-only, existing package gate), evidence model
  (record-only interpreter → pure evidence value), and skill-wiring mechanics made the design phase
  nearly mechanical and kept the change small and coherent. The effects-as-values pattern generalized
  cleanly to a request type that carries an *opaque, product-owned payload* — the framework never
  parsing the payload is the persistence analog of "symbology keeps per-game stat mapping out."

- **The `.fsi`-first + "Public contract exposed by this FS.GG.UI package" doc convention + surface
  baseline regen was frictionless (positive).** One `refresh-surface-baselines.fsx` run produced a
  clean 10-row delta; the SurfaceAreaTests gate then held it.

## Generalizable-code candidates

- The record-only interpreter shape (`empty`/`record`/`interpret` fold over a request DU, normalizing
  carried fields and producing an ordered evidence record) is now duplicated across Audio (243) and
  Persistence (244). If a third request surface lands, consider a tiny shared `RecordOnly` helper or a
  documented pattern note — but only then; two is not yet a library.
