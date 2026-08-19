"""Runtime coverage of the compat surface, split by what "executing a type" can even mean.

A flat "N of 223 types have run" is the wrong number and was steering the work: most of the compat
surface has no native side to exercise. Math and packed-vector types are managed by design
invariant 3, enums are verified by parity tests instead, and an interface runs only through its
implementors. Counting those as uncovered makes the total look bad and, worse, makes it look
improvable by writing tests that would prove nothing.

The number that means something is the native-backed one: types whose source names a native call or
holds a handle, and which therefore have an ABI contract a managed test cannot check.
"""
import glob
import os
import re
import sys

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
COMPAT = REPO + '/src/CNA.XnaCompat'
FRAMEWORK = REPO + '/src/CNA.Framework'
EXERCISED = [REPO + '/tests/CNA.Integration.Tests', REPO + '/../cna-cs-template']


# KNOWN LIMITATION, stated because it hid a real gap for the whole of this work.
#
# This matches bare identifiers, and both layers name their types identically -- Texture2D exists in
# CNA.Graphics and in Microsoft.Xna.Framework.Graphics. So a compat type counted as covered when
# only its CNA counterpart had ever executed, and for a long time that was every one of them: the
# integration tests were written entirely against CNA.* while a ported game uses the compat layer.
#
# Running the compat layer for the first time found four gaps in it immediately, none of which this
# script could see. Fixing the measurement properly means resolving types rather than matching
# names, which is a real parser; until then, treat a covered compat type as "covered on at least one
# layer" and check tests/CNA.Integration.Tests/CompatLayerIntegrationTests.cs for what the compat
# side actually exercises.
def identifiers(paths):
    seen = set()
    for root in paths:
        for path in glob.glob(root + '/**/*.cs', recursive=True):
            if '/obj/' in path or '/bin/' in path:
                continue
            seen.update(re.findall(r'\b[A-Z][A-Za-z0-9]+\b', open(path, errors='ignore').read()))
    return seen


def classify():
    groups = {'native': [], 'managed': [], 'enum': [], 'interface': []}
    for path in glob.glob(COMPAT + '/**/*.cs', recursive=True):
        if '/obj/' in path or '/bin/' in path:
            continue
        src = open(path, errors='ignore').read()
        for kind, name in re.findall(
                r'^public (?:abstract |sealed |static |partial |readonly )*(class|struct|interface|enum|record) (\w+)',
                src, re.M):
            # The compat type usually only re-types; the ABI contract lives in its CNA.Framework
            # counterpart, so both bodies decide the classification.
            twin = glob.glob(f'{FRAMEWORK}/**/{name}.cs', recursive=True)
            body = src + ''.join(open(f, errors='ignore').read() for f in twin)
            if kind == 'enum':
                groups['enum'].append(name)
            elif kind == 'interface':
                groups['interface'].append(name)
            elif 'Native.cna_' in body or 'NativeResourceHandle' in body or 'CnaHandle' in body:
                groups['native'].append(name)
            else:
                groups['managed'].append(name)
    return groups


def main():
    touched = identifiers(EXERCISED)
    groups = classify()
    labels = {
        'native': 'native-backed (crosses the ABI)',
        'managed': 'pure managed (invariant 3)',
        'enum': 'enums (parity-checked instead)',
        'interface': 'interfaces (run via implementors)',
    }

    for key, names in groups.items():
        names = sorted(set(names))
        hit = [n for n in names if n in touched]
        pct = 100 * len(hit) // max(1, len(names))
        print(f'{labels[key]:<36} {len(hit):>3} / {len(names):<3} ({pct}%)')

    missing = sorted(n for n in set(groups['native']) if n not in touched)
    print(f'\nnative-backed and never executed ({len(missing)}):')
    print('  ' + ' '.join(missing) if missing else '  none')
    return 0


if __name__ == '__main__':
    sys.exit(main())
