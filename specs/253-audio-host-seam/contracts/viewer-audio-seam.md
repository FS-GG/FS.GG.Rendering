# Contract: the viewer audio seam

Package: `FS.GG.UI.SkiaViewer` · Depends on: `FS.GG.Audio.Core` (data only) · Issue: #245

## The effect

```fsharp
type ViewerEffect =
    | ...                                        // the fourteen pre-existing cases, unchanged
    | PlayAudio of effects: AudioEffect list
```

`PlayAudio` is **pure data**: no device handle, no stream, no closure. Effects within one batch are
realized in list order; batches are realized in the order the host emitted them.

## The launches

```fsharp
val runApp                          : ViewerOptions -> GeneratedAppHost<'m,'g> -> Result<...>
val runAppWithWindowBehavior        : ViewerOptions -> ViewerWindowBehaviorRequest -> GeneratedAppHost<'m,'g> -> Result<...>
val runAppWithAudio                 : ViewerOptions -> (AudioEffect list -> unit) -> GeneratedAppHost<'m,'g> -> Result<...>
val runAppWithWindowBehaviorAndAudio: ViewerOptions -> ViewerWindowBehaviorRequest -> (AudioEffect list -> unit) -> GeneratedAppHost<'m,'g> -> Result<...>
```

All four share one private body. The two audio-free launches pass `ignore` as the sink — so
"`runApp` discards audio" is a consequence of the implementation, not a claim about it.

## Who owns what

| Concern | Owner | Why |
|---|---|---|
| The vocabulary (`AudioEffect`, `SoundId`, `Bus`) | `FS.GG.Audio.Core` | Data only. Safe for a rendering package to reference. |
| The device (`IAudioBackend`, OpenAL, buses) | `FS.GG.Audio.Host` / `.Engine` | **Never referenced by SkiaViewer.** A rendering core that drags in an audio device stack is a worse defect than a missing seam. |
| The backend's lifetime | the caller | `runAppWithAudio` takes a sink, not a backend. The viewer never creates, owns, or disposes an audio device. |
| `SoundId -> bytes` | the product | The framework does not own the id → asset mapping (FS.GG.Audio FR-005), as it does not own per-game stat mapping in symbology. |

## Invariants

- **Additive.** No member removed or resigned. Every existing `GeneratedAppHost` literal compiles
  unchanged — the sink is a launch parameter, not a record field.
- **Hardware-free by default.** The launch that opens no window plays nothing. The evidence and
  offscreen paths discard `PlayAudio`. `OpenAlBackend.create` degrades to the record-only backend and
  never throws into game code.
- **Observable.** `GeneratedAppHost.audioRequests : ViewerEffect list -> AudioEffect list` flattens a
  frame's batches in dispatch order, so what a product requested is assertable with no window and no
  device. Composed with `Audio.interpret` it yields the same `AudioEvidence` the record-only path
  always produced.

## Non-goals

The pointer-aware `InteractiveViewerHost` has no sink and discards `PlayAudio`. Audio is a
game-family seam; the interactive host is the controls family. This is a decision, not an oversight,
and it is commented at the discard site.
