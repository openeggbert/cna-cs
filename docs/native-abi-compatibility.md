# CNA native ABI compatibility contract

This document is the normative CNA.NET consumer policy for the CNA C ABI. The machine-readable
form is [`eng/cna-native-abi-policy.json`](../eng/cna-native-abi-policy.json); the loader implements
policy `cna-cs-native-abi/1` in `CNA.Interop`. Upstream CNA remains the authority for its C ABI and
is not modified by this policy.

## Version meaning

CNA encodes `major.minor.patch` into a `uint32_t` with 16 bits for major and 8 bits each for minor
and patch. The fields do not imply ordinary stable SemVer while the major is zero:

| Field | CNA C ABI meaning | CNA.NET admission consequence |
| --- | --- | --- |
| Major | At ABI 1.x and later, an incompatible change requires a new major. Major 0 identifies the experimental line and provides no same-major compatibility guarantee. | A different major is rejected. Sharing major 0 is necessary but not sufficient. |
| Minor before 1.0 | A reviewed ABI generation. It may be additive or incompatible; CNA 0.3, 0.6, and 0.8 contain documented contract changes. | Each exact minor must have a reviewed matrix entry. There is no `nativeMinor >= 6` rule. |
| Minor at/after 1.0 | CNA's published contract permits additive, backward-compatible changes within the major. | Future consumers may use a major/minimum-minor rule only after the 1.0 contract and evidence are reviewed into a new CNA.NET policy. |
| Patch | ABI-neutral fixes only: no change to exports, prototypes, layouts, values, callbacks, ownership, errors, or documented behavior. | Exact patch releases still require a matrix entry because today's runtime metadata has a version but no ABI-shape fingerprint. |

The upstream installed CMake package's `SameMajorVersion` selection is not used as runtime proof.
It cannot express CNA's documented experimental-0.x exception by itself.

## Reviewed compatibility matrix

| CNA.NET consumer | Native library | CNA.NET result | Evidence |
| --- | --- | --- | --- |
| 0.20.0 | 0.20.0 | Accept | Consumer baseline at CNA `next` `e178282fc`. |
| 0.20.0 | Any other 0.x | Reject | No audited matrix entry. |
| 0.20.0 | Any 1.x+ | Reject | Different ABI major. |

One accepted entry is not a simplification of the policy; it is what the policy produces when the
consumer moves. There is no `>= 0.20` rule and there never was a `>= 0.6` one.

### Retired entries

The matrix previously accepted 0.6.0, 0.7.0, 0.8.0 and 0.19.0. Nothing was found wrong with any of
those reviews; two different things moved.

**0.6.0, 0.7.0 and 0.8.0** were retired because this binding began importing eight routes CNA
introduced after 0.8.0:

- `cna_render_target_subscribe_content_lost`, `cna_render_target_unsubscribe_content_lost`
- `cna_vertex_buffer_set_data_raw_with_options`, `cna_vertex_buffer_set_data_raw_at_with_options`
- `cna_graphics_device_create`, `cna_graphics_device_destroy`
- `cna_graphics_ext_is_available`, `cna_engine_layer_get_version`

No 0.6/0.7/0.8 library exports those names, so the loader's required-symbol check would refuse one
anyway. Leaving the entries in place would have made the matrix promise something the loader could
not deliver, and moved the failure from load time to first use.

**0.19.0** was retired for the opposite reason: 0.20.0 supersedes it and nothing this consumer
touches differs between the two. Keeping it would have been the first step towards a range, which
is the shape this policy exists to avoid.

Both retirements are enforced rather than merely documented: the `retired-0.8.0` and
`retired-0.19.0` fixtures prove that a generation this consumer used to accept is actually refused.

The substantive part of the earlier 0.8.0 review still stands and carries forward: CNA 0.8 changed
`CNA_GRAPHICS_CAPABILITY_MAXIMUM` from 13 to 18 and `CNA_GRAPHICS_RENDERER_MAXIMUM` from 49 to 50.
0.20.0 keeps the capability value, moves the renderer sentinel again to 49, and CNA.NET binds
neither. `tools/coverage/baselinediff.py` rediscovers exactly those constant changes when run from
0.6.0 or 0.7.0 forward and nothing else, which is a useful cross-check on both the tool and that
review.

