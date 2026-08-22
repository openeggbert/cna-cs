# CNA.NET engineering roadmap

Last measured: 2026-08-22. Session history and superseded decisions live in
[`NEXT.md`](NEXT.md). This file is the current, normative plan.

## Current verified state

**Not release-ready and not API-complete.** `CNA.XnaCompat` runs useful XNA-style code on CNA, but
its remaining public contract differences are systemic rather than cosmetic.

| Area | Measured result |
| --- | --- |
| Debug and Release solution build | 0 warnings, 0 errors |
| Managed tests | 532 `CNA.Framework` + 169 `CNA.XnaCompat`, all passing |
| Native integration | 103/103 passing in Debug and Release on Linux under Xvfb with an ABI 0.6.0 OPENGLES3 CNA library |
| Compile probe | Identical source passes XNA, CNA, FNA, and MonoGame; Kni fails the XNA `VertexDeclaration : GraphicsResource` assignment |
| XNA Windows runtime metadata | 257 reference types, 239 target types, 1,467 unallowlisted differences |
| CNA public-type leakage | 118 findings in public/protected strict-profile signatures |
| Template | CNA build, generated-project build, and real 60/600-frame CNA runs pass |
| Other engines | Source builds pass for FNA, MonoGame, and Kni; 60-frame MonoGame and Kni runs pass; configured FNA runtime reports unavailable with exit 2 |
| Packages | None; all shipping projects remain `IsPackable=false` |
| Tested platform | Linux x64 only in this run |

The metadata result is produced by `tools/api-compat`, not by the legacy name counter. The current
diagnostic breakdown is:

```text
BASE_TYPE_MISMATCH=55                 CNA_TYPE_LEAK=118
FIELD_CONSTANT_MISMATCH=3             FIELD_TYPE_MISMATCH=1
INTERFACE_MISMATCH=39                 MEMBER_ATTRIBUTE_MISMATCH=16
MEMBER_MODIFIER_MISMATCH=14           MISSING_MEMBER=688
MISSING_TYPE=23                       PARAMETER_DEFAULT_MISMATCH=2
PARAMETER_MISMATCH=2                  PARAMETER_NAME_MISMATCH=242
PROPERTY_ACCESSOR_MISMATCH=11         PROPERTY_TYPE_MISMATCH=2
RETURN_TYPE_MISMATCH=9                TYPE_ATTRIBUTE_MISMATCH=2
TYPE_KIND_MISMATCH=3                  TYPE_LAYOUT_MISMATCH=3
TYPE_MODIFIER_MISMATCH=44             UNEXPECTED_MEMBER=185
UNEXPECTED_TYPE=5
```

No current difference is allowlisted. The normal verifier exits 1, as the quality gate should.

## Compatibility definition

Priority is XNA 4.0 source compatibility, exact public metadata, then observable behavior. Binary
compatibility with Microsoft's strong-named assemblies is not a release goal. CNA, FNA, and
MonoGame extensions must be outside the strict `Microsoft.Xna.Framework` contract or explicitly
reviewed and allowlisted.

The current measured profile is `tools/api-compat/profiles/xna40-windows-runtime.json`, aggregating:

- `Microsoft.Xna.Framework.dll`
- `Microsoft.Xna.Framework.Game.dll`
- `Microsoft.Xna.Framework.Graphics.dll`
- `Microsoft.Xna.Framework.Storage.dll`
- `Microsoft.Xna.Framework.Video.dll`
- `Microsoft.Xna.Framework.Input.Touch.dll`
- `Microsoft.Xna.Framework.Xact.dll`

The native CNA headers define available backend capabilities. They never define the managed XNA
contract.

## Definition of done for the declared Windows runtime profile

- Metadata comparison: 0 missing, mismatched, or unexpected items after a reviewed extension
  allowlist; no stale allowlist entries.
- Public/protected CNA-type leak check: 0 accidental findings.
- Every XNA inheritance/interface/generic assignment in the compile corpus builds unchanged.
- Managed differential tests cover high-risk value, collection, lifecycle, content, graphics,
  input, audio, media, and disposal behavior.
- Custom `ContentTypeReader<T>` and `Content.Load<MyType>()` work through normal XNA APIs.
- C declarations, symbols, layouts, ownership, and callback lifetimes are checked against the
  selected CNA ABI on each supported OS.
- The template and a freshly generated `dotnet new cna-game` project build and complete both short
  and stability native runs.
- NuGet/RID installation is reproducible on every claimed OS/architecture.

## P0 — public contract and ownership

### 1. Continue facade-first hierarchy repair

Use composition/internal adapters wherever inheriting a `CNA.*` implementation changes XNA's
public hierarchy. The repaired groups include components, dynamic buffers, dynamic sound,
`GraphicsResource`/textures/effects/states/SpriteBatch, content managers, and curves.

