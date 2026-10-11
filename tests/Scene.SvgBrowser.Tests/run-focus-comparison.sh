#!/usr/bin/env bash
set -euo pipefail
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
original="$repo/_focus-original"
work="${1:?Usage: run-focus-comparison.sh ABSOLUTE_FRESH_HOSTED_OUTPUT_DIRECTORY}"
[[ "$work" = /* && -d "$work/evidence" && -f "$work/evidence/hosted-attempt.json" && ! -e "$work/original" && ! -e "$work/candidate" ]] || { echo 'Fresh hosted attempt/output binding absent' >&2; exit 1; }
evidence="$work/evidence"
finish() {
  local status=$?
  trap - EXIT
  python3 "$repo/tests/Scene.SvgBrowser.Tests/focus-comparison-inputs.py" retain "$work" "$evidence" "$status" || { [[ "$status" != 0 ]] || status=1; }
  exit "$status"
}
trap finish EXIT
[[ "$(node --version)" == v26.10.0 ]] || { echo 'Selected Node differs' >&2; exit 1; }
python3 "$repo/tests/Scene.SvgBrowser.Tests/focus-comparison-inputs.py" check "$repo" "$original" "$evidence"
for variant in original candidate; do
  mkdir "$work/$variant"
  chmod 700 "$work/$variant"
  printf '%s\n' '{"sdk":{"version":"10.0.401","rollForward":"disable","allowPrerelease":false}}' > "$work/$variant/global.json"
  (
    cd "$work/$variant"
    dotnet --version > sdk-version.log
    [[ "$(cat sdk-version.log)" == 10.0.401 ]] || { echo "Actual $variant SDK differs" >&2; exit 1; }
    if [[ "$variant" == original ]]; then
      # Unmodified historical helper inherits the same closed private SDK context.
      bash "$original/tests/Scene.SvgBrowser.Tests/build-fixture.sh" "$work/$variant/fixture"
    else
      SVG_SCENE_FOCUS_SDK=10.0.401 bash "$repo/tests/Scene.SvgBrowser.Tests/build-fixture.sh" "$work/$variant/fixture"
    fi
  )
done
python3 "$repo/tests/Scene.SvgBrowser.Tests/focus-comparison-inputs.py" attest "$repo" "$original" "$evidence" "$work"
node "$repo/tests/Scene.SvgBrowser.Tests/focus-comparison.mjs" "$evidence/comparison-inputs.json" "$evidence/comparison-result.json" >"$work/comparison.log" 2>&1
