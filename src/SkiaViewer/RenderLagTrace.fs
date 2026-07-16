namespace FS.GG.UI.SkiaViewer

open System
open System.Diagnostics
open System.Globalization

module internal RenderLagTrace =
    /// S3 (Feature 175): one structured live-trace event — the event name plus its key/value fields
    /// (e.g. focus/hover/scroll resolution, binding dispatch, model-update/view timing).
    type TraceEvent = { Event: string; Fields: (string * string) list }

    let private stderrEnabled =
        String.Equals(Environment.GetEnvironmentVariable("FS_GG_RENDER_LAG_TRACE"), "1", StringComparison.Ordinal)

    // S3 read-back path: an in-memory ring that captures emitted events so a test or tool can OBSERVE
    // live state programmatically — without the env var and without a repack-to-instrument loop (the
    // friction that made diagnosing the Feature-175 focus lag slow). Independent of the stderr toggle.
    let private captured = System.Collections.Concurrent.ConcurrentQueue<TraceEvent>()
    let mutable private capturing = false

    /// Begin in-memory capture (clears any prior buffer); pair with `drainCapture`. The buffer is
    /// process-global, so a deterministic test should assert on the PRESENCE of its uniquely-named
    /// events rather than an exact list (other activity may emit concurrently).
    let startCapture () =
        captured.Clear()
        capturing <- true

    /// Stop capture and return the events recorded since `startCapture`, in emission order.
    let drainCapture () =
        capturing <- false
        let events = captured.ToArray() |> List.ofArray
        captured.Clear()
        events

    let emit eventName fields =
        if capturing then
            captured.Enqueue { Event = eventName; Fields = fields }

        if stderrEnabled then
            let fieldsText =
                fields
                |> List.map (fun (name, value) -> $"{name}={value}")
                |> String.concat " "

            let suffix = if String.IsNullOrWhiteSpace fieldsText then "" else " " + fieldsText
            let ts = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
            let ticks = Stopwatch.GetTimestamp()
            Console.Error.WriteLine($"FS_GG_RENDER_LAG_TRACE ts={ts} ticks={ticks} event={eventName}{suffix}")