Next coherent groups, ordered by impact:

1. `Game`, `GraphicsDeviceManager`, `GraphicsDevice`, `GameWindow`, and service/device-setting
   types. Completion: exact metadata for the group and no inherited CNA members visible to source.
2. Model types and their exact `ReadOnlyCollection<T>`/nested enumerator contracts. Completion:
   no `CNA.Graphics.*` generic arguments or base types in the model family.
3. Audio/XACT facades, exception bases, `AudioCategory` and `RendererDetail` kind/layout fixes.
4. Media/storage facades and collection bases; remove public helper base types in the strict
   namespace.
5. Remaining graphics collections, adapters, exceptions, and `DisplayMode` kind mismatch.
6. Missing design converters, serialization attributes, nested enumerators, and
   `GamerServicesComponent` for the selected profile.

After each group, run the strict verifier and record the reduced counts in `NEXT.md`. Completion is
measured by metadata, not by matching names.

### 2. Resource ownership

The implementation must distinguish owned, borrowed, adopted, and parent-owned native handles.
`StockEffect.Dispose()` now releases its base reflection handles, fixing a reproduced game-destroy
failure, and `GameComponent.Dispose()` no longer destroys a native component twice.

Remaining criteria:

- disposal/finalization tests for every native-owning facade family;
- construct/dispose/reconstruct stress tests, including exception paths;
- no duplicate wrappers that both own one handle;
- parent-owned wrappers cannot release their parent resource;
- shutdown and signal behavior has an explicit tested policy.

## P0 — content compatibility

This is mostly managed work, not an upstream excuse. `ContentManager` now has the correct public
base relationship, service-provider constructors, cache, unload, and resource-manager facade, but
the reader type system is still structurally wrong.

Implement in this order:

1. Exact `ContentReader : BinaryReader`, abstract `ContentTypeReader`, generic
   `ContentTypeReader<T>`, reader metadata, and serialization attributes.
2. XNB reader table construction, type-reader activation and versioning.
3. Shared resources, nested objects, external references, stream ownership, and deterministic
   exception translation.
4. Route `Content.Load<MyType>()` through managed readers; do not require `LoadForeign<T>`.
5. Expand built-in reader fixtures over uncompressed and LZX XNBs; inventory MonoGame LZ4
   separately as an extension.

Completion: a user-defined reader/content type loads through unchanged XNA-style source and the
same fixture produces normalized equivalent results on an available reference implementation.

## XNA profile inventory

| Profile/family | Current treatment | Completion criterion |
| --- | --- | --- |
| XNA 4.0 Windows runtime | Active strict profile; 1,467 differences | Zero unreviewed diff |
| XACT runtime | Included in current seven-assembly profile | Exact API plus authored-bank tests where assets exist |
| GamerServices | Only what selected Windows assemblies expose is currently measured | Inventory reference assemblies; provide compile-time API with deterministic unsupported behavior where services are extinct |
| Networking/session APIs | Not yet inventoried against authoritative assemblies | Separate profile and explicit status per type |
| Windows Phone sensors/devices | Not yet inventoried | Separate platform profile; no silent exclusion |
| Xbox-only APIs | Not yet inventoried | Separate platform profile, normally compile-time facade plus platform behavior |
| Content Pipeline/build-time assemblies | Explicitly outside the runtime profile | Metadata inventory and separate roadmap/package; never count it as runtime completion |

## Behavioral audit

The 2026-08-22 source scan found 0 `NotImplementedException` sites, 36 explicit
`throw new NotSupportedException` sites, and 2 `TODO` comments (both in the LZX decoder's documented
reference-compatible edge paths). The unsupported sites include correct read-only collection/
stream behavior as well as real content/typed-transfer gaps; each still needs contract-specific
classification rather than a mechanical replacement.

- Keep searching `TODO`, `FIXME`, unsupported exceptions, fallbacks, constant answers, empty
  methods, and documented deviations.
- Add golden/differential tests for validation order, exception types, null/range/disposed cases,
  event order, collection mutation, math edge cases, graphics state, content caching, input
  transitions, audio/media state, and lifecycle ordering.
- Preserve strict behavior in `CNA.XnaCompat` even where `CNA.Framework` intentionally differs.
  `CurveKeyCollection.Clone()` now demonstrates this: compat is shallow while the CNA API remains
  independently designed.

Completion is a published behavior matrix identifying the reference implementation and exact test
corpus, not a percentage inferred from source.

## CNA ABI and interop

`tools/coverage` now discovers repositories/libraries relatively or through `CNA_ROOT`,
`CNA_NATIVE_LIBRARY`, and `CNA_NATIVE_DIR`; ELF, PE, and Mach-O symbol tools are separated. The
latest header sweep found no declared imports absent from the selected headers and no arity
mismatches. An older ABI 0.1.0 library was correctly rejected; ABI 0.6.0 passed all 103 integration
tests.

