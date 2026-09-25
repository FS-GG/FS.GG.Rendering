#!/usr/bin/env python3
"""Read-only reference projection from the current Python stager and stdlib.

Never calls stage-skills.py or creates a staging receiver.
"""

from __future__ import annotations

import argparse
import ast
import hashlib
import inspect
import json
from pathlib import Path
import shutil
import stat


REPO = Path(__file__).resolve().parents[2]
STAGER = REPO / "src/FS.GG.Rendering.Skills/stage-skills.py"
STAGER_SHA256 = "fc28e741661100031d5a6aeabd89a79af96395c73672d20685c77c837f55cb7c"


def assert_current_policy() -> None:
    raw = STAGER.read_bytes()
    actual = hashlib.sha256(raw).hexdigest()
    if actual != STAGER_SHA256:
        raise AssertionError(f"Python skill stager changed: {actual}")
    tree = ast.parse(raw)
    calls = [node for node in ast.walk(tree) if isinstance(node, ast.Call)]

    def qualified_name(node):
        if isinstance(node, ast.Name):
            return node.id
        if isinstance(node, ast.Attribute):
            parent = qualified_name(node.value)
            return f"{parent}.{node.attr}" if parent else None
        return None

    def has_call(name, predicate):
        return any(qualified_name(call.func) == name and predicate(call) for call in calls)

    if not has_call("shutil.rmtree", lambda call: len(call.args) == 1):
        raise AssertionError("stager no longer removes the receiver first")
    if not has_call("os.makedirs", lambda call: len(call.args) == 1):
        raise AssertionError("stager no longer creates a fresh output root")
    if not has_call("open", lambda call: len(call.args) >= 2
                    and isinstance(call.args[1], ast.Constant)
                    and call.args[1].value == "wb"
                    and isinstance(call.args[0], ast.Call)
                    and qualified_name(call.args[0].func) == "os.path.join"
                    and len(call.args[0].args) == 2
                    and isinstance(call.args[0].args[1], ast.Constant)
                    and call.args[0].args[1].value == "skill-manifest.json"):
        raise AssertionError("stager no longer creates the manifest with open(wb)")
    if not has_call("shutil.copytree", lambda call: len(call.args) == 2 and not call.keywords):
        raise AssertionError("stager no longer uses default copytree")
    if inspect.signature(shutil.copytree).parameters["copy_function"].default is not shutil.copy2:
        raise AssertionError("installed Python copytree default is not copy2")
    if "copystat(src, dst" not in inspect.getsource(shutil._copytree):
        raise AssertionError("installed Python copytree no longer copies directory stat")


def reference(source: Path, skill_id: str, umask: int) -> list[dict]:
    if umask < 0 or umask > 0o777:
        raise AssertionError("invalid umask")
    entries = [
        {"path": ".", "kind": "directory", "mode": 0o777 & ~umask},
        {"path": "skills", "kind": "directory", "mode": 0o777 & ~umask},
        {"path": "skill-manifest.json", "kind": "file", "mode": 0o666 & ~umask},
    ]
    for path in [source, *source.rglob("*")]:
        info = path.lstat()
        if stat.S_ISLNK(info.st_mode):
            raise AssertionError(f"linked fixture path: {path}")
        if not (stat.S_ISDIR(info.st_mode) or stat.S_ISREG(info.st_mode)):
            raise AssertionError(f"nonregular fixture path: {path}")
        rel = path.relative_to(source).as_posix()
        dest = f"skills/{skill_id}" + (f"/{rel}" if rel != "." else "")
        entries.append({"path": dest,
                        "kind": "directory" if stat.S_ISDIR(info.st_mode) else "file",
                        "mode": stat.S_IMODE(info.st_mode)})
    return sorted(entries, key=lambda row: row["path"])


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path)
    parser.add_argument("skill_id")
    parser.add_argument("umask", type=int, help="decimal process umask")
    args = parser.parse_args()
    assert_current_policy()
    print(json.dumps(reference(args.source, args.skill_id, args.umask), separators=(",", ":")))


if __name__ == "__main__":
    main()
