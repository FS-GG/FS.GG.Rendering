module Feature157NoClearDamageTests

open System
open Expecto
open FS.GG.UI.SkiaViewer
open FS.GG.UI.SkiaViewer.Host

let private rect x y width height : GlHost.ScissorRect =
    { X = x
      Y = y
      Width = width
      Height = height }

let private eligibility proof retained damage : GlHost.DamageRenderEligibility =
    { Proof = proof
      RetainedBacking = retained
      Damage = damage
      FrameWidth = 640
      FrameHeight = 480
      VisibleChange = true
      FullFrameInvalidation = false
      StaleDamage = false
      IncompleteDamage = false
      AmbiguousDamage = false
      ResourcesAvailable = true
      ParityAccepted = true }

[<Tests>]
let tests =
    testList
        "Feature157 no-clear damage-scissored decisions"
        [ test "accepts damage-scoped repaint only when proof, retained backing, resources, and parity pass" {
              let decision =
                  GlHost.decideDamageScopedRender
                      (eligibility
                          CompositorProof.ProofReadiness.Ready
                          GlHost.RetainedBackingStatus.CurrentBufferPreserved
                          [ rect 16 16 64 64 ])

              Expect.equal decision.Kind GlHost.DamageRenderDecisionKind.DamageScopedAccepted "accepted decision"
              Expect.equal decision.DamageArea (64 * 64) "damage area"
              Expect.equal decision.FallbackReason None "no fallback"
          }

          test "falls back when proof is missing or host-mismatched" {
              let missing =
                  GlHost.decideDamageScopedRender
                      (eligibility
                          CompositorProof.ProofReadiness.Missing
                          GlHost.RetainedBackingStatus.CurrentBufferPreserved
                          [ rect 16 16 64 64 ])

              let mismatch =
                  GlHost.decideDamageScopedRender
                      (eligibility
                          CompositorProof.ProofReadiness.HostMismatch
                          GlHost.RetainedBackingStatus.CurrentBufferPreserved
                          [ rect 16 16 64 64 ])

              Expect.equal missing.Kind GlHost.DamageRenderDecisionKind.FullRedraw "missing proof fallback"
              Expect.stringContains (missing.FallbackReason |> Option.defaultValue "") "missing present-path proof" "missing proof reason"
              Expect.equal mismatch.Kind GlHost.DamageRenderDecisionKind.FullRedraw "host mismatch fallback"
              Expect.stringContains (mismatch.FallbackReason |> Option.defaultValue "") "host-mismatched" "host mismatch reason"
          }

          test "Synthetic rejection fixture: rejects invalid damage and parity mismatch before accepting no-clear output" {
              // SYNTHETIC: pure rejection fixture exercises invalid damage and parity mismatch without host pixel artifacts.
              let invalid =
                  GlHost.decideDamageScopedRender
                      (eligibility
                          CompositorProof.ProofReadiness.Ready
                          GlHost.RetainedBackingStatus.CurrentBufferPreserved
                          [ rect -10 0 24 24 ])

              let parity =
                  let baseEligibility =
                      eligibility
                          CompositorProof.ProofReadiness.Ready
                          GlHost.RetainedBackingStatus.CurrentBufferPreserved
                          [ rect 16 16 64 64 ]

                  GlHost.decideDamageScopedRender
                      { baseEligibility with ParityAccepted = false }

              Expect.equal invalid.Kind GlHost.DamageRenderDecisionKind.FullRedraw "invalid damage fallback"
              Expect.stringContains (invalid.FallbackReason |> Option.defaultValue "") "out-of-bounds" "invalid reason"
              Expect.equal parity.Kind GlHost.DamageRenderDecisionKind.Rejected "parity mismatch rejection"
              Expect.stringContains (parity.FallbackReason |> Option.defaultValue "") "parity-mismatch" "parity reason"
          }

          test "Synthetic rejection fixture: empty no-change damage skips repaint without publishing accepted partial-redraw artifacts" {
              // SYNTHETIC: pure no-change fixture verifies zero accepted artifact publication.
              let decision =
                  let baseEligibility =
                      eligibility
                          CompositorProof.ProofReadiness.Ready
                          GlHost.RetainedBackingStatus.CurrentBufferPreserved
                          []

                  GlHost.decideDamageScopedRender
                      { baseEligibility with VisibleChange = false }

              Expect.equal decision.Kind GlHost.DamageRenderDecisionKind.SkipNoChange "skip"
              Expect.equal decision.DamageArea 0 "no damage area"
          }

          test "viewer and host diagnostics expose stable Feature157 decision tokens" {
              Expect.equal (Viewer.damageDecisionToken ViewerDamageDecision.DamageScopedAccepted) "damage-scoped-accepted" "viewer token"
              let diagnostic = Diagnostics.damageScopedDecision "full-redraw" (Some "missing-retained-content")
              Expect.stringContains diagnostic.Message "Feature157 damage render decision" "diagnostic message"
              Expect.equal diagnostic.Cause (Some "missing-retained-content") "diagnostic cause"
          } ]
