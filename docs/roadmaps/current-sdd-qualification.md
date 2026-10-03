# V2-PREFLIGHT-01 CAP4 — Rendering current compiler qualification

Status: source prepared; installed qualification and delivery remain pending.
This bounded consumer window belongs to the existing
[Unified Roadmap capacity work](https://github.com/FS-GG/.github/blob/main/docs/2026-09-07-154210-fs-gg-unified-development-roadmap.md).
It adds no major feature or publication outcome.

- [ ] Deliver the closed current 2.1 qualification route and its focused guards.
- [ ] Qualify genuine public CLI 2.1.0 author/inspect, deterministic extraction,
  retained/document bounded model runs, stale-binding refusals, and native reducer replay.

The [qualifier](../../tests/svg-foundation/qualify-retained-profile.sh) keeps its default
historical 1.7.0 release route. The [live workflow](../../.github/workflows/svg-retained-qualification.yml)
explicitly selects the current 2.1.0 route. Both require an exact observed CLI version
and matching authored Artifacts package/profile identity. Arbitrary versions are refused.

Two fresh author runs must match completely. Historical qualification still compares the
whole extraction against the frozen tree. Current qualification permits only the authority's
package identity to change from Artifacts/1.7.0 to Artifacts/2.1.0; every other authority value,
file inventory, and extracted file byte must match. Any additional producer change requires
review rather than automatic normalization. Fresh authority and compilation receipts are
retained beside the run receipt, which records the observed CLI and Artifacts identities and
both frozen and fresh evidence tree hashes. Historical reports and committed receipts remain intact.

Source preparation on 2026-10-03 started from live protected main
`900afe37c307575f6104fe7de149088e8737309b`. Shell syntax checks and the
[offline guard fixtures](../../tests/svg-foundation/test-retained-sdd-selection.sh) passed.
These fixture results are not installed 2.1 compiler evidence. Full public installation,
native model execution, and reducer replay await the integrator's resource lane; no current
qualification receipt has been fabricated. Telemetry begin actually returned `not-configured`;
native usage remains unknown.

Workspace impact: this window changes the compiler selected by repository qualification only.
It changes no fresh workspace creation, enabled runtime behavior, provider/lifecycle default,
public API, Contracts 7.5.2 pin, Rendering package version, or original published 0.32 archives.
There is no workspace upgrade action. Producer publication is a separate prerequisite;
receiver adoption is complete only after actual installed qualification and protected delivery.
