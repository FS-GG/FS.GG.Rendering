# Contract: Cadence → Trigger Matrix (Stage R6)

The authoritative derivation of `docs/validation/validation-set.md` into CI triggers, **joined with**
the R5 harness tiers (T0–T-uinput, which trace to the harness — `docs/validation/harness.md` — not to
`validation-set.md`). Every row appears in **exactly one** cadence. This contract is realized in-repo as
`docs/ci/cadence-map.md`; the audit (FR-009) compares the two and fails on any drift or overlap.

## Cadences

| Cadence | Trigger | Required (blocks merge) | Runner | Fork PRs |
|---|---|---|---|---|
| **gate** | `push` + `pull_request` to default branch | **yes** | hosted headless | runs (no secrets) |
| **release** | `release` / tag (+ `workflow_dispatch`) | no | hosted headless | restricted |
| **capability** | `schedule` (+ `workflow_dispatch`) | no | display/GL/uinput-capable (out-of-scope to provision) | restricted |

## Member → cadence map

Frequency labels for validation-set members are quoted from `docs/validation/validation-set.md` (R3);
harness rows carry their R5 source label `infra (R5)` (the harness tiers are **not** validation-set
members — the R3 "Manual/advisory" group is empty). `cap` = capability needed. NB: `capability` below is
a **cadence id** (a trigger), not a frequency label.

| Member | R3 label | Cadence | cap | Behavior on headless runner |
|---|---|---|---|---|
| `Color.Tests` | local | gate | none | runs |
| `Scene.Tests` | local | gate | none | runs |
| `Layout.Tests` | local | gate | none | runs |
| `Input.Tests` | local | gate | none | runs |
| `KeyboardInput.Tests` | local | gate | none | runs |
| `Elmish.Tests` | local | gate | none | runs |
| `Controls.Tests` | local | gate | none | runs |
| `Testing.Tests` | local | gate | none | runs |
| `SkiaViewer.Tests` | local | gate | gl | degrade-and-disclose |
| `Smoke.Tests` | local | gate | gl | degrade-and-disclose |
| `Lib.Tests` (runtime subset) | local | gate | none | runs |
| `surface-baselines` | ci (push/PR) | gate | none | runs |
| docs build (`fsdocs`) | ci (push/PR) | gate | none | runs (build only; publish is release-side) |
| harness **T0** (`offscreen` deterministic) | infra (R5) | gate | none | runs |
| harness **T1** (`offscreen` readback) | infra (R5) | gate | gl | degrade-and-disclose |
| harness **T2** (`live-x11`) | infra (R5) | capability | x11 | degrade-and-disclose until capable runner |
| harness **T3** (`perf`) | infra (R5) | capability | gl/x11 | degrade-and-disclose until capable runner |
| harness **T-uinput** (`input --backend uinput`) | infra (R5) | capability | uinput | inert + disclosed |
| `Package.Tests` | release-only | release | none | runs on release trigger |
| `Product.Tests` (template) | release-only | release | none | runs on release trigger |

## Invariants (the audit checks these)

1. **Exactly one cadence per member** — no member in two rows. *(FR-009)*
2. **No release-only member in `gate`** — `Package.Tests`/template `Product.Tests` never in the push gate. *(FR-008)*
3. **Every row traces to a settled source** — validation-set members trace to `docs/validation/validation-set.md` (R3); harness tiers (T0–T-uinput) trace to the R5 harness (`docs/validation/harness.md`). Nothing is invented here. *(spec Out of Scope: no re-decide)*
4. **Only `gate` is `required`** — release/capability never block merge. *(FR-007)*
5. **Capability rows degrade-and-disclose, never silently drop or falsely pass.** *(FR-005)*

## Acceptance (maps to spec)

- [ ] `docs/ci/cadence-map.md` lists every member with exactly one cadence; audit passes. *(FR-009, FR-012, SC-003)*
- [ ] No release-only member runs in the gate across sampled runs. *(FR-008, SC-007)*
- [ ] Capability members map to `capability`/`release`, never to the required gate. *(FR-007)*