### What the 0.20.0 admission measured

Two diffs, because the consumer moved twice. `tools/coverage/baselinediff.py` compares an
already-accepted generation against the proposed one across both evidence paths.

0.8.0 → 0.19.0, when the binding left the 0.6-era matrix:

| Measured | Result |
| --- | --- |
| Consumed entry points absent | 0 |
| Consumed entry points with a changed header prototype | 0 |
| Exports removed | 0 (1,189 added) |
| Struct size/alignment/field-offset changes | 0 (45 structs added) |
| Scalar width changes | 0 (83 added) |
| Existing integer constant values changed | 0 (326 added) |
| String constants changed | 0 |

0.19.0 → 0.20.0, the renderer removal:

| Measured | Result |
| --- | --- |
| Consumed entry points absent | 0 of 849 |
| Consumed entry points with a changed header prototype | 0 |
| Exports removed / added | 0 / 0 |
| Struct, scalar and string changes | 0 |
| Integer constants | 11 removed, 1 changed |

All twelve constant differences are renderer identities:
`CNA_GRAPHICS_RENDERER_{BLEND2D,DILIGENT,IGL,LLGL,MAGNUM,NANOVG,OPENVG,SKIA,SOKOL,TINYGL,WICKED}`
and `CNA_GRAPHICS_RENDERER_MAXIMUM` moving from 50 to 49. This binding reads the renderer's *name*
through `cna_graphics_device_copy_renderer_name` and consumes no `CNA_GRAPHICS_RENDERER_*` identity
or sentinel, which is why removing eleven renderers is a clean diff here rather than a breaking
change. That is a design property worth keeping: binding the identity enum would turn every future
renderer change into a compatibility event.

`tools/abi-verify` independently passes 86/86 C-authority layout measurements and compiles the
reviewed prototypes against the 0.20.0 headers, and the full native gate set -- integration tests,
ownership stress, corpus capture, fixture matrix, package acceptance -- runs against the 0.20.0
library.

The twelve renderer-identity differences are recorded in
[`eng/cna-upstream-abi-allowlist.txt`](../eng/cna-upstream-abi-allowlist.txt) so the diff gate can
run in CI and still fail on anything else. An allowlist entry that matches nothing fails as stale,
so the exception cannot outlive the difference it was written for.

Run `tools/abi-verify` against a pinned revision rather than a live `next` worktree. That branch
moves, and a header tree ahead of the matrix makes the gate fail by design -- which is the gate
working, not a configuration problem. It is how the 0.20.0 bump was noticed.

## Compatible evolution operations

An operation is compatible only when it preserves every contract an existing consumer can use:

- add an export under a new name without removing or changing an existing export;
- add an unrelated fixed-width constant without renumbering or changing an existing constant;
- append an optional field to a caller-sized/versioned descriptor while retaining the old
  mandatory prefix, accepting the old `struct_size`, and never reading or writing beyond the size
  the caller supplied;
- add a new callback table or append an optional callback to a genuinely caller-sized table while
  honoring the old table size;
- add a fixed-width enum-like identity when existing values and sentinels remain fixed and an old
  consumer cannot be forced to interpret the new value;
- clarify documentation without changing ownership, error, threading, lifetime, or behavior.

Additional exports are deliberately allowed by the loader. They cannot collide with or substitute
for the 849 names imported by this build.

## Breaking operations

The following require a new stable major, or a new reviewed experimental minor plus an explicit
consumer-matrix decision:

- remove, rename, hide, or stop exporting an existing entry point;
- change an export's return type, parameter count/order/type/width/pointer depth, calling
  convention, ownership, errors, threading, lifetime, or behavior;
- change any existing scalar width, constant value, flag bit, handle representation, or string /
  buffer convention;
- reorder, resize, retype, remove, or change the meaning/alignment/packing of an existing struct
  field, or append to a fixed/non-size-aware output struct;
- require a newly appended descriptor field from a caller that supplied the older valid prefix;
- renumber an enum-like identity, change a sentinel such as `MAXIMUM`, or return a new identity to
  a route whose old consumer cannot tolerate unknown values;
