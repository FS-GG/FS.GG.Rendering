# Controlled Fixture Evidence Guide

Fixture mode creates synthetic skill surfaces under the requested output
directory and checks them without modifying repository skill files.

Required synthetic cases:

- `missing-wrapper`: canonical source is absent from a supported wrapper surface.
- `wrapper-only`: wrapper entry has no canonical target and is not a command
  skill.
- `stale-description`: wrapper metadata differs from its canonical source.
- `broken-target`: wrapper route target cannot be resolved.
- `canonical-drift`: duplicate canonical sources with the same name diverge.
- `duplicate canonical-source conflict`: represented by the canonical drift
  case.
- `guidance-gap`: applicable canonical skill misses required guidance tokens.
- `passing`: aligned canonical source and wrapper with covered guidance.

Synthetic fixture evidence must stay visibly caveated in readiness records and
must not be summarized as real repository parity.
