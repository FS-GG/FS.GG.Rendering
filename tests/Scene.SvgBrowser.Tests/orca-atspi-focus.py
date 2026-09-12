#!/usr/bin/env python3
import sys
import time

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


def find_by_name(root, wanted):
    pending = [root]
    examined = 0
    while pending and examined < 20000:
        node = pending.pop()
        examined += 1
        try:
            if Atspi.Accessible.get_name(node) == wanted:
                return node
        except Exception:
            pass
        pending.extend(children(node))
    return None


Atspi.init()
wanted = sys.argv[1]
deadline = time.monotonic() + 10
while time.monotonic() < deadline:
    target = find_by_name(Atspi.get_desktop(0), wanted)
    if target is not None and Atspi.Component.grab_focus(target):
        print(f"atspi-focus: {wanted}")
        raise SystemExit(0)
    time.sleep(0.2)
raise SystemExit(f"AT-SPI object not focusable: {wanted}")