- change an existing callback signature, calling convention, invocation order/thread, error
  propagation, context meaning, or lifetime.

Deprecation is compatible only while the old export and its contract remain available. A changed
signature uses a new export name. A changed fixed struct uses a new type and normally a new route.

### Struct-size and version rules

`struct_size`/`struct_version` are useful only when the callee actually reads the caller's header
before accessing the body. Input and in/out descriptors may grow by appending optional fields when
the old prefix remains valid. An output initializer that receives only `T*` and overwrites the
header has no independent capacity argument; CNA.NET therefore treats that output shape as fixed
despite the header. It may grow only through a size-aware/new export or a new type. The loader's
guarded `CNA_TouchCapabilities` initializer canary exists specifically to enforce one foundational
instance of this distinction.

### Enum-like values

CNA exposes fixed-width `uint32_t` typedefs and named constants, not compiler-sized C enums.
Appending a value is compatible only if all old values and sentinels remain stable and old callers
either cannot receive the new value or explicitly tolerate unknown values. Appending a flag bit is
subject to the same rule for masks. Moving `MAXIMUM` is a constant-value change and is breaking for
callers that use it, which is why 0.8 is not labeled generally additive.

### Callbacks

Existing callback prototypes are immutable. New callback capability uses a new subscription or an
optional appended slot in a size-aware table. A callback's thread, ordering, reentrancy, context,
error, and lifetime rules are ABI contract just as much as its machine-level prototype.

## Loader proof and its boundary

Before returning a library handle, the managed resolver now requires:

1. readable `cna_get_abi_version` metadata and an exact reviewed matrix entry;
2. every one of the 849 `LibraryImport` entry points declared by `CNA.Interop.Native`;
3. a successful `cna_error_get_last_message_size` result/out-parameter signature canary;
4. a successful guarded `cna_touch_capabilities_init` canary proving the 16-byte version-1 shape,
   canonical body, and write bounds.

Version numbers and symbol names cannot describe every native prototype or POD layout. The runtime
checks therefore complement, rather than replace, `tools/abi-verify`: the platform C compiler
checks 86 selected native/managed size, alignment, offset, width, callback, and prototype facts
against the headers, and now rejects headers outside this same version matrix. CNA's own complete
ABI baseline supplies the reviewed release-to-release shape/export/value diff, which
`tools/coverage/baselinediff.py` computes and which must report zero breaking differences. A new ABI
version must pass both evidence paths before it is added to the matrix.

86 is a floor rather than a coverage claim. This binding declares 80 interop structs and twenty
enum-like identities; the C-authority probe measures thirteen of those structs. A layout change to
an unmeasured struct would be caught by the upstream baseline diff and not by the probe, which is
why the admission needs both and why widening the probe is tracked in `plan.md`.

## Automated fixtures

`scripts/Verify-NativeAbiCompatibility.sh` builds dependency-free shared libraries and runs each in
a fresh managed process. The exact matrix is:

| Fixture | Expected | Property proved |
| --- | --- | --- |
| `exact-0.20.0` | Accept | Exact expected ABI. |
| `exact-0.20.0-extra-symbol` | Accept | Unrelated added exports do not break a consumer. |
| `retired-0.8.0` | Reject | A generation retired because this consumer outgrew it. |
| `retired-0.19.0` | Reject | A generation retired because a newer one superseded it -- being previously audited is not admission. |
| `unreviewed-0.21.0` | Reject | Neither is being newer: the matrix is a point list, not a floor. |
| `missing-required-symbol` | Reject | Any missing managed import fails at load, not at first use. |
| `changed-required-signature` | Reject | A testable core signature/out-parameter change fails its canary. |
| `incompatible-major-1.0.0` | Reject | Major mismatch. |
| `structurally-incompatible-0.20.0` | Reject | An accepted version cannot override guarded shape evidence. |
| `malformed-metadata-0.0.0` | Reject | An incomplete/unrecognized encoded generation. |
| `unreadable-metadata` | Reject | Missing version export. |

The fixture inventory is generated from and checked against the managed import declarations. CI
runs this gate without a protected CNA binary; package acceptance reruns it and additionally sends
the selected real library through the same probe.
