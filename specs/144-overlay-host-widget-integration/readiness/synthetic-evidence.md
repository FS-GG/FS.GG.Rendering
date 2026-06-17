# Synthetic Evidence

Feature 144 uses deterministic synthetic fixtures for coordinator and host-edge tests. These fixtures are intentionally named with `Feature144` and are listed in this readiness folder.

Synthetic coverage:

- eight supported transient surface categories in `tests/Controls.Tests/Feature144OverlayFixtures.fs`
- product dispatch scripts in `tests/Elmish.Tests/Feature144OverlayDispatchFixtures.fs`
- deterministic overlay replay corpus in `tests/Rendering.Harness/Input.fs`

The synthetic evidence is authoritative for pure coordinator state, metadata extraction, dispatch ordering, and replay determinism. It is not claimed as live desktop visual proof.
