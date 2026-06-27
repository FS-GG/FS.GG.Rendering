# Contract: Consumer Pinning Behavior (US1/US2 — FR-002, FR-003, FR-004)

Defines the observable restore/build behavior a consumer gets from the BOM. This is the contract the
**live consumer test** (env-gated, R6) proves against a real packed feed.

## CP-1 — One reference ⇒ coherent set (FR-002, US1)

A consumer whose only FS.GG.UI declaration is:

```xml
<PackageReference Include="FS.GG.UI" Version="X" />
```

restores with **every** `FS.GG.UI.*` member resolved to **X**, no missing-package (NU1101) and no
version-conflict (NU1605/NU1608) errors, and **no second FS.GG.UI version literal** in the consuming
project. It then **compiles** against that version's APIs.

## CP-2 — Full coverage, in lockstep (FR-003, US2 AS3)

The set the BOM pins lists **every** member of the snapshot it represents (data-model E2). The
membership list is **single-sourced** in `FS.GG.UI.nuspec` and locked to the packable
`FS.GG.UI.*` projects by the **parity test** — a member added/removed without a matching nuspec edit
fails loudly, so an adopter cannot silently miss a newly published member.

## CP-3 — Deviation is loud (FR-004, US2)

> **Corrected against live evidence (Feature 207 T006 — see research R1 amendment and
> `readiness/bom-consumer-validation.md`).** The original draft said the conflict is `NU1605/NU1107`
> and fails restore unconditionally. The observed behavior is more precise:

Given the BOM at `X` and a member also forced to `Y ≠ X`, the **exact `[X]`** dependency makes the
deviation **detected in both directions**:

- `Y < X` (stale) ⇒ **NU1605** (detected package downgrade), and
- `Y > X` (newer) ⇒ **NU1608** (detected package version outside the dependency constraint).

A floating lower bound `[X,)` would only raise the `Y < X` case and silently absorb `Y > X` — so the
exact bracket is what makes both directions visible (the reason for R1's exact-`[X]` choice).

These two codes are NuGet **warnings by default**: NuGet's nearest-wins then resolves the member to
`Y` and a mixed `X`+`Y` graph **builds**. They become a **blocking** restore/build failure when the
consumer treats them as errors — `WarningsAsErrors=NU1605;NU1608` (or `TreatWarningsAsErrors`), which
is the FS.GG repository's and the governed `fs-gg-ui` template's default posture. So "loud" =
exact-bracket **detection in both directions** + the recommended warnings-as-errors policy, not an
unconditional `NU1107`. The detection itself is unconditional and is the structural signal that did
not exist under hand-aligned per-package pins.

## CP-4 — Optional / additive (FR-007)

Adopting the BOM is opt-in. A consumer (and the `fs-gg-ui` template) that keeps the existing
per-package / `FsSkiaUiVersion` central pinning continues to restore+build unchanged; the BOM adds a
pinning surface, it does not replace or require migrating off the existing one.

## Pass conditions

| ID | Condition | Maps to |
|----|-----------|---------|
| CP-A | Clean consumer referencing only `FS.GG.UI@X` restores: 100% of resolved `FS.GG.UI.*` at `X`; 0 NU1101/NU1605/NU1608; builds. | FR-002, SC-001, US1 AS1/AS2 |
| CP-B | Enumerating the BOM's pinned set lists every member at exactly `X` (none omitted, none at a different version). | FR-003, US1 AS3 |
| CP-C | Parity test green: BOM deps == packable `FS.GG.UI.*` set, single `[X]` token. | FR-003, US2 AS3 |
| CP-D | Forcing any member to `Y≠X` is detected in both directions (`Y<X`⇒NU1605, `Y>X`⇒NU1608) and, under the warnings-as-errors posture, fails restore/build (no mixed graph) in 100% of attempts. | FR-004, SC-003, US2 AS1/AS2 |
| CP-E | A project keeping `FsSkiaUiVersion`/CPM (no BOM) restores+builds unchanged. | FR-007, SC-002 |
