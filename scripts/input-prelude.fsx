#I "../src/KeyboardInput/bin/Debug/net10.0"
#r "FS.GG.UI.KeyboardInput.dll"

open FS.GG.UI.KeyboardInput

let model0, initEffects = Keyboard.init [ { Key = "A"; Command = "accept" } ]

let model1, effects1 =
    Keyboard.update (KeyDown "A") model0

let model2, effects2 =
    Keyboard.update (KeyUp "A") model1

printfn "pressed keys: %A" model2.PressedKeys
printfn "key effects: %A %A %A" initEffects effects1 effects2
