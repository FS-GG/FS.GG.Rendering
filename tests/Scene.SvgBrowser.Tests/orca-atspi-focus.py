#!/usr/bin/env python3
import sys
import time
import json
import os

import gi

gi.require_version("Atspi", "2.0")
from gi.repository import Atspi


def children(node):
    try:
        count = Atspi.Accessible.get_child_count(node)
    except Exception:
        return
    for index in range(count):
        try:
            child = Atspi.Accessible.get_child_at_index(node, index)
        except Exception:
            continue
        if child is not None:
            yield child


def find_objects(root, wanted):
    pending = [root]
    examined = 0
    target = None
    sink = None
    frame = None
    while pending and examined < 20000:
        node = pending.pop()
        examined += 1
        try:
            name = Atspi.Accessible.get_name(node)
            role = Atspi.Accessible.get_role(node)
            if name == wanted:
                target = node
            elif name == "Search or enter address":
                sink = node
            if role == Atspi.Role.FRAME and frame is None:
                frame = node
        except Exception:
            pass
        if target is not None and frame is not None:
            return target, sink, frame
        pending.extend(children(node))
    return target, sink, frame


Atspi.init()
wanted = sys.argv[1]
deadline = time.monotonic() + 10
while time.monotonic() < deadline:
    desktop = Atspi.get_desktop(0)
    target, sink, frame = find_objects(desktop, wanted)
    if target is not None:
        if frame is not None:
            Atspi.Component.grab_focus(frame)
            time.sleep(0.15)
        if sink is not None and wanted != "Search or enter address":
            Atspi.Component.grab_focus(sink)
            time.sleep(0.15)
        focused = Atspi.Component.grab_focus(target)
    else:
        focused = False
    if focused:
        capture_path = os.environ.get("SVG_SCENE_ATSPI_LOG")
        if capture_path:
            with open(capture_path, "a", encoding="utf-8") as capture:
                capture.write(json.dumps({"name": wanted, "focusable": True}) + "\n")
        print(f"atspi-focus: {wanted}")
        raise SystemExit(0)
    time.sleep(0.2)
raise SystemExit(f"AT-SPI object not focusable: {wanted}")
