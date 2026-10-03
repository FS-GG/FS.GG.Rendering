#!/usr/bin/env python3
"""Compare current extraction to frozen 1.7 evidence without rewriting either."""
import json
import sys
from pathlib import Path


def compare(historical: Path, current: Path) -> None:
    def files(root):
        return {p.relative_to(root) for p in root.rglob("*") if p.is_file()}

    expected = files(historical)
    if expected != files(current):
        raise ValueError("extracted file inventory changed")
    for relative in sorted(expected):
        old = (historical / relative).read_bytes()
        new = (current / relative).read_bytes()
        if relative == Path("typed-authority.json"):
            before, after = json.loads(old), json.loads(new)
            if before.get("packageIdentity") != "FS.GG.SDD.Artifacts/1.7.0":
                raise ValueError("frozen authority is not the historical 1.7 release")
            if after.get("packageIdentity") != "FS.GG.SDD.Artifacts/2.1.0":
                raise ValueError("current authority is not the selected 2.1 release")
            before["packageIdentity"] = after["packageIdentity"]
            if before != after:
                raise ValueError("authority changed beyond the selected package identity")
        elif old != new:
            raise ValueError(f"extracted bytes changed: {relative}")


if __name__ == "__main__":
    try:
        compare(Path(sys.argv[1]), Path(sys.argv[2]))
    except (ValueError, OSError) as error:
        sys.exit(f"svg-retained-qualification: {error}")
