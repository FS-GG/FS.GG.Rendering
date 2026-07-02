# Research: derive the gate test loop from the slnx

## R1 — The orphan set (which projects run in no cadence)

`FS.GG.Rendering.slnx` holds 16 `*.Tests` projects. `gate.yml` deterministic tier ran 8
(`Scene Layout KeyboardInput Elmish Controls Diagnostics Testing Lib`); the GL step ran 2
(`SkiaViewer Smoke`). The remaining **six** — `Build.Tests`, `Canvas.Tests`, `Symbology.Tests`,
`Symbology.Render.Tests`, `SymbologyBoard.Tests`, `Rendering.Harness.Tests` — appeared in no
workflow (`gate.yml`, `release.yml`, `capability.yml`) by name. `Package.Tests` and `TestSupport`
are *not* slnx members (release-only exe test / shared helper lib), so they are correctly outside the
slnx-derived loop.

## R2 — Capability classification (do any orphans need GL?)

All six are capability `none` (deterministic, headless-safe):

| project | capability | what it tests |
|---|---|---|
| Build.Tests | none | Build-evidence engine over on-disk `readiness/` fixtures; pure filesystem, no Skia. |
| Canvas.Tests | none | Deterministic canvas simulation → reproducible Scene/fingerprints ("no GL, no wall clock"). |
| Symbology.Tests | none | Pure symbology grammar/channels/codec/labels/motion/legibility (`Scene.measureGlyphRun`). |
| Symbology.Render.Tests | none | Rasterises to PNG via `Render.toPng` → `ReferenceRendering.run` = raster `SKSurface.Create(info)` (no GRContext/GL/X). |
| SymbologyBoard.Tests | none | `samples/SymbologyBoard` build + linter grammar-independence checks; pure logic. |
| Rendering.Harness.Tests | none | Harness evidence/readiness/lane/skill-parity logic; GL/x11 appear only as data strings; probe/uinput steps honest-skip headless. |

Decisive evidence: `src/SkiaViewer/ReferenceRendering.fs:123,140-141` — `SKSurface.Create(info)`,
documented "no `GRContext` … no GPU/GL/X/display". The real GL path (`GRContext.CreateGl`) lives in
`src/SkiaViewer/Host/OpenGl.fs` and is never reached by these tests. Confirmed by running all six
headless: five green outright; `Rendering.Harness.Tests` 211/212 with one real (not environmental)
red — see R4.

## R3 — Design: derive-from-slnx with a single GL source of truth

`gate.yml` gains a job-level `env: GL_TEST_PROJECTS: "SkiaViewer Smoke"`. The deterministic tier
iterates `grep -oE 'tests/[^"]+\.Tests\.fsproj' FS.GG.Rendering.slnx`, skipping any project in
`GL_TEST_PROJECTS`; the GL step iterates `$GL_TEST_PROJECTS`. One list, two consumers → the union is
the slnx test set by construction. New test projects join the deterministic tier automatically;
a project that genuinely needs GL fails **loudly** in the deterministic tier (never a silent skip)
until it is added to `GL_TEST_PROJECTS`.

## R4 — The latent harness red (why wiring mattered)

`Feature168 SkillInventoryTests` "repository parity has no unresolved findings" expected `Passed`,
got `WarningStatus`: one `Warning`/`GuidanceRuleGap` finding, skill `fs-gg-samples`, surface
`template-canonical`, rule `package-pin-drift` — "guidance rule is partial." The rule
(`tools/Rendering.Harness/SkillParity.fs:342`) requires four reference groups; the template skill
(`template/fragments/samples/skill/SKILL.md`) had `FS.GG.UI.` (G1), `package-feed` (G2, an OR-group
already satisfied), `package pins` (G3) but **not** `local feed` (G4). The minimal fix is a one-word
change — "the intended feed" → "the local feed" — covering G4 while leaving G2 satisfied by
`package-feed`.

**Do NOT copy the framework siblings verbatim.** `src/Controls/skill/SKILL.md:281` phrases G2 as
"use `scripts/refresh-local-feed-and-samples.fsx` or `package-feed` … against the local feed", but
that script path is framework-repo-only. `fs-gg-samples` is a **product-emitted** template skill, and
`Package.Tests` G-NODANGLE fails if a product skill body references a path that will not resolve in a
generated product (the Feature 225 de-leak). So the fix adds "local feed" but **not** the script path.
The manifest (`template/skill-manifest/skill-manifest.json`) hashes the template fragment body
(`scripts/generate-skill-manifest.fsx:42,61`), so it must be regenerated.

## R5 — Doc drift (both directions)

`docs/ci/cadence-map.md` §2 and `docs/validation/validation-set.md` "Local inner loop" both still
listed retired `Color.Tests`/`Input.Tests` and omitted `Diagnostics.Tests` + the six orphans. The
gate coverage invariant is now machine-enforced (R3 + the meta-guard), so the docs are refreshed to
the real set: gate-run test projects = every slnx `*.Tests` (14 deterministic + `SkiaViewer`/`Smoke`
GL). Release-only `Package.Tests` / template `Product.Tests` remain outside the slnx and outside the
gate.

## R6 — Meta-guard home

`tests/Build.Tests` already parses the slnx (`RestoreLockTests.fs`) and is one of the six orphans,
so wiring it in makes the new `CadenceCoverageTests.fs` self-validating in the same gate lane it
guards. Build.Tests is an Expecto exe project; the guard is straight filesystem assertions over the
committed slnx, `gate.yml`, and the two cadence docs (no mocks).
