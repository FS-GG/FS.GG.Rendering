# Controls catalog reference

This is the consumer-visible answer to **"which attributes and events does control
*X* support?"** — without reflecting over the assembly or reading framework source.

There are two discovery paths, and both are reachable from this generated project:

1. **Programmatic — the `Catalog` discovery API** (complete, never stale; works from
   IntelliSense and at runtime).
2. **Static — this reference + the bundled `docs/api-surface/Controls/*.fsi`** signatures
   (open them in your editor; they ship with substantive `///` docs in IntelliSense too).

> You never need reflection. The `Catalog` API below reports every control's contract as
> data, and the `docs/api-surface/Controls/` bundle carries the typed `Props` and legacy
> builder signatures on disk.

## Programmatic discovery — `FS.Skia.UI.Controls.Catalog`

Every function is documented in `docs/api-surface/Controls/Catalog.fsi` (and in IntelliSense).
The ones you reach for when authoring:

| Function | Returns | Use it to |
|----------|---------|-----------|
| `Catalog.knownControlKinds ()` | `StandardControlKind list` | enumerate every control kind the package supports |
| `Catalog.requiredAttributes kind` | `StandardAttributeName list` | the attributes a control of `kind` MUST carry |
| `Catalog.supportedAttributes kind` | `StandardAttributeName list` | the COMPLETE set of attributes `kind` accepts |
| `Catalog.supportedEvents kind` | `StandardEventKind list` | the events `kind` can bind |
| `Catalog.markdownSummary ()` | `string` | render the full per-control catalog as Markdown at runtime |
| `Catalog.supportedControls` | `ControlDefinition list` | the raw per-control records (module, typed module, attributes, events, visual states, accessibility) |

### Worked example — a control's complete attribute set, no reflection

```fsharp
open FS.Skia.UI.Controls

// "What does a text box accept?" — answered from the discovery API:
let required = Catalog.requiredAttributes StandardControlKind.TextBox   // [ value ]
let supported = Catalog.supportedAttributes StandardControlKind.TextBox // value + the common set
let events = Catalog.supportedEvents StandardControlKind.TextBox        // [ onChanged ]

// Or render the entire catalog (all controls) to Markdown and read/print it:
printfn "%s" (Catalog.markdownSummary ())
```

## Static reference — the starter-demonstrated controls

These are the controls the generated `View.fs` authors through the typed Props front door
(`FS.Skia.UI.Controls.Typed`). Every control here also accepts the **common attributes**
shared across kinds: `enabled`, `visible`, `width`, `height`, `padding`, `style`, `theme`,
`accessibility`.

| Control | Typed module | Legacy module | Required attributes | Events |
|---------|--------------|---------------|---------------------|--------|
| Text Block | `TextBlock` | `TextBlock` | `text` | — |
| Rich Text | `RichText` | `RichText` | `runs` | — |
| Text Box | `TextBox` | `TextBox` | `value` | `onChanged` |
| Button | `Button` | `Button` | `text` | `onClick` |
| Check Box | `CheckBox` | `CheckBox` | `text` | `onChanged` |
| Slider | `Slider` | `Slider` | `value` | `onChanged` |
| Line Chart | `LineChart` | `LineChart` | `series` | `onSelected` |
| Graph View | `GraphView` | `GraphView` | `nodes` | `onSelected` |
| Data Grid | `DataGrid` | `DataGrid` | `columns`, `rows` | `onSelected`, `onFocusChanged`, `onSortChanged` |
| Stack | `Stack` | `Stack` | `children` | — |

The package supports **52** controls in total. For the kinds not listed above, enumerate them
with `Catalog.knownControlKinds ()` and read each contract with `Catalog.requiredAttributes` /
`Catalog.supportedAttributes` / `Catalog.supportedEvents`, or render them all at once with
`Catalog.markdownSummary ()`.

## Controls without a typed module are still fully supported

A control is **not** unsupported merely because you author it through the legacy builder
rather than the typed `Props` front door. The `Catalog` API reports the full attribute and
event contract for every control regardless of how you construct it, and the legacy builder
surface is fully documented in `docs/api-surface/Controls/`. The typed front door is the
*recommended, compiler-guided* path; the legacy builders remain supported and documented.

## Authoring an interactive controls app — the host seam

Constructing controls is only half of "authoring a controls app". The interactive host entry
point is `FS.Skia.UI.Controls.Elmish.ControlsElmish.runInteractiveApp`, wired in the generated
`Program.fs`. A typed (`Widget<'msg>`-returning) view can be wired straight through with
`ControlsElmish.programOfWidget` (or `ControlsElmish.widgetView`), which lowers via
`Widget.toControl` for you. See `docs/api-surface/Controls/` (the `Elmish` and `Widget`
signatures) for the host surface.
