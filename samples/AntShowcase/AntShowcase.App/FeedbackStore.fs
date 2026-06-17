/// App-edge persistence for user feedback (Principle IV: all I/O at the edge; Core stays
/// pure). Feedback is appended one tab-separated line per submission to a log file, so it
/// survives the session and can be acted upon later via the `feedback` CLI subcommand.
module AntShowcase.App.FeedbackStore

open System.IO
open AntShowcase.Core.Model

/// Directory + file holding accumulated feedback (transient runtime output, gitignored).
let dir = "artifacts/ant-showcase"
let path = dir + "/feedback.jsonl"

/// Load previously-saved feedback, newest first (matching the in-model ordering).
let load (): FeedbackEntry list =
    if File.Exists path then
        File.ReadAllLines path
        |> Array.toList
        |> List.choose decodeFeedbackLine
        |> List.rev
    else
        []

/// Append one freshly-submitted entry to the log.
let append (entry: FeedbackEntry): unit =
    Directory.CreateDirectory dir |> ignore
    File.AppendAllText(path, encodeFeedbackLine entry + "\n")

/// Clear all saved feedback (acting on it / triaging to empty).
let clear (): unit =
    if File.Exists path then File.Delete path
