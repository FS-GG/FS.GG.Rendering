# 0004. Public token, policy & resolver surface (Workstream F, Phase F5)

**Status**: accepted
**Date**: 2026-06-16
**Spec**: [specs/130-promote-token-surface](../../../specs/130-promote-token-surface/spec.md)

## Decision

F5 is the deliberate, baseline-gated **public-surface promotion** that F1–F4 each deferred. Two
proven-internal capabilities in `FS.GG.UI.DesignSystem` become a public, documented, `.fsi`-declared
contract. Promotion is **visibility-only**: token *values* are byte-identical (regenerated from the same
DTCG source), the `Theme` record shape is unchanged, and rendered output is byte-identical under the
default neutral policy.

Per **Principle II** (visibility lives in the `.fsi`, not in `.fs` access modifiers), each module loses
its `internal` modifier and gains a curated `.fsi`. For the generated taxonomy the generator emits a
paired **generated `.fsi`** in lock-step with the `.fs`.

### Promoted — `StyleResolver` (hand-curated [`StyleResolver.fsi`](../../../src/DesignSystem/StyleResolver.fsi))

The full front-half resolver + intent-policy seam — exactly five public members:

| Symbol | Signature |
|--------|-----------|
| `type IntentPolicy` | `{ ApplyIntent: Theme -> string -> ResolvedStyle -> ResolvedStyle }` |
| `val baseStyleFor` | `Theme -> string -> ResolvedStyle` |
| `val neutralPolicy` | `IntentPolicy` |
| `val resolve` | `IntentPolicy -> Theme -> string -> string -> StyleClass list -> VisualState -> ResolvedStyle` |
| `val resolveDefault` | `Theme -> string -> string -> StyleClass list -> VisualState -> ResolvedStyle` |

Baseline rows added: `FS.GG.UI.DesignSystem.StyleResolver`, `FS.GG.UI.DesignSystem.StyleResolver+IntentPolicy`.

> Implementation note: `resolveDefault` is written as a fully-applied function (not the eta-reduced
> `resolve neutralPolicy`) so its arity matches the curated public signature. The change is semantically
> identical — the Feature129 neutral-parity oracle remains byte-identical.

### Promoted — `DesignTokensExt` taxonomy (generated [`DesignTokensExt.fsi`](../../../src/DesignSystem/DesignTokensExt.fsi))

The **entire** Ant-derived taxonomy is promoted as one unit: `Seed`, `Map.{Light,Dark}`,
`Alias.{Light,Dark}`, `Component.{Button,Input,Table,Tabs,Menu}`, `Space`, `Density`,
`Type.{Display,Section,Title,Body,Small}`, `Elevation`. Baseline rows added: `DesignTokensExt` and each
nested sub-module type (23 rows) — every added baseline row corresponds one-to-one to a promoted symbol
(two-way agreement verified against the `tests/surface-baselines/FS.GG.UI.DesignSystem.txt` diff).

- **"Chosen subset" is exercised at module granularity.** The taxonomy is all token *values* with no
  internal helpers, so there is no finer-grained surface to withhold; selectivity (FR-001/FR-009) is
  applied at the candidate-*module* level — `StyleResolver` and `DesignTokensExt` in, `ColorPolicy` out.
- **Documentation is module-granularity (FR-010).** The generated `.fsi` carries a `///` doc comment on
  the top-level module and on each nested layer/sub-module; per-leaf doc comments are **not** emitted
  (the leaf names are self-describing and number ~130). The generator's `--check` covers BOTH the `.fs`
  and the `.fsi` so they can never drift.

## Deferred (recorded, not promoted)

- **`FS.GG.UI.Color.ColorPolicy` (the `wcag`/`ant` color-validation engine) — stays internal.**
  Rationale: `FS.GG.UI.Color` carries **no** public-surface baseline at all (it is absent from the
  refresh table), the `--design-system wcag|ant` template parameter (F3/128) already exposes the policy
  *choice* to consumers, and no current consumer needs the engine itself public. Promoting unguarded
  `Color` internals would contradict the deliberate, baseline-gated discipline this phase exists to
  uphold. Revisit only when a consumer needs the engine as a public contract.
- **`DesignTokens` ↔ `DesignTokensExt` unification — deferred.** The flat `DesignTokens` primitives and
  the `DesignTokensExt` taxonomy remain separate public modules; F5 does not merge them. Unifying the two
  naming surfaces is a larger, separately-specced change with its own migration story.

## Stability & reversibility commitment

- The promoted surface is now a **public contract**: changes to it are governed by the surface-drift gate
  (`tests/surface-baselines/`) and require a Tier-1 artifact chain (spec → plan → `.fsi` → baseline →
  tests → decision record). The token *values* stay generated from the DTCG single source of truth.
- Promotion is **reversible** in principle (re-internalize + drop the `.fsi` + restore IVT), but doing so
  is itself a Tier-1 breaking change once external consumers depend on the surface; it would follow the
  same gated chain in reverse.

## Consequences

- One package's surface grows (`FS.GG.UI.DesignSystem`); no other baseline changes. The redundant F1/F4
  `InternalsVisibleTo` grants are removed (consumers now use the public surface) — invisible to the
  surface gate, so baseline-neutral.
- No new dependency enters any product/test assembly; `System.Text.Json` remains confined to the
  generator script. No control code changes; no render-path change.
