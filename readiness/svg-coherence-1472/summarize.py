"""Join the raw baseline/candidate observations. Usage: python3 summarize.py <dir containing raw/>"""
import glob, json, os, statistics, sys

root = sys.argv[1]
med = statistics.median


def load(kind):
    out = {"baseline": [], "candidate": []}
    for f in sorted(glob.glob(os.path.join(root, "raw", f"{kind}-run*-*.json"))):
        d = json.load(open(f))
        out[d["label"]].append(d)
    return out


create = load("create")
query = load("query")
print("runs: create", {k: len(v) for k, v in create.items()}, "query", {k: len(v) for k, v in query.items()})
d0 = create["baseline"][0]
print(f"runtime {d0['framework']}, processorCount {d0['processorCount']}, rounds {d0['rounds']}")


def sem(d):
    return {(s["layout"], s["n"]): (s["selected"], s["idsHash"], s["matchesLinearReference"], s["visited"], s["candidates"], s["total"]) for s in d["semantic"]}


ref = sem(d0)
allruns = create["baseline"] + create["candidate"]
print("create-harness semantic/counter join identical across all runs:", all(sem(d) == ref for d in allruns),
      "| every cell matches the independent linear reference:", all(v[2] for v in ref.values()))

print()
print("SpatialWorkingSet.create, median over all rounds of all runs (baseline -> candidate); alloc is bytes per create")
print(f"{'layout':24}{'N':>8} | {'base ms':>10} {'cand ms':>10} {'ratio':>7} | {'base MB':>9} {'cand MB':>9} {'ratio':>7} | per-run medians base / cand (ms)")
cells = [(c["layout"], c["n"]) for c in d0["create"]]
for layout, n in cells:
    def pick(arm, field):
        return [next(c for c in d["create"] if (c["layout"], c["n"]) == (layout, n))[field] for d in create[arm]]
    bt, ct = pick("baseline", "createMs"), pick("candidate", "createMs")
    ba, ca = pick("baseline", "allocatedBytes"), pick("candidate", "allocatedBytes")
    bm, cm = med([x for r in bt for x in r]), med([x for r in ct for x in r])
    bam, cam = med([x for r in ba for x in r]), med([x for r in ca for x in r])
    print(f"{layout:24}{n:>8} | {bm:10.3f} {cm:10.3f} {cm / bm:7.3f} | {bam / 1e6:9.2f} {cam / 1e6:9.2f} {cam / bam:7.3f} | "
          f"{[round(med(r), 3) for r in bt]} / {[round(med(r), 3) for r in ct]}")

print()
print("query (#1469 harness, unchanged cells), median microseconds per call; a ratio near 1 means no query change")
qd0 = query["baseline"][0]


def qsem(d):
    return {(s["cell"], s["n"]): (s["selected"], s["idsHash"], s["matchesLinearReference"], s["visited"], s["candidates"], s["total"]) for s in d["semantic"]}


qref = qsem(qd0)
print("query-harness semantic/counter join identical across all runs:", all(qsem(d) == qref for d in query["baseline"] + query["candidate"]),
      "| linear reference match all:", all(v[2] for v in qref.values()))
worst = 0.0
rows = []
for q in qd0["queryNsPerCall"]:
    c = (q["cell"], q["n"])
    def runs(arm):
        return [next(x for x in d["queryNsPerCall"] if (x["cell"], x["n"]) == c)["samplesNs"] for d in query[arm]]
    bm, cm = med([x for r in runs("baseline") for x in r]), med([x for r in runs("candidate") for x in r])
    rows.append((c, bm, cm))
    worst = max(worst, cm / bm)
for c, bm, cm in rows:
    if c[1] in (1000, 100000, 300000):
        print(f"  {c[0]:34}{c[1]:>8}  {bm / 1000:9.2f} -> {cm / 1000:9.2f}   ratio {cm / bm:6.3f}")
print(f"largest candidate/baseline query median ratio over all {len(rows)} cells: {worst:.3f}")

print()
print("create-harness creation block of the #1469 harness (spread layout, median ms / bytes), as a cross-check")
for i, n in enumerate([1000, 10000, 100000, 300000]):
    for arm in ("baseline", "candidate"):
        ms = [x for d in query[arm] for x in d["creation"][i]["createMs"]]
        al = [x for d in query[arm] for x in d["creation"][i]["allocatedBytes"]]
        print(f"  N={n:7} {arm:9}: {med(ms):9.3f} ms  {med(al):>12.0f} B")
