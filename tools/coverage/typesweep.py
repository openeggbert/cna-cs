import re, glob, os
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

CPP = CNA_ROOT + '/modules'
CS = REPO + '/src'

# every C# type name declared anywhere in the binding
cs=set()
for p in glob.glob(f'{CS}/**/*.cs', recursive=True):
    if '/obj/' in p or '/bin/' in p: continue
    for m in re.finditer(r'\b(?:class|struct|interface|enum|record)\s+(\w+)', open(p, errors='ignore').read()):
        cs.add(m.group(1))

missing=[]
for h in glob.glob(f'{CPP}/**/include/**/*.hpp', recursive=True):
    src=open(h, errors='ignore').read()
    if 'namespace Microsoft::Xna::Framework' not in src and 'namespace Microsoft::Devices' not in src: continue
    src2=re.sub(r'/\*.*?\*/','',src,flags=re.S)
    for m in re.finditer(r'\b(?:class|struct|enum class|enum)\s+(?:CNAEXT\s+)?(\w+)\s*(?:final|:|\{)', src2):
        n=m.group(1)
        if n in cs: continue
        # CNAEXT-marked types are engine extensions, not XNA
        if re.search(r'\b(?:class|struct|enum class|enum)\s+CNAEXT\s+'+n+r'\b', src2): continue
        ns=re.search(r'namespace\s+([\w:]+)', src)
        missing.append((n, ns.group(1) if ns else '?', os.path.basename(h)))

seen=set(); out=[]
for n,ns,f in missing:
    if n in seen: continue
    seen.add(n); out.append((n,ns,f))
print(f'C++ types with no C# counterpart: {len(out)}')
for n,ns,f in sorted(out, key=lambda x:(x[1],x[0])):
    print(f'  {ns}::{n}   [{f}]')
