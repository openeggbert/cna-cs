import re, glob, os, collections
import os, glob

def _find_cna_root():
    """The openeggbert/cna checkout's *directory name* is not stable (cnabinding is gone;
    the headers have lived in cnanext, cnagltf and cnabindingc). Locate it, never hard-code it."""
    env = os.environ.get('CNA_ROOT')
    if env and os.path.isdir(os.path.join(env, 'modules/c-api/include/CNA/C')):
        return env
    base = '/rv/data/development/github.com/openeggbert'
    found = sorted(glob.glob(base + '/*/modules/c-api/include/CNA/C/media_library.h'))
    if not found:
        raise SystemExit('No openeggbert/cna checkout with modules/c-api found. Set CNA_ROOT.')
    root = found[0][:-len('/modules/c-api/include/CNA/C/media_library.h')]
    if len(found) > 1:
        print(f'# headers: {root}  (of {len(found)} checkouts; set CNA_ROOT to pick another)')
    return root

CNA_ROOT = _find_cna_root()
REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

H = CNA_ROOT + '/modules/c-api/include/CNA/C/'
src=open(REPO + '/src/CNA.Interop/Native.cs').read()
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
