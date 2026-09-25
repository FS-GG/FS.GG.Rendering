#!/usr/bin/env python3
"""Read-only contrast with the live stager's source-directory containment check."""

import importlib.util
from pathlib import Path
import sys
import tempfile

sys.dont_write_bytecode = True
script = Path(__file__).resolve().parents[2] / "src/FS.GG.Rendering.Skills/stage-skills.py"
spec = importlib.util.spec_from_file_location("stage_skills", script)
stager = importlib.util.module_from_spec(spec)
spec.loader.exec_module(stager)

with tempfile.TemporaryDirectory() as temporary:
    root = Path(temporary)
    repository = root / "repo"
    outside = root / "outside"
    repository.mkdir()
    outside.mkdir()
    (repository / "link").symlink_to(outside, target_is_directory=True)
    try:
        stager.safe_relative(str(repository.resolve()), "link/", "synthetic supplied-by")
    except SystemExit as error:
        assert error.code == 2
    else:
        raise AssertionError("live Python containment check accepted an outside-pointing source link")

print("live Python outside-link containment control: passed")
