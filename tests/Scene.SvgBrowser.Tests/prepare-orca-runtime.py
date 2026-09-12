#!/usr/bin/env python3
"""Prepare the exact Orca source used by the hosted browser qualification."""

from pathlib import Path
import sys


speech_source = Path(sys.argv[1])
meson_source = Path(sys.argv[2])
text = speech_source.read_text()

import_anchor = "from dataclasses import dataclass\n"
if import_anchor not in text:
    raise SystemExit("Orca speech module import anchor was not found")
text = text.replace(
    import_anchor,
    "from dataclasses import dataclass\nimport json\nimport os\n",
    1,
)

speak_anchor = '''def _speak(text: str, acss: ACSS | dict[str, Any] | None) -> None:
    """Speaks the individual string using the given ACSS."""
'''
recorder = '''def _speak(text: str, acss: ACSS | dict[str, Any] | None) -> None:
    """Speaks the individual string using the given ACSS."""

    capture_path = os.environ.get("SVG_SCENE_ORCA_SPEECH_LOG")
    if capture_path:
        with open(capture_path, "a", encoding="utf-8") as capture:
            capture.write(json.dumps({"text": text}, ensure_ascii=False) + "\\n")
'''
if speak_anchor not in text:
    raise SystemExit("Orca speech function anchor was not found")
speech_source.write_text(text.replace(speak_anchor, recorder, 1))

# Ubuntu 24.04's AT-SPI 2.52 supplies the interfaces used by this browser
# journey. Orca 50 raises its package floor to 2.56 for newer optional paths;
# its runtime guards keep those paths inactive on this qualification host.
meson = meson_source.read_text()
dependency_floor = "version: '>= 2.56.0'"
if meson.count(dependency_floor) != 2:
    raise SystemExit("Orca AT-SPI dependency anchors were not found")
meson_source.write_text(meson.replace(dependency_floor, "version: '>= 2.52.0'"))
