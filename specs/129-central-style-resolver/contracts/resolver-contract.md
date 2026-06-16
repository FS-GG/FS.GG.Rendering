# Contract — Front-Half Resolution Path & Intent Policy Seam (internal)

**Surface**: `module internal StyleResolver` in `src/DesignSystem/StyleResolver.fs` (no `.fsi`),
assembly `FS.GG.UI.DesignSystem`. Reachable only via `InternalsVisibleTo`
(`FS.GG.UI.Controls`, `Controls.Tests`). **Not public** — invisible to the surface-drift gate.

## Types

```fsharp
type IntentPolicy =
    { ApplyIntent: Theme -> string -> ResolvedStyle -> ResolvedStyle }
```

## Functions

| Function | Signature | Contract |
|----------|-----------|----------|
| `baseStyleFor` | `Theme -> string -> ResolvedStyle` | Total. Returns the kind's structural base: `"button"` → filled (`Fill = theme.Accent`, `StrokeWidth = 0.0`), `"icon-button"` → outline (`Fill = transparent`, `Stroke = theme.Accent`, `StrokeWidth = 2.0`). Any other/unknown kind → the filled base (defined fallback; never empty, transparent-fill-only, or exception). Literals relocated verbatim from `Control.fs:823–839`. |
| `neutralPolicy` | `IntentPolicy` | `ApplyIntent = fun _ _ s -> s`. The default — intent-agnostic, byte-identity preserving. |
| `resolve` | `IntentPolicy -> Theme -> kind:string -> intent:string -> StyleClass list -> VisualState -> ResolvedStyle` | `= Style.resolve theme (policy.ApplyIntent theme intent (baseStyleFor theme kind)) classes state`. Total + deterministic. |
| `resolveDefault` | `Theme -> string -> string -> StyleClass list -> VisualState -> ResolvedStyle` | `= resolve neutralPolicy`. The path `buttonGeom` calls. |

## Behavioural guarantees

1. **Neutrality (default policy)**: `resolveDefault theme kind intent classes state` equals the
   pre-migration `Style.resolve theme (structural base for kind) classes state` for **every**
   `intent` and **every** `state` (the intent is ignored). → FR-003, SC-001.
2. **Intent reaches resolution**: `intent` is a consumed argument, not discarded. → FR-002.
3. **Overridability without forking**: for any non-default `policy` whose `ApplyIntent`
   distinguishes two intents, `resolve policy theme kind iA … ≠ resolve policy theme kind iB …`,
   reachable with **zero** edits to control render code. → FR-005, SC-007.
4. **Totality + determinism**: no input combination throws or is nondeterministic. → FR-004,
   SC-003.
5. **Precedence preserved**: classes and state compose via the unchanged `Style.resolve`
   (`base < classes(attach order) < state`); F4 adds only the kind+intent → base step ahead of
   it. → FR-006.
6. **Reuse, not replace**: the 093 back half and the existing variant/colour machinery are
   reused; no second resolver or second precedence is introduced. → spec Assumptions.

## Consumer contract (Controls)

- `buttonGeom` obtains its `baseStyle` exclusively from `StyleResolver.resolveDefault`; the
  `primary: bool` parameter is replaced by `kind` (selecting geometry) + `intent`.
- `faithfulContent` extracts the intent from the existing `style` attribute (`textValueOf
  "style"`, default neutral) and forwards `kind` + `intent` to the `"button"`/`"icon-button"`
  dispatch.
- Controls references no concrete theme; `Theme` arrives as a render-time parameter (unchanged).
