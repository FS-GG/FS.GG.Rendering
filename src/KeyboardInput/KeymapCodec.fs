namespace FS.GG.UI.KeyboardInput

open System
open System.IO
open System.Text.Json

type KeymapCodecError =
    | MalformedJson of detail: string
    | UnsupportedFormat of format: string
    | UnsupportedVersion of version: int
    | MissingField of field: string
    | InvalidBinding of detail: string

/// JSON serialization/persistence for `Keymap`. See `KeymapCodec.fsi` for the contract. Built on
/// `System.Text.Json` (shared framework — no package dependency), so the codec adds nothing to the
/// package's dependency set: JSON is the single contract format here.
module KeymapCodec =

    let formatId = "fsgg.keymap"

    let formatVersion = 1

    let encode (keymap: Keymap) : byte[] =
        use stream = new MemoryStream()
        use writer = new Utf8JsonWriter(stream)
        writer.WriteStartObject()
        writer.WriteString("format", formatId)
        writer.WriteNumber("version", formatVersion)
        writer.WriteStartArray("bindings")

        for binding in Keymap.toBindings keymap do
            writer.WriteStartObject()
            writer.WriteString("key", binding.Key)
            writer.WriteString("command", binding.Command)
            writer.WriteEndObject()

        writer.WriteEndArray()
        writer.WriteEndObject()
        writer.Flush()
        stream.ToArray()

    // A JSON string member if present and of string kind, else None.
    let private stringMember (name: string) (element: JsonElement) =
        match element.TryGetProperty(name) with
        | true, value when value.ValueKind = JsonValueKind.String ->
            match value.GetString() with
            | null -> None
            | text -> Some text
        | _ -> None

    // Validate one `bindings` entry, accumulating into `bindings`/`errors` by index.
    let private readBinding (index: int) (element: JsonElement) (bindings: ResizeArray<KeyboardBinding>) (errors: ResizeArray<KeymapCodecError>) =
        if element.ValueKind <> JsonValueKind.Object then
            errors.Add(InvalidBinding $"bindings[{index}] is not an object")
        else
            match stringMember "key" element, stringMember "command" element with
            | Some key, Some command -> bindings.Add({ Key = key; Command = command })
            | None, _ -> errors.Add(InvalidBinding $"bindings[{index}] has no string 'key'")
            | _, None -> errors.Add(InvalidBinding $"bindings[{index}] has no string 'command'")

    let decode (bytes: byte[]) : Result<Keymap, KeymapCodecError list> =
        let parsed =
            try
                Ok(JsonDocument.Parse(ReadOnlyMemory(bytes)))
            with :? JsonException as ex ->
                Error [ MalformedJson ex.Message ]

        match parsed with
        | Error errors -> Error errors
        | Ok document ->
            use document = document
            let root = document.RootElement

            if root.ValueKind <> JsonValueKind.Object then
                Error [ MalformedJson "the root is not a JSON object" ]
            else
                let format = root.TryGetProperty("format")
                let version = root.TryGetProperty("version")
                let bindings = root.TryGetProperty("bindings")

                match format, version, bindings with
                | (false, _), _, _ -> Error [ MissingField "format" ]
                | _, (false, _), _ -> Error [ MissingField "version" ]
                | _, _, (false, _) -> Error [ MissingField "bindings" ]
                | (true, format), (true, version), (true, bindings) ->
                    let formatValue =
                        match (if format.ValueKind = JsonValueKind.String then format.GetString() else format.ToString()) with
                        | null -> ""
                        | text -> text

                    if format.ValueKind <> JsonValueKind.String || formatValue <> formatId then
                        Error [ UnsupportedFormat formatValue ]
                    elif version.ValueKind <> JsonValueKind.Number then
                        Error [ MalformedJson "'version' is not a number" ]
                    else
                        match version.TryGetInt32() with
                        | false, _ -> Error [ MalformedJson "'version' is not an int32" ]
                        | true, value when value <> formatVersion -> Error [ UnsupportedVersion value ]
                        | true, _ ->
                            if bindings.ValueKind <> JsonValueKind.Array then
                                Error [ MalformedJson "'bindings' is not an array" ]
                            else
                                let accepted = ResizeArray<KeyboardBinding>()
                                let errors = ResizeArray<KeymapCodecError>()

                                bindings.EnumerateArray()
                                |> Seq.iteri (fun index element -> readBinding index element accepted errors)

                                if errors.Count > 0 then
                                    Error(List.ofSeq errors)
                                else
                                    Ok(Keymap.ofBindings (List.ofSeq accepted))
