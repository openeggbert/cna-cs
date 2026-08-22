import collections, glob, os, re

from paths import REPO_ROOT, find_cna_root

CNA_ROOT = find_cna_root()
H = str(CNA_ROOT / 'modules/c-api/include/CNA/C') + '/'
src = (REPO_ROOT / 'src/CNA.Interop/Native.cs').read_text()
bound={m.group(1) for m in re.finditer(r'partial \w+ (cna_\w+)\s*\(', src)}
by=collections.defaultdict(list)
for p in sorted(glob.glob(H+'*.h')):
    for m in re.finditer(r'CNA_C_API\s+[\w\*\s]+?\s*(cna_\w+)\s*\(', open(p).read()):
        n=m.group(1)
        if n in bound or n.endswith('_ext'): continue
        by[os.path.basename(p)].append(n)
tot=sum(len(v) for v in by.values())
print(f'unbound non-_ext: {tot}')
for f,v in sorted(by.items(), key=lambda kv:-len(kv[1])):
    print(f'  {f}: {len(v)}')