Next criteria:

- compile-time layout assertions for every interop struct, union, enum width, bool, callback, and
  string view;
- symbol resolution against current Linux, Windows, and macOS builds using the platform's real
  export mechanism;
- callback rooting/concurrency tests and systematic SafeHandle add-ref/keep-alive audit;
- never infer cross-platform ABI validity from `nm` alone.

## Extensions

Current evidence is deliberately narrow:

- Strict XNA: measured and incomplete.
- FNA: the template source compiles against a configured FNA.dll; no FNA extension subset is yet
  promised.
- MonoGame: the template compiles and completes 60 frames against DesktopGL 3.8.1.303; extension
  inventory and broader runtime matrix are pending.
- Kni: the template compiles and completes 60 frames against the 4.2.9001 framework and
  4.2.9001.1 SDL2.GL backend, while the strict compile corpus records Kni's non-XNA
  `VertexDeclaration` ancestry; broader runtime matrix is pending.
- CNA: renderer diagnostics moved to `CNA.XnaCompat.Extensions`; remaining inherited CNA pollution
  is still reported by the metadata/leak tool.

For each proposed FNA/MonoGame addition, record authority, source-portability value, implementation
status, and whether it belongs in an extension namespace/assembly. Do not merge APIs into a random
union.

## Template

The sibling `cna-cs-template` is now CNA-first and installable as `cna-game`. It uses raw
`Texture2D.FromStream` for the PNG, isolates CNA diagnostics, exercises 2D and a guarded 3D path,
and supports `--smoke-test`, `--stability-test`, and `--frames N`.

Next criteria:

- 600-frame CNA stability run on every additional claimed renderer (OPENGLES3 passes);
- diagnose the configured FNA assembly/runtime load failure and complete an FNA frame run;
- expand the passing MonoGame and Kni runs beyond this one Linux/x64 environment;
- generated project build outside both repositories on all supported hosts;
- generated output reduced to the clean consumer template if repository-only portability harness
  files become intrusive.

## Packaging and platform matrix

The intended package graph is documented in `docs/packaging.md`. Packaging remains blocked until
the public contract and native distribution policy are stable. Do not flip `IsPackable` merely to
produce misleading packages.

No OS/architecture is supported by claim until restore, Debug/Release build, ABI check, generated
template build, and native smoke/stability runs pass there. Current runtime evidence is Linux x64
with OPENGLES3 only.

## CI/tooling gates

Target quality gate:

1. restore and Debug/Release solution build;
2. managed unit tests and compile corpus;
3. strict metadata diff plus CNA leak check;
4. portable ABI/header validation;
5. native integration when an ABI-matched library is supplied;
6. CNA template build/run and generated-project build;
7. alternate-engine build/runtime jobs when dependencies are available;
8. package creation and isolated-consumer install test.

The strict API job cannot be green today: it correctly exits 1 for 1,467 unallowlisted findings.
Do not hide that with a blanket allowlist. CI reference assemblies must be supplied legally through
`XNA_REFERENCE_PATH` or an equivalent protected artifact.

## Precise upstream CNA requirements

Managed work should continue around these. Do not modify upstream without authorization.

- Texture3D: raw region upload already exists and is now used by generic `SetData<T>`; an equivalent
  raw readback route is still needed for `GetData<T>` beyond Color.
- TextureCube: general typed/raw per-face write and readback routes are needed beyond Color.
- Dynamic audio: `cna_sound_effect_instance_apply_3d_multi_ext` is now bound and used atomically,
  but the native implementation deliberately rejects listener counts other than one. True
  multi-listener mixing therefore requires an implementation change, not a new ABI entry point.
- Compiled effect bytecode needs a renderer/native implementation before `.fx` runtime support can
  be claimed on that renderer.
- The historical SIGTERM `pure virtual method called` shutdown report remains open until reproduced
  or disproved on the current native build.

Custom managed content readers are not listed here: implement the managed XNB machinery first.

## Reproducible commands

```bash
dotnet build CNA.sln -c Debug
dotnet build CNA.sln -c Release
dotnet test tests/CNA.Framework.Tests/CNA.Framework.Tests.csproj
dotnet test tests/CNA.XnaCompat.Tests/CNA.XnaCompat.Tests.csproj

XNA_REFERENCE_PATH=/path/to/xna-reference-assemblies \
  dotnet run --project tools/api-compat -c Release -- --format json

CNA_ROOT=/path/to/cna python3 tools/coverage/sweep.py

CNA_NATIVE_LIBRARY=/path/to/libcna_c_api.so \
  xvfb-run -a dotnet test tests/CNA.Integration.Tests/CNA.Integration.Tests.csproj
```
