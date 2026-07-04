# Feature 240 — fs-gg / Spec Kit feedback (T017)

Captured during implementation of the `fs-gg-game-core` product skill. Severity noted per item.

## Process friction

- **A "docs-only" skill quietly became a packaging change (high).** The spec/plan scoped this as a
  SKILL.md + manifest entry, assuming "the library surface is fixed." That held for `Geometry` (in
  `FS.GG.UI.Scene`, already referenced) but **not** for `Rng`/`FixedStep` (in `FS.GG.UI.Canvas`, which
  no generated product references or pins). A product skill that advises an API the scaffold can't
  compile is a defect, so the feature had to grow to wire Canvas into the `game`/`sample-pack` template.
  The tell was only found by asking "can a scaffolded product actually `open` this?" — worth a standing
  planning check for any skill/doc feature: **does a fresh product of each targeted profile reference the
  package the guidance names?**

- **The packed api-surface drifts silently from `src/**/*.fsi` (medium).** `template/base/docs/api-surface/**`
  is copied verbatim into every product as "the authoritative contract surface" (Feature 060), but nothing
  regenerates or parity-checks it against `src/`. Feature 239 shipped `Geometry`/`Rng`/`FixedStep` in
  `src/` and updated `product.md` prose, yet the packed `Scene.fsi` still lacked `Geometry` and there was
  no `Canvas/` surface at all. A skill pointing at `docs/api-surface/…` would have dangled.

- **Skill count is encoded in five+ Package.Tests that must move in lockstep (medium).** Adding one
  product skill required edits to Feature231/238 (catalog 12→13), Feature219 (rows + source count 9→10),
  Feature204 (framework-source count 9→10), Feature225 (product set 9→10), and Feature209 (pin manifest
  11→12). None of these is discoverable from the others; a grep for the literal counts was the only way to
  find Feature204. Easy to miss one and get a late red.

## Generalizable-code candidates

- **An api-surface regenerator + parity gate (high).** A script that emits `template/base/docs/api-surface/<Pkg>/*.fsi`
  from `src/<Pkg>/*.fsi` (curating to the packed house style) plus a test asserting the packed docs match
  the `src` public surface would have caught the Feature-239 doc gap automatically and made this feature's
  T002c mechanical instead of hand-copied.

- **A single source of truth for "which packages each profile references" (medium).** The profile→package
  matrix is duplicated between `Directory.Packages.props`, `Product.fsproj`, and asserted in
  `Feature209.templateExpected` — adding Canvas touched all three by hand. A generated/derived matrix would
  remove the drift surface.

## What went well

- The `template.json` product-skill source is the single driver of both the spec-kit and sdd emission
  lanes (Feature219 re-derives sdd emission from it), so the skill wiring itself was genuinely one source
  + one generator tuple, exactly as the pattern promises.
- The manifest generator's `--check` + additive-diff made the 12→13 change provably non-disruptive.
