# V2-PREFLIGHT-01 CAP4 — Rendering current compiler qualification

Status: current public installed qualification passed; protected delivery remains pending.
This bounded consumer window belongs to the existing
[Unified Roadmap capacity work](https://github.com/FS-GG/.github/blob/main/docs/2026-09-07-154210-fs-gg-unified-development-roadmap.md).
It adds no major feature or publication outcome.

- [ ] Deliver the closed current 2.1 qualification route and its focused guards.
- [x] Qualify genuine public CLI 2.1.0 author/inspect, deterministic extraction,
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
These fixture results are separate from the installed acceptance below. Both source-preparation
and public-qualification telemetry begin actually returned `not-configured`; native usage remains unknown.

On the granted Lane2 CPU1, executable source `d60241583d83406197c455898936aff8c4a55b2f`
qualified a fresh public CLI 2.1.0 installation from NuGet.org against producer source
`518517f6b90330a6e99f90bbce68faa0a891287f`. The upstream
[dual-feed publication run](https://github.com/FS-GG/FS.GG.SDD/actions/runs/37114843541)
completed attempt 2 successfully. Fresh downloads of all three public archives matched every
literal non-signature entry of the retained original archives, including `[Content_Types].xml`.
The retained installed CLI, Artifacts DLL and runtime Core matched those served archive entries.

The actual current qualifier passed exact identity/profile checks, two author/inspect runs,
complete deterministic extraction, the strict identity-only historical comparison, retained and
document bounded traces, and both stale-binding refusals. Two additional unformatted author
outputs were retained and proved entire-tree byte equality before formatter processing. Native
F# compilation of the fresh generated bindings passed the contract fingerprint and nine-action
catalogue assertions. Six real reducer tests passed. Packed .NET and Fable/Node consumers each
replayed 192 retained and 192 document transitions with matching projections and killed all five
document mutants. Packed input consumers passed both runtimes; bounded input qualification passed
its seven witnesses, deterministic seeded traces, and two mutant refusals.

Private evidence is retained under
`/tmp/rendering-public-sdd21-qualification-20261003/evidence/`, including both raw author trees,
fresh authority, TRX, command logs, public archive/Core joins, and the window summary. The actual
installed qualifier receipt SHA-256 is
`57e3f0692442032f50259ff4f9bf9aae87689cf7551a04b2de3ee36fa54a9f8e`;
the packed reducer replay receipt SHA-256 is
`16e59bba5ede81e4615f6817fe573f2bff33f141d3f042793d139211e35cbf51`.
The frozen tree remains `a66ac67b81b785c7917a2d3a42ab3b37f7aa42e4cc71d96be616ade70f3365f0`;
the current formatted tree is `6b740da5dec7e9d9d323df7283473635a36386086d58db60064955e8ce499034`.

The known local SDK Core mismatch was handled through an isolated SDK mirror using verified official
Core bytes; no global SDK changed. The first reducer restore failed with shared-cache `NU1403`;
a fresh private package/HTTP cache restored successfully without lockfile changes. Lane2 was released
after every native command terminated. Protected source delivery and repository required CI remain pending.

Workspace impact: this window changes the compiler selected by repository qualification only.
It changes no fresh workspace creation, enabled runtime behavior, provider/lifecycle default,
public API, Contracts 7.5.2 pin, Rendering package version, or original published 0.32 archives.
There is no workspace upgrade action. Producer publication is a separate prerequisite;
receiver adoption is complete only after actual installed qualification and protected delivery.
