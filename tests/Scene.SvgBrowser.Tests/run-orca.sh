#!/usr/bin/env bash
set -euo pipefail
fixture="${1:?built browser fixture path is required}"
output="${2:?observation output path is required}"
command -v dbus-run-session >/dev/null
command -v Xvfb >/dev/null
command -v orca >/dev/null
mkdir -p "$(dirname "$output")"
work="$(mktemp -d "${TMPDIR:-/tmp}/scene-svg-orca.XXXXXX")"
trap 'for log in "$work"/orca.stdout "$work"/orca.stderr; do if [[ -s "$log" ]]; then cat "$log"; fi; done; rm -rf "$work"' EXIT
raw="$work/orca-speech.jsonl"
journey="$work/journey.json"

dbus-run-session -- bash -c '
  set -euo pipefail
  export DISPLAY=:97
  export GNOME_ACCESSIBILITY=1
  export NO_AT_BRIDGE=0
  export GTK_MODULES=gail:atk-bridge
  export SVG_SCENE_ORCA_SPEECH_LOG="$2"
  Xvfb "$DISPLAY" -screen 0 1280x800x24 >"$1/xvfb.log" 2>&1 &
  xvfb_pid=$!
  trap '\''kill "$orca_pid" "$xvfb_pid" 2>/dev/null || true'\'' EXIT
  sleep 1
  gsettings set org.gnome.desktop.interface toolkit-accessibility true
  gsettings set org.gnome.desktop.a11y.applications screen-reader-enabled true
  orca --replace >"$1/orca.stdout" 2>"$1/orca.stderr" &
  orca_pid=$!
  sleep 3
  node "$3/orca-test.mjs" --out "$4"
  kill -0 "$orca_pid"
  sleep 2
' _ "$work" "$raw" "$fixture" "$journey"

python3 - "$raw" "$journey" "$output" <<'PY'
import json, os, pathlib, re, sys
raw_path, journey_path, output_path = map(pathlib.Path, sys.argv[1:])
journey = json.loads(journey_path.read_text())
speech = [json.loads(line)["text"] for line in raw_path.read_text(errors="replace").splitlines() if line.strip()]
joined = "\n".join(speech).lower()
required = {
    "betaControl": "beta unit" in joined,
    "sceneRoot": "svg foundation scene" in joined,
    "alphaSelection": "alpha unit" in joined,
    "studioRectangleControl": "rectangle" in joined,
    "studioSelection": "rectangle created and selected" in joined,
    "studioValidation": "validation error" in joined and "finite translation" in joined,
}
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
)
result = "pass" if all(required.values()) and negative and studio_agreement else "fail"
version = os.environ.get("SVG_SCENE_ORCA_VERSION", "system-orca")
evidence = {
    "schema": "fsgg.svg-scene.orca-observation/v1",
    "result": result,
    "assistiveTechnology": {"name": "Orca", "version": version, "transport": "AT-SPI2"},
    "environment": {"display": "Xvfb", "sessionBus": "isolated dbus-run-session", "browser": "Playwright Chromium headed with forced renderer accessibility through AT-SPI2"},
    "candidate": {"sourceDigest": os.environ["SVG_SCENE_AT_SOURCE_DIGEST"], "packageDigest": os.environ["SVG_SCENE_AT_PACKAGE_DIGEST"]},
    "journey": journey,
    "announcements": required,
    "negativeControl": {"nonInteractiveDecorationExcludedFromKeyboardFocus": journey["negativeControl"]["nonInteractiveDecorationFocusable"] is False},
    "semanticIdentityAgreement": identity_agreement,
    "studioStateAgreement": studio_agreement,
    "speechOutput": speech,
    "claims": {"actualAssistiveTechnologyProcessObserved": True, "utterancesCapturedAtOrcaSpeechBoundary": True, "domOrAccessibilityTreeSubstitution": False},
}
pathlib.Path(output_path).write_text(json.dumps(evidence, indent=2) + "\n")
output_path.with_suffix(".log").write_text("\n".join(speech) + "\n")
if result != "pass":
    raise SystemExit(f"Orca observation incomplete: announcements={required} negative={negative} studio={studio_agreement}; debug={raw_path}")
print(f"orca-observation: result=pass announcements={','.join(k for k,v in required.items() if v)} evidence={output_path}")
PY
