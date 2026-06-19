---
phase: implement
date: 2026-06-19
severity: major
---

## Process friction

The implementation report was written before the later manual interactive review, so the
initial feedback did not capture several user-visible defects: pointer input felt delayed,
slider value changes were not obvious or working, keyboard behavior was not visibly proven,
and the light theme screenshots exposed black/transparent regions plus a primary-blue
navigation rail that did not match Ant Design. What would have helped is a required
post-implementation live smoke pass that exercises pointer, keyboard, and value-changing
controls before the report is considered complete.

The package-consuming sample loop also added friction. Framework fixes in `src/Controls` and
`src/Controls.Elmish` did not affect SecondAntShowcase until the local packages were repacked,
NuGet caches were cleared, and the sample was rebuilt. A single local-feed refresh command for
"pack changed framework projects and rebuild this sample" would make this much less error
prone.

## Generalizable code

Skill family/topic: FS.GG.UI retained interaction routing and visual readiness.

Candidate helpers:

- A framework-owned opaque viewport composition helper, now prototyped as
  `ControlInternals.sceneWithViewportBackground`, should be kept as a formal render contract
  so full and retained rendering cannot diverge on root background semantics.
- A value-control pointer binding helper should map pointer coordinates to authored
  `changed` payloads for slider-like controls. The current slider-specific logic in
  `Controls.Elmish` is useful, but the pattern should become reusable for range, rating,
  numeric, and date/time controls.
- A visual-readiness alpha/background assertion helper should inspect generated PNGs and fail
  when a full viewport is transparent or visually dependent on the image viewer background.
- A keyboard evidence matrix helper should list representative focus and keyboard activation
  expectations by control family.

## Skill gaps

Additional skills that would have helped:

- Ant visual audit skill: compare screenshots against Ant design patterns and flag filled
  primary-button side navigation, missing root backgrounds, poor selected-state treatment, and
  one-off palettes.
- Generated product interaction audit skill: drive pointer click, pointer drag, keyboard
  activation, and value controls through retained routing and report which authored bindings
  fired.
- Package-consuming sample loop skill: repack local framework packages, clear only required
  caches, rebuild the consuming sample, and verify that the consuming app is using the newly
  packed package timestamps.

## Research links

Local Ant guidance was used:

- `docs/product/ant-design/reference/ant-llms-sources.md`
- `docs/product/ant-design/README.md`
- `docs/product/ant-design/patterns/input.md`
- `docs/product/ant-design/patterns/navigation.md`

No external online research was needed for this post-interactive correction pass; the defects
were reproduced from local screenshots, live manual review, and existing framework tests.

