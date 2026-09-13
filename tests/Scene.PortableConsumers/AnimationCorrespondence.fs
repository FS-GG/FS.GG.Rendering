module AnimationCorrespondence

open System
open FS.GG.UI.Scene

let private ms (value: float) = TimeSpan.FromMilliseconds value
let private scalar time value = { Time = ms time; Value = ScalarValue value; EasingToNext = Linear }

let run () =
    let source =
        { Id = "portable-clip"
          Duration = ms 100.0
          Tracks =
            [ PositionX, [ scalar 0.0 0.0; scalar 100.0 20.0 ]
              Opacity, [ scalar 0.0 0.0; scalar 100.0 1.0 ]
              PathMorph "body",
                [ { Time = TimeSpan.Zero; Value = PathValue [ 0.0, 0.0; 10.0, 0.0 ]; EasingToNext = Linear }
                  { Time = ms 100.0; Value = PathValue [ 0.0, 10.0; 20.0, 10.0 ]; EasingToNext = Linear } ] ]
          Cues =
            [ { Id = "start"; Time = TimeSpan.Zero; Payload = "ui.start" }
              { Id = "impact"; Time = ms 50.0; Payload = "sfx.impact" } ]
          Loop = PingPong 2 }
    let clip = AnimationClip.validate source |> Result.defaultWith (fun issues -> failwith $"clip rejected: {issues}")
    [ TimeSpan.Zero, Seek
      ms 50.0, LiveAdvance TimeSpan.Zero
      ms 100.0, LiveAdvance(ms 50.0)
      ms 150.0, LiveAdvance(ms 100.0)
      ms 250.0, Paused ]
    |> List.map (fun (elapsed, mode) ->
        let sample = AnimationClip.sample elapsed mode clip
        let scalarValue property =
            match sample.Values[property] with ScalarValue value -> string value | _ -> failwith "unexpected value"
        let cues = sample.Cues |> List.map (fun occurrence -> $"{occurrence.Cue.Id}@{occurrence.Iteration}:{occurrence.Direction}") |> String.concat ","
        let complete = if sample.IsComplete then "true" else "false"
        $"{elapsed.TotalMilliseconds}|{sample.LocalTime.TotalMilliseconds}|{sample.Iteration}|{sample.Direction}|{complete}|{scalarValue PositionX}|{scalarValue Opacity}|{cues}")
    |> String.concat "\n"
    |> fun value -> value + "\n"
