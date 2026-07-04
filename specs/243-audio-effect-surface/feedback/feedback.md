# Feature 243 — fs-gg / Spec Kit feedback (T019)

Captured during implementation of the `fs-gg-audio` request surface + product skill. Severity noted.

## Process friction

- **The template engine cached the folder-installed `template.json` — the live scaffold check
  silently under-materialized until `--force` reinstall (high).** After adding the `fs-gg-audio`
  copy block, the FIRST `dotnet new fs-gg-ui --profile game` did **not** emit the skill even though
  `fs-gg-game-core` did and every deterministic gate (manifest regen, `--check`, Package.Tests) was
  green. Only `dotnet new install . --force` refreshed the engine's cached config. This is exactly
  the Feature 175/228 failure mode: deterministic tests pass while the real scaffold is wrong. The
  standing "live scaffold check" (T005/T016) caught it — but the fix (`--force` reinstall) is
  non-obvious and belongs in the task guidance for any skill/template-copy-block change. **Candidate:
  a helper/target that reinstalls the template before the live scaffold assertion.**

- **Four Package.Tests hardcode the skill inventory as exact counts + literal catalog lists
  (medium).** Adding one real skill turned six assertions red (Feature219 count 10→11 + per-profile
  matrix; Feature204 count 11→12; Feature231 + Feature238 declared-catalog lists). These are correct
  guards, but the "expected exactly N" + duplicated literal lists mean every skill addition edits ~4
  test files in lockstep with the generator catalog and `template.json`. **Candidate: derive the
  expected inventory from a single source (the generator catalog) instead of re-declaring it per
  test**, so the count/list drift can't happen and a skill add touches one place.

- **The generator catalog is a hand-maintained literal, separate from `template.json` and the
  wrapper pair (medium).** Shipping one skill required edits in five coordinated places:
  `template/product-skills/<id>/SKILL.md`, the `.agents` + `.claude` wrapper pair, the
  `.template.config/template.json` copy block, and the `scripts/generate-skill-manifest.fsx` catalog
  literal — with no single "add a skill" affordance. Parity is enforced *after the fact* by
  SkillParity rather than generated. **Candidate: an "add-product-skill" scaffold/target** that emits
  all five in one step from the canonical body.

## What went well

- **The effects-as-values discipline made audio a near-trivial, honest fit.** Modelling sound as a
  pure `AudioEffect` value + a record-only interpreter meant US1+US2 were fully testable with no
  device, no new dependency, and no SkiaViewer churn — and the deferred real backend needs no surface
  change. The constitution's Principle IV/VI pointed straight at the right shape.

- **Folding into `FS.GG.UI.Canvas` (skill-only, mirroring game-core) avoided a whole package's worth
  of ceremony** (BOM, release matrix, 38 lockfiles) and rode the existing `game/sample-pack` gate, so
  the generated product picked up the surface with zero new `Product.fsproj` reference.

- **The `refresh-surface-baselines.fsx` generator is the honest path** — it reflected the real
  exported surface (including DU nested case types) so the baseline matched reality without hand-editing.
