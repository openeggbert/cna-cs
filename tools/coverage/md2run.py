import sys, os, glob, collections
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from md2 import cpp_public, normalise, cs_file_members, CPP

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
CS = REPO + '/src'

# index C# files by class name, per layer
def index(layer):
    idx = {}
    for p in glob.glob(f'{CS}/{layer}/**/*.cs', recursive=True):
        idx.setdefault(os.path.basename(p)[:-3], []).append(p)
    return idx

def members_with_bases(name, idx, seen=None):
    seen = seen or set()
    if name in seen: return set()
    seen.add(name)
    out = set()
    for p in idx.get(name, []):
        m, bases = cs_file_members(p)
        out |= m
        for b in bases:
            out |= members_with_bases(b, idx, seen)
    return out

hdrs = [h for h in glob.glob(f'{CPP}/**/include/**/*.hpp', recursive=True)]
report = collections.defaultdict(list)
for layer in ('CNA.Framework', 'CNA.XnaCompat'):
    idx = index(layer)
    # compat: also allow falling through to Framework (compat types subclass/compose them)
    fw = index('CNA.Framework') if layer == 'CNA.XnaCompat' else None
    for h in hdrs:
        name, mem = cpp_public(h)
        if not name: continue
        mem = normalise(mem)
        if not mem: continue
        if name not in idx:
            if layer == 'CNA.XnaCompat' and name not in (fw or {}): continue
            if layer == 'CNA.Framework': continue
        have = members_with_bases(name, idx)
        if layer == 'CNA.XnaCompat':
            have |= members_with_bases(name, fw)
        if not have: continue
        missing = sorted(m for m in mem if m not in have and m != name)
        if missing:
            report[layer].append((name, os.path.basename(h), missing))

for layer, rows in report.items():
    total = sum(len(r[2]) for r in rows)
    print(f'==== {layer}: {len(rows)} types, {total} candidate members ====')
    for name, h, missing in sorted(rows):
        print(f'  {name} [{h}]: {", ".join(missing)}')
