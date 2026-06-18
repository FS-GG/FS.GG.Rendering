# Feature 149 Live Proof Artifacts

## Artifact Schema

- `proof.md`: human-readable proof with host profile, package version, algorithm, sentinel artifact, damage artifact, sampled regions, verdict, and diagnostics.
- `proof.json`: optional machine-readable proof with the same fields.
- `sentinel-frame.*`: first full-frame sentinel artifact when a capable host can capture it.
- `damage-frame.*`: second frame drawn through the damage-only scissor/no-clear path.
- `limitations.md`: environment-limited disclosure when the host cannot prove live preservation.

## Acceptance Gate

- `passed`: three fresh capable-host runs show untouched regions retained sentinel identity and damaged regions reflected the damage draw on a matching host profile.
- `failed`: readback completed but stale pixels, cleared pixels, host mismatch, algorithm mismatch, blank artifacts, or missing artifacts invalidated the proof.
- `environment-limited`: display, renderer, readback, permission, timeout, or host-error conditions prevented a proof.

## Current Status

The deterministic harness records environment-limited live proof evidence by default. That output is useful for readiness bookkeeping and fallback diagnostics, but it does not unlock partial redraw.

Synthetic simulations are disclosed in Synthetic-named tests with `// SYNTHETIC:` comments and are never reported as accepted live proof.
