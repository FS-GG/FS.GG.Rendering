import json,statistics,sys,glob
runs={}
for f in sorted(glob.glob('raw/*.json')):
    d=json.load(open(f)); runs[f.split('/')[-1]]=d
def med(x): return statistics.median(x)
names=sorted(runs)
# semantic join
base=[runs[n] for n in names if n.endswith('base.json')]
cand=[runs[n] for n in names if n.endswith('cand.json')]
def sem(d): return {(s['cell'],s['n']):(s['selected'],s['idsHash'],s['matchesLinearReference'],s['visited'],s['candidates'],s['total']) for s in d['semantic']}
ref=sem(base[0])
allsem=all(sem(d)==ref for d in base+cand)
print("semantic/counter join identical across all runs:",allsem, "linear reference match all:", all(v[2] for v in ref.values()))
cells=[(q['cell'],q['n']) for q in base[0]['queryNsPerCall']]
def stat(d):
    return {(q['cell'],q['n']):(med(q['samplesNs']),min(q['samplesNs']),max(q['samplesNs'])) for q in d['queryNsPerCall']}
print(f"{'cell':34}{'N':>8} | {'base med(us)':>12} {'min':>8} | {'cand med(us)':>12} {'min':>8} | ratio(med)  run-to-run spread base/cand")
for c in cells:
    bs=[stat(d)[c] for d in base]; cs=[stat(d)[c] for d in cand]
    bm=med([x[0] for x in bs]); cm=med([x[0] for x in cs])
    print(f"{c[0]:34}{c[1]:>8} | {bm/1000:12.2f} {min(x[1] for x in bs)/1000:8.2f} | {cm/1000:12.2f} {min(x[1] for x in cs)/1000:8.2f} | {cm/bm:8.3f}   {[round(x[0]/1000,2) for x in bs]} {[round(x[0]/1000,2) for x in cs]}")
print("creation (median ms, median alloc bytes):")
for i,n in enumerate([1000,10000,100000,300000]):
    for lab,ds in (('base',base),('cand',cand)):
        ms=[x for d in ds for x in d['creation'][i]['createMs']]; al=[x for d in ds for x in d['creation'][i]['allocatedBytes']]
        print(f"  N={n:7} {lab}: {med(ms):9.3f} ms  {med(al):>12.0f} B")
