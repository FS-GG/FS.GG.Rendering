# Contract: Browser Feasibility

## Scope

This contract defines the evidence required to decide whether to continue with a browser-capable
rendering path. It does not commit the project to a production browser backend in this feature.

## Candidate Path

The first candidate is a CanvasKit-compatible browser path. The implementation may prove this with
direct reuse of a .NET/F# browser-hosted painter or by generating/interpreting a CanvasKit command
stream from the portable package. If neither is practical, the feasibility report must document the
fallback path.

## Harness Surface

`tests/Rendering.Harness` must expose a render-anywhere feasibility command and pure evidence
formatters with `.fsi` coverage.

Required command behavior:

- Load an agreed corpus of portable scene packages.
- Locate matching reference rendering evidence for each package.
- Render each package through the browser candidate or classify why it cannot run.
- Compare candidate image output against the reference oracle with explicit tolerance.
- Persist a machine-readable report and human-readable summary.

## Report Record

The feasibility report contains:

- Candidate backend id and version/facts.
- Corpus entries with scenario id and package identity.
- Reference artifact identity for each scene.
- Candidate artifact identity when produced.
- Comparison tolerance and diff metric.
- Per-scene verdict: pass, fail, unsupported capability, missing resource, or environment-limited.
- Unsupported capability/resource summary.
- Final decision: accepted candidate path or documented fallback path.

## Acceptance Rules

- At least three representative showcase scenes are evaluated.
- Each accepted comparison points to a passed reference evidence record.
- Unsupported candidate capabilities name the capability id and affected scenes.
- The final decision must be present even when all browser runs are environment-limited.
- A report with only environment-limited results cannot claim accepted candidate path.

## Fallback Rules

When the candidate is not accepted, the report must choose and document one fallback:

- Continue CanvasKit with specific missing prerequisite follow-ups.
- Use a generated/interpreted CanvasKit command stream instead of direct painter reuse.
- Limit a Canvas2D path to a declared core subset.
- Defer browser backend work until a named dependency or host capability exists.
