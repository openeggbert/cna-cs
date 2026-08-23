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

| CNA.NET consumer | Native library | General CNA compatibility | CNA.NET result | Evidence |
| --- | --- | --- | --- | --- |
| 0.6.0 | 0.6.0 | Exact | Accept | Consumer baseline at CNA `25a7932cb`. |
| 0.6.0 | 0.7.0 | Additive | Accept | `CBIND-078` added six PBR/morph-target routes. The upstream baseline retains every 0.6 export, value, prototype, layout, and contract. CNA `a09196a64`. |
| 0.6.0 | 0.8.0 | Not compatible for every 0.7 consumer | Accept only for this reviewed consumer shape | CNA 0.8 changes `CNA_GRAPHICS_CAPABILITY_MAXIMUM` from 13 to 18 and `CNA_GRAPHICS_RENDERER_MAXIMUM` from 49 to 50. CNA.NET does not bind or consume either sentinel. The upstream 0.7→0.8 baseline otherwise adds names and preserves the existing constants, exports, prototypes, and struct layouts used here. CNA merge `1d6da4af8`. |
| 0.6.0 | Any other 0.x | Unknown | Reject | No audited matrix entry. |
| 0.6.0 | Any 1.x+ | Different major | Reject | Different ABI major. |

Thus a 0.6 consumer is guaranteed compatible with the specific 0.7 release because that release is
documented and baselined as additive. A general 0.6-to-any-later-0.x guarantee does not exist.
Version 0.8 remains usable by CNA.NET for the narrower reason above; this must not be generalized
to another 0.6 consumer that enumerates through either changed `MAXIMUM` value.

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
for the 841 names imported by this build.

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
2. every one of the 841 `LibraryImport` entry points declared by `CNA.Interop.Native`;
3. a successful `cna_error_get_last_message_size` result/out-parameter signature canary;
4. a successful guarded `cna_touch_capabilities_init` canary proving the 16-byte version-1 shape,
   canonical body, and write bounds.

Version numbers and symbol names cannot describe every native prototype or POD layout. The runtime
checks therefore complement, rather than replace, `tools/abi-verify`: the platform C compiler
checks 86 selected native/managed size, alignment, offset, width, callback, and prototype facts
against the headers, and now rejects headers outside this same version matrix. CNA's own complete
ABI baseline supplies the reviewed release-to-release shape/export/value diff. A new ABI version
must pass both evidence paths before it is added to the matrix.

## Automated fixtures

`scripts/Verify-NativeAbiCompatibility.sh` builds dependency-free shared libraries and runs each in
a fresh managed process. The exact matrix is:

| Fixture | Expected | Property proved |
| --- | --- | --- |
| `exact-0.6.0` | Accept | Exact expected ABI. |
| `additive-0.7.0` | Accept | Reviewed additive minor. |
| `additive-0.7.0-extra-symbol` | Accept | Unrelated added exports do not break a consumer. |
| `reviewed-subset-0.8.0` | Accept | Explicit consumer-specific 0.8 matrix entry. |
| `missing-required-symbol` | Reject | Any missing managed import fails at load, not at first use. |
| `changed-required-signature` | Reject | A testable core signature/out-parameter change fails its canary. |
| `incompatible-major-1.0.0` | Reject | Major mismatch. |
| `structurally-incompatible-0.7.0` | Reject | A same-major version cannot override guarded shape evidence. |
| `malformed-metadata-0.0.0` | Reject | An incomplete/unrecognized encoded generation. |
| `unreadable-metadata` | Reject | Missing version export. |

The fixture inventory is generated from and checked against the managed import declarations. CI
runs this gate without a protected CNA binary; package acceptance reruns it and additionally sends
the selected real library through the same probe.
