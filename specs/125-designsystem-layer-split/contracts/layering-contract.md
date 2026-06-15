# Contract — Layering / Dependency-Direction

The interface this feature exposes to consumers and maintainers is a **structural contract**: an
acyclic package dependency graph that makes the four-layer architecture physical. This document is
the checkable statement of that contract (the "API" of a layer split is its dependency direction).

## The contract

```text
FS.GG.UI.Scene            (no UI-layer deps)
   ▲
FS.GG.UI.DesignSystem     depends on: Scene            — and NOTHING else in the UI stack
   ▲                 ▲
   │                 │
Themes.Default     Controls
depends on:        depends on:
  DesignSystem       DesignSystem (+ existing Scene/Layout/KeyboardInput)
```

### Required edges
- `FS.GG.UI.DesignSystem` → `FS.GG.UI.Scene` (only).
- `FS.GG.UI.Themes.Default` → `FS.GG.UI.DesignSystem` (only UI-layer edge).
- `FS.GG.UI.Controls` → `FS.GG.UI.DesignSystem`.

### Forbidden edges (any one fails the contract)
- `FS.GG.UI.DesignSystem` → `FS.GG.UI.Controls`  ❌ (back-edge to the catalog)
- `FS.GG.UI.DesignSystem` → `FS.GG.UI.Themes.Default`  ❌ (primitives must not know a concrete theme)
- `FS.GG.UI.Themes.Default` → `FS.GG.UI.Controls`  ❌
- `FS.GG.UI.Controls` → `FS.GG.UI.Themes.Default`  ❌ (catalog must not hard-wire the default theme)

## How the contract is enforced

1. **Compile-time (primary)**: F#/MSBuild reject a `ProjectReference` cycle. A green build of the
   solution is positive proof the graph is acyclic (SC-006).
2. **Dependency-closure check (US1/SC-002)**: a consumer that references **only**
   `FS.GG.UI.DesignSystem` resolves all design-system primitives and `FS.GG.UI.Controls` does **not**
   appear in its transitive closure. Verifiable by inspecting the project's resolved references
   (e.g. the restore assets / `dotnet list package --include-transitive`) or by compiling a minimal
   consumer that opens `FS.GG.UI.DesignSystem`, uses `Theme`/`ResolvedStyle`/`Style.resolve`, and
   references no controls type.
3. **`.fsproj` review**: `DesignSystem.fsproj` has exactly one `ProjectReference` (`Scene`);
   `Themes.Default.fsproj` has exactly one (`DesignSystem`); `Controls.fsproj` adds `DesignSystem`
   and references no theme project.

## Acceptance (maps to spec)

| Contract clause | Spec criterion |
|---|---|
| DesignSystem deps == {Scene} | US1.2, SC-002, FR-001 |
| Controls not in DesignSystem closure | US1.1, SC-002 |
| Themes.Default deps == {DesignSystem} | US2.1, FR-002 |
| Acyclic graph (green build) | SC-006, FR-009 |
| No control fork / one control set | constitution layering clause |
