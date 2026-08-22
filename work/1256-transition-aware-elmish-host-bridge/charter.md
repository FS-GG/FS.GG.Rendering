---
schemaVersion: 1
workId: 1256-transition-aware-elmish-host-bridge
title: Transition-aware Elmish host bridge
stage: charter
changeTier: tier1
status: chartered
policyPointers:
  - .fsgg/sdd.yml
  - .fsgg/agents.yml
  - .fsgg/policy.yml
  - .fsgg/capabilities.yml
  - .fsgg/tooling.yml
---

# Transition-aware Elmish host bridge Charter

## Identity
- Work id: `1256-transition-aware-elmish-host-bridge`
- Lifecycle stage: charter
- Status: chartered

## Principles
- Keep ordinary Elmish updates, simulation, and controlled-input state synchronous while making workspace presentation transitions explicit, typed, and generation-fenced.
- Treat asynchronous worker and feature responses, commit acknowledgement, input suppression, visibility recovery, focus, and ARIA as one observable host transaction rather than unrelated callbacks.
- Preserve deterministic pure transition semantics in the adapter; React scheduling, DOM commit acknowledgement, visibility, pointer capture, and global input stay at the host edge.
- Hold production qualification to the filed budget without relaxation: no renderer task above 16 ms, p95 at most 16 ms, p99 at most 32 ms, and zero dropped frames.

## Scope Boundaries
- In: an additive public `FS.GG.UI.Elmish` transition-host contract, adapter semantics, fail-capable tests, generated Elmish template projection, a production Fable/React timing fixture, surface documentation, and coherent-set delivery evidence.
- In: delayed asynchronous responses, current-generation commit acknowledgement, rapid target replacement, held pointer/global input safety, controlled file/input/blur behavior, hidden-tab convergence, focus/ARIA recovery, and an authoritative typed ledger.
- Out: changing ordinary synchronous update semantics, simulation scheduling, unrelated Scene/SkiaViewer/Controls APIs, consumer-specific S.I.R. workspace code, or weakening the filed performance thresholds.
- Keep SDD lifecycle ownership separate from optional Governance enforcement.

## Policy Pointers
- SDD policy comes from `.fsgg/sdd.yml` and `.fsgg/agents.yml`.
- Governance files are optional compatibility pointers and are not evaluated by this command.
- Constitution I, III, V, VI, VII, and VIII govern specify-before-implementation, declared public surface, Model–Update–Effect separation, real fail-before/pass-after evidence, shared lifecycle contracts, and explicit safe failure.
- The coordination route receipt for `FS-GG/FS.GG.Rendering#1256` is revision 1 with digest `358a995a7448c0735f8f2b802acdfd4d72133bcc84858acc149c00e3dfe39cde` and requires full SDD readiness.

## Lifecycle Notes
- Tier 1 contracted change: signature, implementation, surface baseline, focused adapter tests, generated template projection, production timing fixture, package/coherent-set handoff, and source-bound lifecycle evidence must move together.
- Source implementation MUST NOT begin until `fsgg-sdd analyze` reports `implementationReady` for this work id.
- Next lifecycle action: `fsgg-sdd specify --work 1256-transition-aware-elmish-host-bridge`.
