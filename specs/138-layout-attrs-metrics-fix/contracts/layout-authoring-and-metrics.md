# Contract: Layout Authoring and Metrics

## Public Controls Authoring Contract

The Controls package exposes layout authoring through `FS.GG.UI.Controls.Attr` builders and existing typed
container front doors where applicable.

Required canonical builders:

```text
Attr.padding      : float -> Attr<'msg>
Attr.margin       : float -> Attr<'msg>
Attr.gap          : float -> Attr<'msg>
Attr.alignItems   : LayoutAlign -> Attr<'msg>
Attr.alignSelf    : LayoutAlign -> Attr<'msg>
Attr.justifyContent : LayoutAlign -> Attr<'msg>
Attr.flexGrow     : float -> Attr<'msg>
Attr.flexShrink   : float -> Attr<'msg>
Attr.flexBasis    : float -> Attr<'msg>
Attr.minWidth     : float -> Attr<'msg>
Attr.minHeight    : float -> Attr<'msg>
Attr.maxWidth     : float -> Attr<'msg>
Attr.maxHeight    : float -> Attr<'msg>
```

Existing builders remain valid:

```text
Attr.width        : float -> Attr<'msg>
Attr.height       : float -> Attr<'msg>
Stack.orientation : string -> Attr<'msg>
```

Compatibility:

- Omitted values preserve current bounds for existing screens.
- Explicit zero values are honored.
- `spacing` remains a gap compatibility alias if an existing typed front door still emits it.
- Padding and margin are uniform values for this feature.
- Alignment uses the existing `FS.GG.UI.Layout.LayoutAlign` vocabulary.

## Layout Lowering Contract

`Control.toLayout` must project authored values into `LayoutIntent` in the same frame:

```text
padding        -> LayoutIntent.Padding
margin         -> LayoutIntent.Margin
gap/spacing    -> LayoutIntent.Gap
alignItems     -> LayoutIntent.AlignItems
alignSelf      -> LayoutIntent.AlignSelf
justifyContent -> LayoutIntent.JustifyContent
flexGrow       -> LayoutIntent.FlexGrow
flexShrink     -> LayoutIntent.FlexShrink
flexBasis      -> LayoutIntent.FlexBasis
minWidth       -> LayoutIntent.MinSize.Width
minHeight      -> LayoutIntent.MinSize.Height
maxWidth       -> LayoutIntent.MaxSize.Width
maxHeight      -> LayoutIntent.MaxSize.Height
```

The lower-level `Layout` package remains responsible for normalization, diagnostics, clamping, Yoga
projection, and incremental/full evaluation equivalence.

## Incremental Layout Contract

`ControlInternals.layoutAffectingAttrNames` must cover every attribute name read by `Control.toLayout` that
can affect geometry:

```text
width
height
orientation
padding
margin
gap
spacing
alignItems
alignSelf
justifyContent
flexGrow
flexShrink
flexBasis
minWidth
minHeight
maxWidth
maxHeight
```

If implementation removes the `spacing` alias by changing all typed front doors to emit `gap`, then
`spacing` may be omitted from the final covered set only if the behavioral probe proves `toLayout` no longer
reads it.

The dirty-set guard must continue to prove:

- Geometry-driving names are included.
- Non-geometry style/content/state names are excluded.
- `AttrCategory.Layout` changes dirty layout even when the name is not in the covered set.
- Incremental layout output equals full layout output for the same frame.

## Text-Cache Metrics Contract

Public `FrameMetrics` fields retain their names:

```text
TextMeasureCacheHitCount
TextMeasureCacheMissCount
LayoutInvalidatedNodeCount
RemeasuredNodeCount
```

Required per-frame behavior:

| Frame class | Hit count | Miss count | Layout invalidated | Remeasured |
|---|---:|---:|---:|---:|
| Cold text-heavy | 0 | > 0 | implementation-dependent | implementation-dependent |
| Warm equivalent text-heavy | > 0 | 0 | 0 | 0 |
| Style-only over warm text | allowed only for prior-frame reuse | 0 | 0 | 0 |
| Idle | 0 | 0 | 0 | 0 |

Metric hit means prior-frame reuse: the key was resident before the frame's measurement window began.
Same-frame inserts must not be reported as hits on a cold frame.

## Compatibility Contract

- The feature must not introduce larger renderer-architecture refactors.
- No new runtime dependency is allowed.
- `FS.GG.UI.Layout` public API is expected to remain unchanged.
- `FS.GG.UI.Controls` public surface changes must be reflected in surface baselines.
- Existing cache-on/cache-off text-measure scene and layout parity must remain true.
