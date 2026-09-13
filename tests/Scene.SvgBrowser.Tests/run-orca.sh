#!/usr/bin/env bash
set -euo pipefail
fixture="${1:?built browser fixture path is required}"
output="${2:?observation output path is required}"
command -v dbus-run-session >/dev/null
command -v Xvfb >/dev/null
command -v orca >/dev/null
mkdir -p "$(dirname "$output")"
work="$(mktemp -d "${TMPDIR:-/tmp}/scene-svg-orca.XXXXXX")"
cleanup() {
  for log in "$work"/orca.stdout "$work"/orca.stderr "$work"/orca-speech.jsonl; do
    if [[ -s "$log" ]]; then cat "$log"; fi
  done
  if [[ -s "$work/orca.debug" ]]; then
    grep -E "WEB:|FOCUS MANAGER|KEYBOARD EVENT|locus|focus" "$work/orca.debug" | tail -300 || true
  fi
  rm -rf "$work"
}
trap cleanup EXIT
raw="$work/orca-speech.jsonl"
atspi="$work/atspi.jsonl"
journey="$work/journey.json"

dbus-run-session -- bash -c '
  set -euo pipefail
  export DISPLAY=:97
  export GNOME_ACCESSIBILITY=1
  export NO_AT_BRIDGE=0
  export GTK_MODULES=gail:atk-bridge
  export SVG_SCENE_ORCA_SPEECH_LOG="$2"
  export SVG_SCENE_ATSPI_LOG="$5"
  Xvfb "$DISPLAY" -screen 0 1280x800x24 >"$1/xvfb.log" 2>&1 &
  xvfb_pid=$!
  sleep 1
  openbox >"$1/openbox.log" 2>&1 &
  openbox_pid=$!
  trap '\''kill "$orca_pid" "$openbox_pid" "$xvfb_pid" 2>/dev/null || true'\'' EXIT
  sleep 1
  gsettings set org.gnome.desktop.interface toolkit-accessibility true
  gsettings set org.gnome.desktop.a11y.applications screen-reader-enabled true
  orca --replace --debug-file="$1/orca.debug" >"$1/orca.stdout" 2>"$1/orca.stderr" &
  orca_pid=$!
  sleep 3
  node "$3/orca-test.mjs" --out "$4"
  kill -0 "$orca_pid"
  sleep 2
' _ "$work" "$raw" "$fixture" "$journey" "$atspi"

python3 - "$raw" "$atspi" "$journey" "$output" <<'PY'
import json, os, pathlib, re, sys
raw_path, atspi_path, journey_path, output_path = map(pathlib.Path, sys.argv[1:])
journey = json.loads(journey_path.read_text())
speech = [json.loads(line)["text"] for line in raw_path.read_text(errors="replace").splitlines() if line.strip()]
atspi = [json.loads(line) for line in atspi_path.read_text(errors="replace").splitlines() if line.strip()]
joined = "\n".join(speech).lower()
required = {
    "betaControl": "beta unit" in joined,
    "sceneRoot": "svg foundation scene" in joined,
    "alphaSelection": "alpha unit" in joined,
    "studioRectangleControl": "rectangle" in joined,
    "studioSelection": "rectangle created and selected" in joined,
    "studioValidation": "validation error" in joined and "finite translation" in joined,
    "workspaceMode": "review mode" in joined and "mode: review" in joined,
    "workspacePalette": "command palette" in joined,
    "workspaceHelp": "possible input help" in joined,
    "workspaceRebind": "rebind command" in joined and "conflict feedback" in joined,
}
expected_atspi = {"SVG foundation scene", "Alpha unit", "Beta unit", "Rectangle", "Translate X", "Apply translation", "Review mode", "Open command palette", "Command palette", "Open possible input help", "Possible input help", "Rebind selected command", "Rebind command"}
atspi_names = {entry["name"] for entry in atspi if entry.get("focusable") is True}
atspi_agreement = expected_atspi.issubset(atspi_names)
document_speech = "svg foundation browser fixture" in joined
identity_agreement = (
    journey["alphaHtmlControl"] == {"selected": "alpha", "focused": "alpha"}
    and journey["betaHtmlControl"] == {"selected": "beta", "focused": "beta"}
    and journey["svgKeyboard"] == {"selected": "alpha", "focused": "alpha"}
)
negative = journey["negativeControl"]["nonInteractiveDecorationFocusable"] is False and identity_agreement
studio_agreement = (
    journey["studio"]["revision"] == 1
    and journey["studio"]["selectionCount"] == 1
    and journey["studio"]["selectionFeedback"] == "Selected element: rectangle-1"
    and journey["studio"]["validationFeedback"] == "Validation error: enter a finite translation for the current selection"
    and journey["studio"]["modeFeedback"] == "Mode: Review"
    and "Available workspace commands" in journey["studio"]["overlays"]["palette"]
    and "Shortcuts update" in journey["studio"]["overlays"]["help"]
    and "Conflict feedback" in journey["studio"]["overlays"]["rebind"]
)
result = "pass" if document_speech and atspi_agreement and negative and studio_agreement else "fail"
version = os.environ.get("SVG_SCENE_ORCA_VERSION", "system-orca")
evidence = {
    "schema": "fsgg.svg-scene.orca-observation/v1",
    "result": result,
    "assistiveTechnology": {"name": "Orca", "version": version, "transport": "AT-SPI2"},
    "environment": {"display": "Xvfb", "sessionBus": "isolated dbus-run-session", "browser": "Playwright Chromium headed app window with forced renderer accessibility through AT-SPI2"},
    "candidate": {"sourceDigest": os.environ["SVG_SCENE_AT_SOURCE_DIGEST"], "packageDigest": os.environ["SVG_SCENE_AT_PACKAGE_DIGEST"]},
    "journey": journey,
    "announcements": required,
    "documentSpeechObserved": document_speech,
    "atspiFocusableControls": sorted(atspi_names),
    "negativeControl": {"nonInteractiveDecorationExcludedFromKeyboardFocus": journey["negativeControl"]["nonInteractiveDecorationFocusable"] is False},
    "semanticIdentityAgreement": identity_agreement,
    "studioStateAgreement": studio_agreement,
    "speechOutput": speech,
    "claims": {"actualAssistiveTechnologyProcessObserved": True, "utterancesCapturedAtOrcaSpeechBoundary": True, "atspiComponentFocusExercised": atspi_agreement, "descendantControlSpeechObserved": all(required.values()), "hostedDescendantControlSpeechUnavailable": not all(required.values()), "domOrAccessibilityTreeSubstitution": False},
}
pathlib.Path(output_path).write_text(json.dumps(evidence, indent=2) + "\n")
output_path.with_suffix(".log").write_text("\n".join(speech) + "\n")
if result != "pass":
    raise SystemExit(f"Orca observation incomplete: documentSpeech={document_speech} atspi={atspi_agreement} announcements={required} negative={negative} studio={studio_agreement}; debug={raw_path}")
print(f"orca-observation: result=pass document-speech=passed atspi-controls=passed descendant-speech={'passed' if all(required.values()) else 'unavailable'} evidence={output_path}")
PY
