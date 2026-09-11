# SVG-SCENE-02.5 model and complete-browser qualification

Date: 2026-09-12

Rendering base: `3a94881fc01845cc9d8faf9f413db214df94326f`

## Installed model and real reducer replay

The public `FS.GG.SDD.Cli` 1.7.0 package installed from NuGet.org, provisioned the exact qualified
Quint 0.32.0 and `lmt` objects, and authored and inspected the profile twice with network isolation.
Both offline outputs agreed. Exact Quint typecheck/tests and deterministic bounded runs passed, and stale
range and action bindings were refused. The finite authority covers retained state and document-control
behavior; native SVG geometry, font loading, browser timing, accessibility, and rendering remain qualified
by their respective runtime observations rather than being collapsed into the model result. The checked-in
[model receipt](../../readiness/svg-scene-02-5/model-qualification.json) binds the public package, exact
tool hashes, seeds, model evidence tree, and both corpus hashes.

The original retained corpus remains byte-for-byte at 192 transitions. The additive document-interaction
corpus remains a distinct 192-transition amendment, preserving the `192 + 192` relationship established by
SVG-SCENE-02.4. Isolated locally packed `FS.GG.UI.Scene 0.29.0-preview.1` consumers replay both corpora
through the real .NET 10 and Fable/Node reducers with matching projected results. Mutation controls for
wrong order, stale-revision acceptance, invalid-reference acceptance, lost capture, and non-atomic invalid
replacement are killed at the first observed divergence in both runtimes. The checked-in
[replay receipt](../../readiness/svg-scene-02-5/reducer-replay.json) records all ten runtime/mutant
coordinates and the matching projection hashes. This is a typed document contract and makes no arbitrary
SVG-import claim.

## Complete automated browser surface

The isolated packed browser gallery runs the same declared functional and visual contract in Chromium,
Firefox, and WebKit (observed versions 151.0.7922.34, 153.0, and 26.5 respectively). Each family covers the selected definitions and paints, standalone export/reload with
local references, analytic geometry and DOM hit agreement, functional selection and retained replacement,
per-browser interior-color references with a per-channel tolerance of 28, viewport resize, DPR 1 and 2,
touch emulation, narrow reflow, and the declared 400%-equivalent layout viewport. Individual family results
remain separate in the aggregate [browser receipt](../../readiness/svg-scene-02-5/browser-observations.json),
so an unavailable or failed family cannot be counted as a pass.
The hosted and assistive-desktop package builds have distinct environment-specific package-set digests;
the aggregate classifies them separately and links them through the identical qualified source digest
`8371647a14d4622cec4a96915ca30dc52f422cd4b8ca635c0688f06005d2c43f` rather than claiming byte identity.

SVG symbol instances now carry an invisible, accessibility-hidden viewport hit proxy inside their declared
transform, clip, mask, and semantic wrapper. This makes the explicit masked-transparency policy portable:
masked symbol geometry remains targetable while clipped-out geometry does not. The proxy contributes no
paint, and isolated export/reload retains the selected visual samples and local references. Candidate
packages stay local and the CI path installs all three pinned Playwright browser families; its historical
check name remains stable for branch-protection compatibility.

## Actual assistive-technology observation

The accessible desktop journey was exercised with a real Orca 50.2 process over AT-SPI2, not a browser
accessibility-tree or DOM substitute. Orca and headed Chromium ran in an isolated D-Bus/Xvfb desktop session
with renderer accessibility enabled. Orca's own debug speech stream announced the SVG foundation scene,
Alpha unit, Beta unit, toggle states, document loading, and browse/focus transitions. The driven journey
selected Alpha and Beta through the equivalent native controls and Alpha through the SVG keyboard route;
all three observations resolved to the same reducer identity. As a negative control, the non-selectable
decoration remained outside keyboard focus. The checked-in
[Orca receipt](../../readiness/svg-scene-02-5/orca-observation.json) retains the exact utterance stream,
journey states, transport, environment, and the explicit non-substitution claim. Earlier attempts missing an
announcement were reported as failures and were not folded into the final pass.

## Boundary

This qualifies the SVG-SCENE-02.5 model, replay, automated browser, and accessible desktop dimensions. Touch
is browser emulation as required; it is not a physical-device or mobile-GPU claim. Geometry/font/browser
timing retain their native analytic and runtime proofs. Rendering remains independent of Game. Candidate
packages were neither published nor activated by default, no complete M5 claim is made, and S.I.R. was not
accessed.
