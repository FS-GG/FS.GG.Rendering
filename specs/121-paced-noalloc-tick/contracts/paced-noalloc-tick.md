# Contract — Frame-Rate Pacing & No-Alloc Idle Tick (Feature 121)

The **internal** idle gate (pinned via `InternalsVisibleTo`) + the public pacing decision/validation.
Signatures from `RetainedRender.fsi` / `OpenGl.fsi` / `SkiaViewer.fs`; behaviour clauses are what the two
suites assert.

## C1 — `advanceStateClocks` (the no-alloc idle gate, internal)

```fsharp
val internal advanceStateClocks:
    delta: System.TimeSpan -> state: Map<RetainedId, RetainedUiState> -> Map<RetainedId, RetainedUiState>
```

- No active clock ⇒ result is `obj.ReferenceEquals` to `state` (no allocation).
- Active clock(s) ⇒ result is **not** reference-equal (rebuilt); each clock advanced exactly as `advance`
  (099/103 unchanged).

*Pins*: FR-004. *Used by*: US2.

## C2 — `GlHost.shouldAdvanceFrame` (the pure pacing decision, public val)

```fsharp
val shouldAdvanceFrame: lastFrameTime: float -> now: float -> frameInterval: float -> bool
```

- `true` iff ≥ `frameInterval` elapsed; gates update **and** present; a tighter cap ⇒ strictly fewer advances
  over the same window.

*Pins*: FR-002. *Used by*: US1.

## C3 — `ViewerOptions.FrameRateCap` (the consumer cap, public field + validation)

- Default 60; a positive cap clears option validation; a **non-positive** cap is rejected as a `ProductDefect`
  ("frame-rate cap must be positive") at validation, before GL init.

*Pins*: FR-001, FR-003. *Used by*: US1.

## Surface-drift

- **Zero new public-surface-baseline delta** (FR-005): `advanceStateClocks` is `internal`; `shouldAdvanceFrame`
  is a public `val` on the already-baselined `GlHost`, and `FrameRateCap` an additive field on the
  already-baselined public `ViewerOptions` (the SkiaViewer baseline is type-granular). `FS.GG.UI.Controls.txt`
  / `FS.GG.UI.SkiaViewer.txt` stay byte-unchanged.
</content>
