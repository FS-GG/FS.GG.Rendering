module Feature170DamageRegionUnionTests

open Expecto
open FS.GG.UI.Scene

let private frame: Rect =
    { X = 0.0
      Y = 0.0
      Width = 100.0
      Height = 100.0 }

let private rect x y w h : Rect =
    { X = x
      Y = y
      Width = w
      Height = h }

[<Tests>]
let tests =
    testList
        "Feature170 damage region union"
        [ test "dirty region area uses clipped true union not summed overlap" {
              let damage =
                  RetainedInspection.damageRegion
                      "overlap"
                      frame
                      [ rect 0.0 0.0 40.0 40.0
                        rect 20.0 20.0 40.0 40.0
                        rect -10.0 70.0 30.0 15.0 ]
                      [ "content" ]
                      [ "panel" ]
                      1
                      0
                      2
                      (Some "fixture")
                      (Some 50.0)

              Expect.equal damage.UnionArea 3100 "overlap counted once and off-frame area clipped"
              Expect.equal damage.VisibleDirtyArea 3100 "visible dirty area mirrors true union"
              Expect.equal damage.DamageStatus DamageInspectionStatus.Localized "under threshold is localized"
              Expect.isSome damage.UnionBounds "union bounds available"
          }

          test "empty broad and full-surface damage statuses are explicit" {
              let empty =
                  RetainedInspection.damageRegion "empty" frame [] [] [] 0 0 3 None (Some 10.0)

              let broad =
                  RetainedInspection.damageRegion "broad" frame [ rect 0.0 0.0 80.0 80.0 ] [ "content" ] [ "panel" ] 1 0 0 None (Some 10.0)

              let full =
                  RetainedInspection.damageRegion "full" frame [ rect 0.0 0.0 100.0 100.0 ] [ "root" ] [ "root" ] 1 0 0 None (Some 99.0)

              Expect.equal empty.DamageStatus DamageInspectionStatus.Empty "empty damage explicit"
              Expect.equal broad.DamageStatus DamageInspectionStatus.Broad "over threshold broad"
              Expect.equal full.DamageStatus DamageInspectionStatus.FullSurface "full frame damage explicit"
              Expect.equal full.DirtyPercentage 100.0 "full dirty percentage"
          } ]
