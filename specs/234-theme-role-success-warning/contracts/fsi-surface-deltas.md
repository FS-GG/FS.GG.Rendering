# Contract / `.fsi` surface deltas

## `src/Themes.Default/Theming.fsi` — `RolePalette` gains a field (additive)

```
 type RolePalette =
+    { Mode: ThemeMode        // NEW: the mode the palette was resolved against
       Background: Color
       Foreground: Color
       Accent: Color
       Danger: Color
       Muted: Color
       FocusRing: Color }
```

- **Type-level surface: unchanged.** No new exported type; `RolePalette` already appears in
  `readiness/surface-baselines/FS.GG.UI.Themes.Default.txt`. The drift gate validates *type FullName*
  granularity (`GetExportedTypes`), so the baseline needs no edit.
- **Source compatibility:** the only `RolePalette` constructor is `Theming.resolve` (grep-verified); no
  external construction site, so the required field is source-safe in-repo.
- `toTheme`'s doc comment is corrected (mode-seeded, not "carry from `Theme.light`").

## `src/DesignSystem/Style.fsi` — unchanged

`successColor`/`warningColor`/`isDark` were never on the surface (module-private; only `Style.resolve`
is exported). The behavioural fix is internal.

No cross-repo contract (`fs-gg-ui-template`, registry `dependencies.yml`) is touched. Ships in the next
batched `fs-gg-ui` coherent-set release; no version bump in this feature merge.
