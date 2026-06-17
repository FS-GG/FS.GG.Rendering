# Data Model: Layout Attributes and Metrics Green

## Layout Authoring Value

Consumer-supplied attribute that can change geometry when lowered from `Control<'msg>` to `LayoutIntent`.

| Field | Description |
|---|---|
| `Name` | Stable attribute name, for example `padding`, `gap`, or `flexGrow`. |
| `Category` | `AttrCategory.Layout` for public builders; existing category-channel invalidation still applies. |
| `Value` | Numeric value for spacing/flex/size fields or alignment value for alignment fields. |
| `CanonicalName` | Preferred public name used by `Attr` builders. |
| `Aliases` | Compatibility names that must keep working, currently including `spacing` as a gap alias if typed surfaces still emit it. |
| `LayoutIntentField` | Target field in `FS.GG.UI.Layout.LayoutIntent`. |
| `DefaultSource` | Either current Controls compatibility default or `LayoutDefaults.layoutIntent`. |
| `GeometryAffecting` | Always `true` for the values in this feature. |

### Supported Values

| Canonical name | Value shape | LayoutIntent field | Notes |
|---|---|---|---|
| `width` | float | `Size.Width` | Existing builder; unchanged. |
| `height` | float | `Size.Height` | Existing builder; unchanged. |
| `orientation` | text | `Direction` | Existing Stack helper; unchanged. |
| `padding` | float | `Padding` | Uniform all edges; explicit zero overrides compatibility default. |
| `margin` | float | `Margin` | Uniform all edges; omitted remains zero. |
| `gap` | float | `Gap.Row` and `Gap.Column` | Uniform row/column; `spacing` may remain an alias. |
| `alignItems` | `LayoutAlign` | `AlignItems` | Container child cross-axis alignment. |
| `alignSelf` | `LayoutAlign` | `AlignSelf` | Per-child override. |
| `justifyContent` | `LayoutAlign` | `JustifyContent` | Main-axis distribution. |
| `flexGrow` | float | `FlexGrow` | Nonnegative value; explicit zero honored. |
| `flexShrink` | float | `FlexShrink` | Nonnegative value; explicit zero pins a child against shrink. |
| `flexBasis` | float | `FlexBasis` | Nonnegative basis when supplied. |
| `minWidth` | float | `MinSize.Width` | Width clamp. |
| `minHeight` | float | `MinSize.Height` | Height clamp. |
| `maxWidth` | float | `MaxSize.Width` | Width clamp. |
| `maxHeight` | float | `MaxSize.Height` | Height clamp. |

### Validation Rules

- Omitted values keep current compatibility geometry.
- Explicit zero values are authored values and must override compatibility defaults.
- Negative and invalid numeric values follow existing Layout normalization/diagnostic behavior rather than
  throwing in Controls.
- Padding, margin, and gap are uniform for this feature.
- Min/max constraints use existing Layout clamp semantics.
- Last-writer-wins attribute behavior remains unchanged.

## Default Layout Behavior

Compatibility baseline for a control tree that does not author the new layout values.

| Field | Description |
|---|---|
| `ImplicitPadding` | Current Controls boundary padding used by `toLayout` when no `padding` is authored. |
| `ImplicitGap` | Current Controls boundary gap used by `toLayout` when neither `gap` nor `spacing` is authored. |
| `LayoutDefaults` | Lower-level `LayoutDefaults.layoutIntent` values used for fields not overridden by Controls compatibility behavior. |
| `CompatibilityBounds` | Bounds produced today for representative no-authored-value screens. |

### Validation Rules

- Representative no-authored-value trees must keep byte-identical bounds before and after the feature.
- Explicit authored values equal to compatibility defaults must not create extra invalidation or bounds drift.
- Explicit authored values different from compatibility defaults must affect bounds in the same frame.

## Layout Invalidation Set

The exact set of attribute names that make incremental layout re-measure geometry.

| Field | Description |
|---|---|
| `CoveredNames` | `ControlInternals.layoutAffectingAttrNames`. |
| `ProbeCorpus` | Names toggled by `Feature101LayoutDriftGuardTests`. |
| `DiscoveredNames` | Names proven by the behavioral probe to change real `evaluateLayout` output. |
| `CategoryChannel` | Any changed `AttrCategory.Layout` attribute, independent of name membership. |

### Validation Rules

- `DiscoveredNames` must equal `CoveredNames`.
- Every supported layout authoring value and compatibility alias must appear in both the probe corpus and
  the covered name set when it is read by `toLayout`.
- Visual-only/content-only names must remain excluded unless they also change a geometry-driving value.
- Removing an authored geometry value invalidates layout just like changing it.

## Shell Chrome Layout

Representative application frame proving the authoring surface solves the immediate shell layout blocker.

| Field | Description |
|---|---|
| `Header` | Fixed-height, non-shrinking top chrome. |
| `Footer` | Fixed-height, non-shrinking bottom chrome. |
| `Navigation` | Fixed-width, non-shrinking side chrome. |
| `ContentRegion` | Flexible region with `flexGrow` and bounded size. |
| `ViewportSizes` | Window sizes validated by the test. |

### Validation Rules

- Header/footer/nav keep authored dimensions.
- Content receives remaining space.
- Overflowing content remains bounded so chrome stays visible.
- Incremental layout of shell geometry remains byte-identical to a full layout of the same frame.

## Text-Cache Metric Window

Frame-level accounting boundary for text measurement reuse.

| Field | Description |
|---|---|
| `FrameStartResidentKeys` | Text-measure keys present before a frame begins measuring. |
| `FrameInsertedKeys` | Keys first inserted during the current frame. |
| `HitCount` | Count of measurements served from `FrameStartResidentKeys`. |
| `MissCount` | Count of cold measurement work for keys not resident at frame start. |
| `LayoutInvalidatedNodeCount` | Existing pre-propagation dirty-set metric. |
| `RemeasuredNodeCount` | Existing post-propagation re-measured node count. |

### State Transitions

1. **Cold frame**: cache has no resident keys at frame start; text-heavy measurement reports `HitCount = 0`
   and `MissCount > 0`.
2. **Warm equivalent frame**: same text after the cold frame; reports `HitCount > 0` and `MissCount = 0`.
3. **Style-only frame over warm text**: reports `MissCount = 0`, `LayoutInvalidatedNodeCount = 0`, and
   `RemeasuredNodeCount = 0`.
4. **Idle frame**: reports zero text hits, zero text misses, and zero layout invalidations.

### Validation Rules

- Repeated identical text inside a cold frame must not produce metric hits unless the key was resident from
  an earlier frame.
- The cache-on and always-miss oracle must remain scene/bounds byte-identical.
- Re-running the same frame script must produce identical metric tuples.
