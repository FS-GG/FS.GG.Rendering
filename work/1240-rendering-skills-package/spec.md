---
schemaVersion: 1
workId: 1240-rendering-skills-package
title: Publish FS.GG.Rendering.Skills
stage: specify
changeTier: tier1
status: specified
publicOrToolFacingImpact: true
---

# Publish FS.GG.Rendering.Skills Specification

Prose status: specified

## User Value
A product tree scaffolded through any workspace provider receives this repository's 18 product-scope rendering skill bodies, instead of only the trees the fs-gg-ui template itself creates.

## Scope
- SB-001: Publish this repository's 18 scope-product owner-fs-gg-rendering skill bodies as one versioned, content-addressed package on the same substrate FS.GG.Drivers and FS.GG.Game.Skills already use.
- SB-002: Ship a producer manifest that records, for every packed body, the identifier, the source path it was packed from, and its sha256 digest.
- SB-003: Ship a verify gate that recomputes every digest from packaged bytes and refuses the package when any byte differs from its recorded digest.
- SB-004: Record an explicit disposition for the two rows whose declared source path lies outside template/product-skills, namely fs-gg-feedback-report and fs-gg-samples.

## Non-Goals
- SB-005: Do not remove, gate, or otherwise change what the fs-gg-ui dotnet new template emits; this package is an additional channel, not a replacement.
- SB-006: Do not vendor or restate these skill bodies into the .github repository; that route was refuted upstream and is forbidden by ADR-0058, ADR-0062 and ADR-0063.
- SB-007: Do not implement the consuming side; enrolling the receiver is separate work tracked on the repositories this row blocks.

## User Stories
- US-001 (P1): As a product owner scaffolding through a non-rendering provider, I receive the rendering product skills in my tree so that skills whose predicate is always are actually present.
- US-002 (P1): As a consumer of the package, I can prove the bytes I received are the bytes the producer declared, without trusting the producer's working tree.
- US-003 (P1): As a maintainer of the fs-gg-ui template, I keep emitting these skills unchanged for the trees the template does create.

## Acceptance Scenarios
- AC-001 [US-002] [FR-001] [FR-002] [FR-003]: Given the published package, when a consumer recomputes each body's sha256 from the packaged bytes and compares it to the producer manifest and to the skill registry, then all 18 comparisons are equal.
- AC-002 [US-002] [FR-004]: Given the published package, when a single byte of one packed body is mutated, then the verify gate exits non-zero and names that body.
- AC-003 [US-001] [FR-001] [FR-005]: Given a product tree scaffolded through a non-rendering provider that restores this package, when the tree is materialized, then all 18 rendering product skills are present.
- AC-004 [US-003] [FR-006]: Given the change, when the fs-gg-ui template is exercised, then it emits the same three skill trees it emitted before, unconditionally.

## Functional Requirements
- FR-001: The package carries exactly the 18 rows that the skill registry declares with scope product and owner fs-gg-rendering; the packed count equals 18 and the set difference against the registry is empty in both directions. (Stories: US-001, US-002; Acceptance: AC-001, AC-003)
- FR-002: For each of the 18 packed bodies, the sha256 recorded in the producer manifest equals the sha256 the skill registry declares for that row; 18 of 18 digests compare equal. (Stories: US-002; Acceptance: AC-001)
- FR-003: The 18 digests are verified from published package bytes rather than from the working tree, so the comparison is made against the artifact a consumer actually receives. (Stories: US-002; Acceptance: AC-001)
- FR-004: The verify gate exits non-zero and names the offending identifier and both digests when any single packed byte is mutated; the mutation and the observed failure are recorded as evidence. (Stories: US-002; Acceptance: AC-002)
- FR-005: The producer manifest states, for each of fs-gg-feedback-report and fs-gg-samples, the source path the body was packed from, so the disposition of the two out-of-tree rows is machine-readable rather than implied. (Stories: US-001; Acceptance: AC-003)
- FR-006: The fs-gg-ui template still emits template/product-skills, template/feedback-report/skill and template/fragments/samples/skill with no added condition, verified by the template's own emission gates after the change. (Stories: US-003; Acceptance: AC-004)

## Ambiguities
- AMB-001 open: Whether the two out-of-tree rows are relocated under template/product-skills or packed from their existing paths is a decision this work must record either way; both answers are acceptable but silence is not.

## Public Or Tool-Facing Impact
- Introduces a new public package identifier that other repositories pin, plus a producer manifest whose schema is a consumed contract.
- The package is published to both feeds, so it participates in the organisation release train rather than a repository-local build.

## Lifecycle Notes
- Next lifecycle action: `fsgg-sdd clarify --work 1240-rendering-skills-package`.
