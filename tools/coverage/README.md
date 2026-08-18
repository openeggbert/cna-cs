# Coverage sweeps

Five scripts that answer "does this binding actually cover XNA 4.0, and is every
P/Invoke real?" — mechanically, against the `openeggbert/cna` headers, which are
the authority. They exist because prose could not be trusted: a header audit
found **ten** doc comments in this repository asserting the C API could not do
something it could do perfectly well, and three P/Invoke declarations naming
functions that exist in no header at all.

Run them from anywhere; they locate both trees themselves.

```
python3 tools/coverage/sweep.py       # P/Invoke declarations vs headers
python3 tools/coverage/unbound.py     # header functions with no binding
python3 tools/coverage/typesweep.py   # C++ types with no C# counterpart
python3 tools/coverage/md2run.py      # member-level diff, both layers
```

## Locating the headers

The `openeggbert/cna` checkout's **directory name is not stable** — the headers
have lived in `cnabinding` (gone), `cnanext`, `cnagltf` and `cnabindingc`. Every
script finds it by globbing for `modules/c-api/include/CNA/C/media_library.h`
and prints which one it chose when there is more than one. Override with:

```
CNA_ROOT=/path/to/a/cna/checkout python3 tools/coverage/sweep.py
```

**Pick a checkout whose working tree is clean.** A checkout with uncommitted
header edits will happily validate a binding written against a signature that is
still being designed, which is exactly how a fabricated P/Invoke gets in.

## What each one can and cannot see

This matters more than the scripts do. Each is blind to something the next one
catches, which is why there are four rather than one:

| Script | Finds | Cannot see |
| --- | --- | --- |
| `sweep.py` | fabricated declarations, arity drift, versioned structs passed `out` instead of `ref` | anything about XNA coverage |
| `unbound.py` | native routes nothing binds | members with no native counterpart; members onto an already-bound route |
| `typesweep.py` | whole XNA types with no C# file | anything below type level |
| `md2run.py` | missing members on types that exist on both sides | **missing types** — it skips a C++ class with no C# counterpart |

`md2.py` is the shared parser (`cpp_public`, `normalise`, `cs_file_members`), not
a script to run on its own.

## Triaging the output

Neither `unbound.py` nor `md2run.py` returns zero, and neither should — most of
the C++ surface is engine-internal. `plan.md`'s "Coverage: how it is measured"
section lists the noise categories with examples.

One warning from experience: **"not XNA 4.0" is the category to distrust.** Every
other category is self-evident from the name. That one is a judgement call, and
when it is wrong it is silently wrong — `GraphicsAdapter.MonitorHandle` sat in it
for a full pass and is a real XNA 4.0 property. Check the actual XNA surface
before filing anything there.

The `CNAEXT` marker in the C++ headers is the filter that makes the member diff
tractable at all: it cut one run's candidate list from 107 to 45.
