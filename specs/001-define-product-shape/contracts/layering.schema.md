# Contract: Layering Document (`docs/product/layering.md`)

Defines the UI layer boundaries the product commits to. Adapted from the source
`design-and-controls.md`; MUST stay consistent with constitution Engineering Constraints.

## Required structure

1. **Ownership** — one paragraph naming what the rendering repo owns (semantic controls,
   design-system primitives, themes, optional design-specific kits).
2. **Four layer definitions**, each with: *Owns* / *Does NOT own* / *Examples*:
   - Semantic controls
   - Design-system primitives
   - Themes
   - Design-specific kits
3. **One-control-set rule** — explicit statement that there is one semantic control set
   styled by many themes, and that per-theme control forks (`AntButton`, `FluentButton`,
   `MaterialButton`) are rejected by default, with the justification (tests, a11y, keyboard,
   focus, docs, examples must not multiply per theme).
4. **Decision rule table** — change type → layer:

   | Change type | Layer |
   |---|---|
   | Visual tokens, color, spacing, typography, radius, shadow, density, icon, visual states | Theme |
   | Shared style slots / token names needed across themes | Design system |
   | Input/focus/accessibility behavior, state machine, value model, command semantics | Control |
   | Opinionated composition, data workflow, validation layout, table behavior | Kit / pattern |

## Acceptance (maps to spec)

- [ ] Exactly four layers, each with non-overlapping *Owns* / *Does NOT own*. *(FR-004, SC-003)*
- [ ] One-control-set rule stated and justified; per-theme forks explicitly rejected. *(FR-005)*
- [ ] Decision rule table present and resolves the quickstart classification cases. *(SC-003)*
- [ ] No contradiction with constitution Engineering Constraints layering clause.
