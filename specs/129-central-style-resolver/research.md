# Phase 0 Research — Central Visual-State Style Resolver (F4)

All NEEDS CLARIFICATION resolved. Findings are grounded in the current tree (paths/lines
verified 2026-06-16).

## R1 — Where does the front-half resolver live?

**Decision**: A new `module internal StyleResolver` in **`src/DesignSystem/StyleResolver.fs`**
(no `.fsi`), in assembly `FS.GG.UI.DesignSystem`. `DesignSystem.fsproj` grants
`InternalsVisibleTo` to `FS.GG.UI.Controls` (so `buttonGeom` can call it) and `Controls.Tests`
(so the parity/totality/divergence tests can call it).

**Rationale**:
- DesignSystem already owns `Theme`, `ResolvedStyle`, the back-half `Style.resolve`
  (`Style.fsi:30`), and the intent→colour machinery (`applyVariant`/`applyCustom`,
  `Style.fs:26–59`). The master plan describes "one resolver mapping `theme → kind → intent →
  states → ControlStyle` so controls stop sprinkling `theme.Accent`/… across render code" — that
  is a design-layer concern, so the central path belongs in the central layer.
- Reuses the back half **in the same assembly**, no cross-layer hop.
- Keying on **strings** for both `kind` (already a string discriminator in the render dispatch,
  `Control.fs:1072`) and `intent` (the existing lowered vocabulary `"primary"`/`"danger"`/…,
  `Primitives.fs:48–53`) avoids introducing any DesignSystem-level intent enum and keeps
  DesignSystem ignorant of the Controls `ButtonIntent` type — preserving the acyclic layering
  (`Controls → DesignSystem`, never reverse).

**Alternatives considered**:
- *Front-half in `Controls` (internal).* `Controls` already defines `ButtonIntent` and hosts
  `buttonGeom`, so no IVT-to-production grant would be needed. **Rejected** because it splits the
  "single central resolver" across two layers (back half in DesignSystem, front half in Controls)
  and makes the resolver invisible to any future non-Controls consumer; the master plan frames
  one central path in the design layer.
- *Promote a public `Style.resolveControl` now.* **Rejected** — violates FR-007 (zero public
  surface delta); public promotion is explicitly F5's job.
- *DesignSystem-level typed `Intent` enum.* **Rejected** — a new public type is surface delta;
  an internal enum adds a second intent vocabulary to maintain. String keying reuses the lowered
  strings already flowing through the attribute bus.

## R2 — How is the migration byte-identical under the default theme?

**Decision**: The default `IntentPolicy` is **intent-agnostic** — it returns the kind's
structural base unchanged, ignoring intent. The two structural bases are the *exact literal
records* relocated from `buttonGeom` (`Control.fs:823–839`):
`"button"` → filled (`Fill = theme.Accent`, `StrokeWidth = 0.0`), `"icon-button"` → outline
(`Fill = Colors.transparent`, `Stroke = theme.Accent`, `StrokeWidth = 2.0`).

The full path is `resolve policy theme kind intent classes state = Style.resolve theme
(policy.ApplyIntent theme intent (baseStyleFor theme kind)) classes state`. Under the neutral
policy, `ApplyIntent` is identity, so this is exactly `Style.resolve theme structuralBase classes
state` — the pre-migration call verbatim. For the no-class `Normal` case it reduces to the
structural base (the 093 invariant `resolve theme base [] Normal = base`, `Style.fsi:26`),
reproducing today across **all four intents** (intent ignored) and **all eight visual states**
(back half unchanged).

**Rationale**: This is the F2/F3 "default-neutral until a theme opts in" precedent. Byte-identity
is achieved by construction — the relocation preserves the literals and the neutral policy drops
the intent the same way today's renderer does (today via never-reading it; now via an explicit
identity policy). The difference is that the intent is now *threaded as an argument* (reaches
resolution, FR-002), making it a live seam rather than dead code.

**Alternatives considered**:
- *Route intent through `StyleClass`/`applyCustom` under the default theme.* **Rejected** — under
  the default theme `applyCustom "danger"` → `theme.Danger ≠ theme.Accent`, which would change
  output. That divergence is exactly what F4 must NOT enable by default.

## R3 — How does the intent reach the renderer (eliminating the dead-code drop)?

**Decision**: `Button.view` already lowers the intent to a `style` attribute string
(`Primitives.fs:99`, `yield Attr.style (LegacyControls.intentStyle props.Intent)`). Today the
renderer reads `styleClasses` and `visualState` but **never** the `style` attribute, so the value
is discarded (`Danger` ≡ `Primary`). F4 makes `faithfulContent` (`Control.fs:1062`) **extract**
that attribute (e.g. `textValueOf "style" control`, defaulting to `"primary"`/neutral when
absent) and pass it as the `intent` argument into the `"button"`/`"icon-button"` dispatch
(`Control.fs:1095–1096`), which forwards it to `buttonGeom` → `StyleResolver.resolve`.

**Rationale**: Minimal, behaviour-neutral, and reuses the existing lowering — the attribute is no
longer dead because it is now read. No new attribute key, no `Primitives.fs` behaviour change. The
intent now "reaches resolution" even though the neutral policy doesn't act on it (FR-002, SC-002).

**Alternatives considered**:
- *Add a dedicated typed intent attribute / change `Primitives` lowering.* **Rejected** — larger
  surface, and the existing `style`-string seam already carries the value; reading it is the
  smallest faithful change.
- *Remove the lowering entirely and thread `ButtonIntent` directly.* **Rejected** — would couple
  the renderer dispatch to the typed `ButtonIntent` and require a wider `buttonGeom` signature
  change for no behavioural benefit; the string seam is sufficient and total.

## R4 — How is the policy/intent seam represented, and how is divergence proven?

**Decision**: A one-field internal record `type IntentPolicy = { ApplyIntent: Theme -> string ->
ResolvedStyle -> ResolvedStyle }`, with `neutralPolicy` (the default, `ApplyIntent = fun _ _ s ->
s`). `buttonGeom` calls the neutral path. The **divergence test** (User Story 3) constructs a
non-default policy whose `ApplyIntent` maps `"danger"` to a base with `Fill = theme.Danger`
(inline, or reusing the `Danger` variant delta) and calls `StyleResolver.resolve divergentPolicy
…` directly — confirming the resolved `Danger` style differs from `Primary` **with no edit to any
control's render code**.

**Rationale**: A record-of-functions is the plainest representation of "an overridable mapping"
(Principle III). The default stays neutral; the seam *admits* divergence; F4 proves divergence is
reachable through the resolver alone without wiring it into a default theme (FR-005, SC-007). This
is the seam D2 (Ant theme) and F5 will consume.

**Alternatives considered**:
- *Derive the policy from the `Theme` record.* **Rejected for F4** — would require a `Theme` shape
  change (FR-012 forbids) or a convention overload; F4 only needs to prove the seam exists, so an
  explicit policy argument is sufficient and smaller.

## R5 — Totality and determinism

**Decision**: `baseStyleFor theme kind` is a total `match` with a defined fallback for unknown /
`Custom` kinds (returns the filled base — a defined, visible default, never empty/transparent,
never an exception). `intent` is a string, so unknown strings are simply ignored by the neutral
policy (and handled by whatever a non-default policy chooses). The full cross-product `{kind} ×
{intent} × {state}` therefore yields a concrete `ResolvedStyle` for every combination,
deterministically (pure function of its arguments, no clock/random/global state).

**Rationale**: Inherits the back half's totality (`Style.resolve` is total over every
`(Theme, ResolvedStyle, StyleClass list, VisualState)`, `Style.fsi:27–29`) and adds a total front
half. Satisfies FR-004 / SC-003 and the edge cases (unknown kind/intent, combined states).

## R6 — Gates, build, and test invariants

**Decision / facts** (from 126/127/128 precedent):
- **Build**: `dotnet build FS.GG.Rendering.slnx -c Release` — expect 0 warnings, 0 errors
  (`TreatWarningsAsErrors=true`).
- **Headless deterministic tests**:
  `dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --filter "Feature129"`.
- **Full suite (unchanged pass/skip counts)**: `dotnet test FS.GG.Rendering.slnx -c Release`.
- **Surface-drift gate**: `dotnet fsi scripts/refresh-surface-baselines.fsx` (or `--check`) →
  `git status --porcelain tests/surface-baselines/` MUST be empty (zero rows changed, FR-007 /
  SC-004). Internal module + IVT are invisible to this gate.
- **Design-token-drift gate**: unchanged — F4 reads existing `Theme` fields only, regenerates
  nothing; the gate stays green.

**Rationale**: F4 must leave both drift gates and the full suite untouched; these commands are the
evidence (quickstart V4/V5). No baseline file is added (no new public package surface).
