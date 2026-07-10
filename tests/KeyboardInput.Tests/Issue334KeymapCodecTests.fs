module Issue334KeymapCodecTests

// Issue 334 (epic 330) — `KeymapCodec`: JSON serialization/persistence for `Keymap`. A keymap
// round-trips through a versioned UTF-8 JSON envelope (`{ format, version, bindings }`), the inverse
// of `Scene/SceneCodec`. Red on the pre-334 build (no `KeymapCodec`). Covers the round-trip, envelope
// stability/determinism, and every structural decode error. JSON is the single contract format.

open System.Text
open Expecto
open FS.GG.UI.KeyboardInput

let private binding key command : KeyboardBinding = { Key = key; Command = command }

let private sampleKeymap =
    Keymap.ofBindings
        [ binding "ArrowUp" "MoveUp"
          binding "ArrowDown" "MoveDown"
          binding "Space" "Jump" ]

let private utf8 (bytes: byte[]) = Encoding.UTF8.GetString(bytes)

// Order-insensitive keymap equality: `toBindings` is key-ordered, so sort the expected side too.
let private expectSameBindings (expected: Keymap) (actual: Keymap) message =
    Expect.equal (Keymap.toBindings actual) (Keymap.toBindings expected) message

[<Tests>]
let tests =
    testList "Issue 334 keymap JSON codec (round-trip, versioned envelope)" [

        test "round-trips a keymap through bytes unchanged" {
            match KeymapCodec.decode (KeymapCodec.encode sampleKeymap) with
            | Ok decoded -> expectSameBindings sampleKeymap decoded "keymap → bytes → keymap is identity"
            | Error errors -> failtestf "expected a decoded keymap, got errors: %A" errors
        }

        test "round-trips the empty keymap" {
            match KeymapCodec.decode (KeymapCodec.encode Keymap.empty) with
            | Ok decoded -> Expect.equal (Keymap.count decoded) 0 "empty keymap stays empty"
            | Error errors -> failtestf "expected a decoded empty keymap, got errors: %A" errors
        }

        test "round-trips keys and commands that need JSON escaping" {
            let tricky = Keymap.ofBindings [ binding "Quote\"Key" "cmd\\with\ttabs\nand\"quotes" ]
            match KeymapCodec.decode (KeymapCodec.encode tricky) with
            | Ok decoded -> expectSameBindings tricky decoded "special characters survive the round-trip"
            | Error errors -> failtestf "expected a decoded keymap, got errors: %A" errors
        }

        test "encode is deterministic and carries the versioned envelope" {
            let bytes = KeymapCodec.encode sampleKeymap
            Expect.equal bytes (KeymapCodec.encode sampleKeymap) "equal keymaps encode byte-identically"
            let text = utf8 bytes
            Expect.stringContains text "\"format\":\"fsgg.keymap\"" "carries the format discriminator"
            Expect.stringContains text "\"version\":1" "carries the schema version"
            // Bindings are emitted in key order (ArrowDown < ArrowUp < Space).
            let firstKeyAt = text.IndexOf("ArrowDown")
            let lastKeyAt = text.IndexOf("Space")
            Expect.isLessThan firstKeyAt lastKeyAt "bindings are emitted in key order"
        }

        test "encode agrees with the exposed format constants" {
            Expect.equal KeymapCodec.formatId "fsgg.keymap" "formatId is the stable discriminator"
            Expect.equal KeymapCodec.formatVersion 1 "formatVersion is the current schema version"
        }

        test "decode rejects bytes that are not JSON" {
            match KeymapCodec.decode (Encoding.UTF8.GetBytes "not json {") with
            | Error [ MalformedJson _ ] -> ()
            | other -> failtestf "expected a single MalformedJson error, got %A" other
        }

        test "decode reports a missing envelope field" {
            let bytes = Encoding.UTF8.GetBytes """{ "format": "fsgg.keymap", "bindings": [] }"""
            match KeymapCodec.decode bytes with
            | Error [ MissingField "version" ] -> ()
            | other -> failtestf "expected MissingField \"version\", got %A" other
        }

        test "decode rejects an unknown format discriminator" {
            let bytes = Encoding.UTF8.GetBytes """{ "format": "other", "version": 1, "bindings": [] }"""
            match KeymapCodec.decode bytes with
            | Error [ UnsupportedFormat "other" ] -> ()
            | other -> failtestf "expected UnsupportedFormat \"other\", got %A" other
        }

        test "decode rejects an unsupported version" {
            let bytes = Encoding.UTF8.GetBytes """{ "format": "fsgg.keymap", "version": 99, "bindings": [] }"""
            match KeymapCodec.decode bytes with
            | Error [ UnsupportedVersion 99 ] -> ()
            | other -> failtestf "expected UnsupportedVersion 99, got %A" other
        }

        test "decode surfaces every malformed binding at once" {
            let bytes =
                Encoding.UTF8.GetBytes
                    """{ "format": "fsgg.keymap", "version": 1, "bindings": [ { "key": "A" }, { "command": "B" }, 7 ] }"""
            match KeymapCodec.decode bytes with
            | Error errors ->
                Expect.equal (List.length errors) 3 "all three offending entries are reported"
                Expect.isTrue (errors |> List.forall (fun e -> match e with | InvalidBinding _ -> true | _ -> false))
                    "each error is an InvalidBinding"
            | Ok _ -> failtest "expected decode to fail on malformed bindings"
        }

        test "decode collapses a duplicated key last-wins, as ofBindings does" {
            let bytes =
                Encoding.UTF8.GetBytes
                    """{ "format": "fsgg.keymap", "version": 1, "bindings": [ { "key": "Space", "command": "Jump" }, { "key": "Space", "command": "Fire" } ] }"""
            match KeymapCodec.decode bytes with
            | Ok decoded ->
                Expect.equal (Keymap.count decoded) 1 "the duplicate key collapses to one binding"
                Expect.equal (Keymap.tryFind "Space" decoded) (Some "Fire") "the last binding wins"
            | Error errors -> failtestf "expected a decoded keymap, got errors: %A" errors
        }
    ]
