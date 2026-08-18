import re, os, glob, collections
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

CPP = CNA_ROOT + '/modules'

NOISE = re.compile(
 r'^(begin|end|cbegin|cend|rbegin|rend|iterator|const_iterator|container_type|size_type|value_type|'
 r'operator|get|data|size|empty|at|clear|push_back|swap|first|second|it_|base)$'
 r'|ForTests$|^INTERNAL_|GetTypeName|Renderer|FillGpuDrawParams|GetCompiled|Weak$|^SetOwned|'
 r'ReconstructFromCache|AddResourceReference|RemoveResourceReference|GetTrackedResourceCount|'
 r'^Debug$|LoaderFn$|^Get(Fragment|Vertex)Source|^GetCpu|^Has[A-Z]|Internal$|Mutable$|'
 r'TestAccess|^intcs$|^false$|^const$|^DEF_PROP$|^void$|^T$|^String$|^IntPtr$|Factory$|'
 r'^std|^typeid$|^move$|^find$|^to_string$|^str$|_$')

def cpp_public(path):
    src=open(path, errors='ignore').read()
    src=re.sub(r'/\*.*?\*/','',src,flags=re.S)
    src=re.sub(r'//[^\n]*','',src)
    m=re.search(r'\b(?:class|struct)\s+(?:CNAEXT\s+)?(\w+)[^;{]*\{(.*)\n\s*\};', src, re.S)
    if not m: return None, set()
    name, body = m.group(1), m.group(2)
    parts=re.split(r'\b(public|private|protected)\s*:', body)
    keep = [parts[0]] if len(parts)==1 else [parts[i+1] for i in range(1,len(parts),2) if parts[i]=='public']
    body='\n'.join(keep)
    # drop inline function bodies so locals don't leak in
    out=[]; depth=0
    for ch in body:
        if ch=='{': depth+=1
        elif ch=='}': depth=max(0,depth-1)
        elif depth==0: out.append(ch)
    body=''.join(out)
    names=set()
    for mm in re.finditer(r'(\w+)\s*\(', body):
        names.add(mm.group(1))
    for mm in re.finditer(r'^\s*(?:static\s+|const\s+|mutable\s+)*[\w:<>,\s\*&]+?\s+(\w+)\s*(?:=[^;]*)?;', body, re.M):
        names.add(mm.group(1))
    return name, names

def normalise(members):
    res=set()
    for x in members:
        if 'EXT' in x: continue
        m=re.match(r'^(?:get|set)(\w+?)Property$', x)
        res.add(m.group(1) if m else x)
    return {x for x in res if not NOISE.search(x)}

DECL = re.compile(
  r'^\s*(?:public|protected internal|protected|internal)\s+'
  r'(?:static\s+|virtual\s+|override\s+|new\s+|readonly\s+|abstract\s+|sealed\s+|unsafe\s+|'
  r'partial\s+|event\s+|const\s+|required\s+|extern\s+|async\s+)*'
  r'(?:[\w<>\[\],\.\?\s]+?\s+)?(\w+)\s*(?:<[^>(]*>)?\s*[\(\{=;]', re.M)

def cs_file_members(path):
    src=open(path, errors='ignore').read()
    src=re.sub(r'///[^\n]*','',src); src=re.sub(r'/\*.*?\*/','',src,flags=re.S)
    out={mm.group(1) for mm in DECL.finditer(src)}
    # interface members have no modifier
    if re.search(r'\b(?:public\s+)?interface\s+\w+', src):
        for mm in re.finditer(r'^\s*(?!//)[\w<>\[\],\.\?]+\s+(\w+)\s*[\({;]', src, re.M):
            out.add(mm.group(1))
    if re.search(r'\bthis\s*\[', src): out.add('Item')
    bases=set()
    for mm in re.finditer(r'^\s*(?:public|internal)\s+(?:abstract\s+|sealed\s+|static\s+|partial\s+)*'
                          r'(?:class|struct|interface|record)\s+\w+(?:<[^>]*>)?\s*:\s*([^\{]+)', src, re.M):
        for b in mm.group(1).split(','):
            b=b.strip().split('<')[0].split('.')[-1]
            if b and b[0].isupper(): bases.add(b)
    return out, bases
