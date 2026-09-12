#!/usr/bin/env python3
"""Add a narrow utterance recorder to the pinned Orca source used by CI."""

from pathlib import Path
import sys


source = Path(sys.argv[1])
text = source.read_text()

import_anchor = "import importlib\n"
if import_anchor not in text:
    raise SystemExit("Orca speech module import anchor was not found")
text = text.replace(import_anchor, "import importlib\nimport json\nimport os\n", 1)

speak_anchor = '''def _speak(text, acss, interrupt):
    """Speaks the individual string using the given ACSS."""
'''
recorder = '''def _speak(text, acss, interrupt):
    """Speaks the individual string using the given ACSS."""

    capture_path = os.environ.get("SVG_SCENE_ORCA_SPEECH_LOG")
    if capture_path:
        with open(capture_path, "a", encoding="utf-8") as capture:
            capture.write(json.dumps({"text": text}, ensure_ascii=False) + "\\n")
'''
if speak_anchor not in text:
    raise SystemExit("Orca speech function anchor was not found")
text = text.replace(speak_anchor, recorder, 1)
source.write_text(text)
