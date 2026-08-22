import glob, os, re, shutil, subprocess, sys

from paths import REPO_ROOT, find_cna_root, find_native_libraries

CNA_ROOT = find_cna_root()
H = str(CNA_ROOT / 'modules/c-api/include/CNA/C') + '/'
CS = str(REPO_ROOT / 'src/CNA.Interop/Native.cs')

# --- headers: name -> (param list, doc)
hdr={}
versioned=set()
for p in sorted(glob.glob(H+'*.h')):
    raw=open(p).read()
    for m in re.finditer(r"typedef struct (CNA_\w+)\s*\{(.*?)\}\s*CNA_\w+\s*;", raw, re.S):
        if 'struct_size' in m.group(2): versioned.add(m.group(1))
    for m in re.finditer(r"(/\*\*.*?\*/)?\s*CNA_C_API\s+[\w\*\s]+?\s*(cna_\w+)\s*\(([^;]*?)\)\s*;", raw, re.S):
        doc,name,params=m.groups()
        ps=[x.strip() for x in ' '.join(params.split()).split(',') if x.strip() and x.strip()!='void']
        hdr[name]=(ps, doc or '', os.path.basename(p))

def split_params(text):
    """Split a C# parameter list on top-level commas only.

    A function-pointer parameter carries its own commas --
    `delegate* unmanaged[Cdecl]<nint, CnaStringView, nint*, CnaResult>` is one parameter with three
    of them -- so a naive split reports a callback-taking route as having four extra arguments.
    That is a false positive, and a false positive in a verification tool is worse than no tool:
    it trains the reader to skim past the one line that will eventually be real."""
    out, depth, current = [], 0, []
    for ch in text:
        if ch in '<([':
            depth += 1
        elif ch in '>)]':
            depth -= 1
        if ch == ',' and depth == 0:
            out.append(''.join(current).strip())
            current = []
        else:
            current.append(ch)
    tail = ''.join(current).strip()
    if tail:
        out.append(tail)
    return [x for x in out if x]

src=open(CS).read()
decls={}
for m in re.finditer(r'internal static (?:unsafe )?partial \w+ (cna_\w+)\s*\(([^;]*?)\)\s*;', src, re.S):
    name=m.group(1)
    ps=split_params(' '.join(m.group(2).split()))
    decls[name]=ps

missing=[n for n in decls if n not in hdr]
arity=[(n,len(decls[n]),len(hdr[n][0])) for n in decls if n in hdr and len(decls[n])!=len(hdr[n][0])]
print(f'declarations: {len(decls)}   headers export: {len(hdr)}')
print(f'NOT IN HEADERS ({len(missing)}): {missing}')
print(f'ARITY MISMATCH ({len(arity)}): {arity}')

# out-vs-ref on versioned structs
bad=[]
for n,ps in decls.items():
    if n not in hdr: continue
    hps,doc,f=hdr[n]
    if len(ps)!=len(hps): continue
    for i,hp in enumerate(hps):
        base=hp.replace('const','').replace('*',' ').split()
        if not base or base[0] not in versioned or '*' not in hp: continue
        if ps[i].startswith('out '):
            pn=hp.split()[-1].replace('*','')
            pm=re.search(r'@param\s+'+pn+r'\s+([^@]*?)(?=@param|@return|\*/)', doc, re.S)
            t=' '.join((pm.group(1) if pm else '(no doc)').split()).replace('*','')
            bad.append((n,f,ps[i],t[:110]))
print(f'\nversioned struct passed by `out` ({len(bad)}):')
for b in bad: print('  ',b[0],'['+b[1]+']','\n     ',b[2],'\n      doc:',b[3])


# --- declarations vs the shipped library ---------------------------------------------------------
#
# The header check above and this one answer different questions, and the difference is not
# academic: graphics.h declares cna_graphics_device_get_shading_dialect and neither built library
# exports it, so a binding written correctly against the header still dies with
# EntryPointNotFoundException at the call site.
#
# A *count* comparison cannot see that. "2,855 declared, 2,855 exported" is equally true of a header
# naming one route the library lacks while the library exports one the header lacks. Only the set
# difference finds it, which is why this runs over names rather than totals.
def check_library(path):
    suffix = os.path.basename(path).lower()
    if suffix.endswith('.so'):
        command = ['nm', '-D', '--defined-only', path]
    elif suffix.endswith('.dylib'):
        command = ['nm', '-gU', path]
    elif suffix.endswith('.dll'):
        if shutil.which('dumpbin'):
            command = ['dumpbin', '/exports', path]
        elif shutil.which('llvm-nm'):
            command = ['llvm-nm', '--defined-only', path]
        else:
            print(f'\n(skip {path}: PE inspection needs dumpbin or llvm-nm)')
            return
    else:
        print(f'\n(skip {path}: unknown library format)')
        return

    try:
        out = subprocess.run(command,
                             capture_output=True, text=True, check=True).stdout
    except Exception as exc:
        print(f'\n(could not read {path}: {exc})')
        return
    exported = {line.split()[-1].split('@@')[0] for line in out.splitlines() if ' cna_' in line}
    absent = sorted(n for n in decls if n not in exported)
    print(f'\n{path}: {len(exported)} exports, {len(absent)} declaration(s) absent')
    for name in absent:
        print('   ', name)

libraries = find_native_libraries(CNA_ROOT)
if not libraries:
    print('\n(no native library found; set CNA_NATIVE_LIBRARY or CNA_NATIVE_DIR for symbol verification)')
for candidate in libraries:
    check_library(str(candidate))
