# CNA.NET engineering roadmap

Last measured: 2026-08-22. Session history and superseded decisions live in
[`NEXT.md`](NEXT.md). This file is the current, normative plan.

## Current verified state

**Not release-ready.** The selected seven-assembly XNA 4.0 Windows runtime profile is now public-
metadata complete: the strict facade has the same 257 types and produces zero verifier
diagnostics with an empty allowlist. Remaining compatibility work is behavioral, profile breadth,
native-platform validation, content fixtures, packaging, and release engineering.

| Area | Measured result |
| --- | --- |
| Debug and Release solution build | 0 warnings, 0 errors |
| Managed tests | 543 `CNA.Framework` + 199 `CNA.XnaCompat`, all passing |
| Native integration | 114/114 passing in Debug and Release on Linux under Xvfb with an ABI 0.6.0 CNA library |
| Compile probe | Identical source passes XNA, CNA, FNA, and MonoGame; Kni fails the XNA `VertexDeclaration : GraphicsResource` assignment |
| Behavior corpora | 299 observations: 83 math/geometry + 23 input + 153 graphics + 13 resource/lifetime + 27 content-error. Identical source compiles against XNA, CNA, FNA, and MonoGame; CNA and MonoGame execute all 299 on Linux, FNA executes the 133 pure observations plus 118 safe graphics/resource observations and emits 48 deterministic opt-in placeholders, and the Windows XNA snapshot remains pending |
| Ownership stress | Separate process passes 100/100 cycles in Debug and Release: 50 explicit/double-dispose cycles, 50 finalizer cycles, 10 throwing device-disposal handlers, and complete game teardown/recreation each cycle |
| XNA Windows runtime metadata | 257 reference types, 257 target types, 0 differences, empty allowlist |
| CNA public-type leakage | 0 findings in public/protected strict-profile signatures |
| Template | CNA build, generated-project build, and real 60/600-frame CNA runs pass |
| Other engines | Source builds pass for FNA, MonoGame, and Kni; 60-frame MonoGame and Kni runs pass; configured FNA runtime reports unavailable with exit 2 |
| Packages | None; all shipping projects remain `IsPackable=false` |
| Tested platform | Linux x64 only in this run |

The metadata result is produced by `tools/api-compat`, not by the legacy name counter. The current
hard invariants are:

```text
TOTAL_DIAGNOSTICS=0                   ALLOWLIST_ENTRIES=0
CNA_TYPE_LEAK=0                       BASE_TYPE_MISMATCH=0
INTERFACE_MISMATCH=0                  MISSING_TYPE=0
MISSING_MEMBER=0                      UNEXPECTED_TYPE=0
UNEXPECTED_MEMBER=0                   PARAMETER_NAME_MISMATCH=0
```

No difference is allowlisted. Both the normal strict verifier and the standalone leak gate exit 0.

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

- Metadata comparison: 0 missing, mismatched, or unexpected items with the allowlist empty.
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

### 1. Facade-first hierarchy repair: structurally complete

Use composition/internal adapters wherever inheriting a `CNA.*` implementation changes XNA's
public hierarchy. Every exported strict-profile type now has an XNA/BCL base rather than a
`CNA.*` base, and the standalone public/protected leak gate is clean.

Completed on 2026-08-22 across the two facade-repair runs:

1. `Game`, `GraphicsDeviceManager`, `GraphicsDevice`, `GameWindow`, and their service/device-setting
   types are facade-first and no longer expose CNA implementation inheritance.
2. The managed content-reader type system and ordinary custom `Content.Load<T>()` path are
   implemented; its remaining work is fixture breadth and behavioral parity, not hierarchy repair.
3. `Model`, `ModelBone`, `ModelMesh`, `ModelMeshPart`, their exact read-only collections, and nested
   enumerators are composition facades. Loaded models are lifetime-tracked by `ContentManager`.
4. The complete strict Audio/XACT family is facade-first, including exact exception bases and the
   `AudioCategory`/`RendererDetail` kind/layout corrections.
5. Media and Storage use composition and exact BCL/XNA collection bases. The public
   `MediaLibraryObject<TBase>`, `ReadOnlyMediaCollection<TCompat,TBase>`, and
   `NamedModelCollection<T>` implementation bases are no longer exported.
6. All 60 public conversion operators connecting strict XNA value types to `CNA.*` were replaced
   with internal conversion helpers; internal behavior is preserved without adding 120 public
   contract findings.
7. Remaining direct-inheritance graphics/core types were repaired, including `BoundingFrustum`,
   `OcclusionQuery`, `SpriteFont`, state/texture collections, graphics exceptions, `DisplayMode`,
   and `LaunchParameters`.

The final metadata-completion run added all design converters, `GamerServicesComponent`, and the
exact touch enumerator; completed vector, quaternion, matrix, color, plane, ray, rectangle and
bounding-volume families; repaired input state/enums; and closed effects, vertex declarations,
viewport, packed vectors, modifiers, attributes, accessors, constants and parameter names. The
same compile corpus builds against both CNA and Microsoft XNA references, and deterministic
math/geometry and input corpora now provide a cross-engine behavior baseline.

Next work must preserve every zero above. Priorities are now:

1. Execute and archive the 299-observation differential corpus on a Windows XNA 4.0 runtime. CNA
   and MonoGame execute the complete snapshot on Linux; FNA executes all safe observations and
   retains deterministic placeholders for 48 process-fatal comparator paths. Direct XNA source/IL
   has adjudicated implemented behavior, but only Windows can provide the independent runtime
   snapshot.
2. Continue graphics behavior work from the exact blockers in
   [`docs/native-behavior-blockers.md`](docs/native-behavior-blockers.md): device-event ordering,
   cross-device state/resource validation, additional format/mip/rectangle combinations, and the
   native routes that cannot yet represent XNA behavior.
3. Expand content fixtures beyond the 27 malformed/error observations into external references,
   compressed payload variants, built-in reader failures, and stream-ownership cases without
   changing the exact public contract.
4. Deepen native input with deterministic polling/injection support before adding CI assertions;
   cover dead zones, packet numbers, disconnects, focus, wheel state, and multiple players.
5. Preserve the checked-in leak-only CI gate and add the full strict zero-diagnostic job where
   protected XNA reference artifacts can be supplied legally.
6. Inventory additional XNA profiles separately; never merge Phone, Xbox, networking, or Content
   Pipeline types into the completed Windows runtime profile without an authoritative profile.

### 2. Resource ownership

The implementation must distinguish owned, borrowed, adopted, and parent-owned native handles.
`StockEffect.Dispose()` now releases its base reflection handles, fixing a reproduced game-destroy
failure, and `GameComponent.Dispose()` no longer destroys a native component twice.

The separate ownership runner now completes 100 cycles in both Debug and Release. Each cycle
creates a game, exercises textures, buffers, SpriteBatch, SpriteFont atlas ownership, effects and
parent-owned reflection objects, SoundEffect, media, storage, an adopted `Texture2D.FromStream`
handle, and a content-managed model graph, then destroys and recreates the game. Even cycles use
explicit/double disposal; odd cycles abandon resources and force finalization. This found and
fixed a real leak: service-provider-created `CNA.Content.ContentManager` instances held an owned
raw handle without a finalizer, preventing the next game from being created. Owned content
managers now use `NativeResourceHandle`; game-borrowed managers remain non-owning.

The SafeHandle audit now protects native calls with explicit managed liveness (`GC.KeepAlive`),
and owner-thread destruction is queued, drained, and retried at game safe points. Regression tests
cover finalizer deferral, cross-thread disposal, parent-release retry, callback exception
containment, failed unsubscribe rooting, and XNA-order `GraphicsDevice.Disposing` delivery after
the disposed state becomes visible. Managed teardown stays in `finally` when a handler throws.

Remaining criteria include authored XACT bank ownership, `VideoPlayer` frame-texture lifetime,
exception paths not constructible with current assets, long-duration/multithreaded stress beyond
100 cycles, and an upstream owner-thread pump if handles may outlive every managed `Game` safe
point.

## P0 — content compatibility

The structural managed work is complete. `ContentManager` has the correct public base relationship,
service-provider constructors, cache, unload and resource-manager facade. `ContentReader` derives
from `BinaryReader`; abstract and generic `ContentTypeReader` contracts, reader-table activation
and versioning, shared resources, nested/existing objects, disposable tracking, LZX handling and
ordinary custom `Content.Load<MyType>()` are implemented.

The pure corpus now has 27 deterministic content-error observations covering invalid magic,
platform/version/profile, truncated and inconsistent headers/payloads, compression flags, reader
table activation/version/index failures, shared-resource indices, wrong target type, missing
assets, reader exceptions, duplicate disposables, cache/root-directory behavior, and unload/dispose
after failure. The managed path now normalizes truncation to `ContentLoadException`, validates XNB
headers and reader versions/indices, records duplicate disposable occurrences like XNA, clears
unload state in `finally`, and remains disposed after an unload exception.

Remaining content work:

1. Expand built-in reader fixtures over additional uncompressed and LZX failures; inventory
   MonoGame LZ4 separately as an extension.
2. Deepen the existing external-reference and failed-`OpenStream` disposal observations with
   nested/missing/path-normalization references and successful-stream ownership; add compressed
   truncation and partial shared-resource fixtures that require a broader binary fixture builder.
3. Execute the identical 27 observations on Windows XNA and preserve the zero-diagnostic content
   surface while adjudicating any differences.

Completion: a user-defined reader/content type loads through unchanged XNA-style source and the
same fixture produces normalized equivalent results on an available reference implementation.

## XNA profile inventory

| Profile/family | Current treatment | Completion criterion |
| --- | --- | --- |
| XNA 4.0 Windows runtime | Metadata complete: 257/257 types, 0 differences, 0 CNA leaks | Preserve zero; complete behavioral corpus |
| XACT runtime | Included in current seven-assembly profile | Exact API plus authored-bank tests where assets exist |
| GamerServices | Only what selected Windows assemblies expose is currently measured | Inventory reference assemblies; provide compile-time API with deterministic unsupported behavior where services are extinct |
| Networking/session APIs | Not yet inventoried against authoritative assemblies | Separate profile and explicit status per type |
| Windows Phone sensors/devices | Not yet inventoried | Separate platform profile; no silent exclusion |
| Xbox-only APIs | Not yet inventoried | Separate platform profile, normally compile-time facade plus platform behavior |
| Content Pipeline/build-time assemblies | Explicitly outside the runtime profile | Metadata inventory and separate roadmap/package; never count it as runtime completion |

## Behavioral audit

The earlier 2026-08-22 source scan found 0 `NotImplementedException` sites, 36 explicit
`throw new NotSupportedException` sites, and 2 `TODO` comments (both in the LZX decoder's documented
reference-compatible edge paths). This run added deliberate `NotSupportedException` guards where
the native ABI cannot represent XNA semantics; the exact active native blockers are classified in
[`docs/native-behavior-blockers.md`](docs/native-behavior-blockers.md), so raw exception counts are
not a progress metric.

- Keep searching `TODO`, `FIXME`, unsupported exceptions, fallbacks, constant answers, empty
  methods, and documented deviations.
- Add golden/differential tests for validation order, exception types, null/range/disposed cases,
  event order, collection mutation, math edge cases, graphics state, content caching, input
  transitions, audio/media state, and lifecycle ordering.
- Keep alternate-engine results as comparators rather than authorities. The current 299-observation
  snapshot includes 83 math/geometry, 23 input, 153 graphics, 13 resource/lifetime, and 27
  content-error observations. FNA's 48 gated graphics paths demonstrate why comparator crashes and
  exceptions cannot define XNA behavior. Actual XNA metadata/source/IL, documentation, and the
  Windows runtime snapshot decide strict behavior.
- Preserve strict behavior in `CNA.XnaCompat` even where `CNA.Framework` intentionally differs.
  `CurveKeyCollection.Clone()` now demonstrates this: compat is shallow while the CNA API remains
  independently designed.

Completion is a published behavior matrix identifying the reference implementation and exact test
corpus, not a percentage inferred from source.

## CNA ABI and interop

`tools/coverage` now discovers repositories/libraries relatively or through `CNA_ROOT`,
`CNA_NATIVE_LIBRARY`, and `CNA_NATIVE_DIR`; ELF, PE, and Mach-O symbol tools are separated. The
latest header sweep found no declared imports absent from the selected headers and no arity
mismatches. An older ABI 0.1.0 library was correctly rejected; ABI 0.6.0 passed all 114 integration
tests in both configurations.

Next criteria:

- compile-time layout assertions for every interop struct, union, enum width, bool, callback, and
  string view;
- symbol resolution against current Linux, Windows, and macOS builds using the platform's real
  export mechanism;
- callback rooting/concurrency tests and systematic SafeHandle add-ref/keep-alive audit;
- never infer cross-platform ABI validity from `nm` alone.

## Extensions

Current evidence is deliberately narrow:

- Strict XNA: exact public metadata for the selected Windows runtime profile; behavioral parity and
  additional product profiles remain incomplete.
- FNA: the template source compiles against a configured FNA.dll; no FNA extension subset is yet
  promised.
- MonoGame: the template compiles and completes 60 frames against DesktopGL 3.8.1.303; extension
  inventory and broader runtime matrix are pending.
- Kni: the template compiles and completes 60 frames against the 4.2.9001 framework and
  4.2.9001.1 SDL2.GL backend, while the strict compile corpus records Kni's non-XNA
  `VertexDeclaration` ancestry; broader runtime matrix is pending.
- CNA: renderer diagnostics live in `CNA.XnaCompat.Extensions`; the strict namespace now has zero
  public/protected CNA-type leaks.

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

The strict API job is green with zero diagnostics and an empty allowlist. Treat any future
diagnostic—including a leak, hierarchy/interface regression, unexpected extension, or parameter
name change—as a gate failure. CI reference assemblies must be supplied legally through
`XNA_REFERENCE_PATH` or an equivalent protected artifact.

## Precise upstream CNA requirements

Managed work should continue around these. Do not modify upstream without authorization. The
argument shape, observed fallback, and required versioned C ABI for each graphics limitation are
specified in [`docs/native-behavior-blockers.md`](docs/native-behavior-blockers.md). In summary:

- `Present(source,destination,window)` needs a versioned descriptor; the current route carries no
  arguments, so only the all-default tuple is forwarded.
- Dynamic vertex raw/window uploads need `SetDataOptions`; dynamic index window uploads currently
  reject non-`None` options. Representable complete typed uploads now forward all three values.
- Vertex-buffer partial-element strided transfers need independent element-size and buffer-stride
  scatter/gather fields; complete declaration-sized and contiguous full-byte windows work.
- Texture2D arbitrary compatible structs, broader backbuffer readback, raw Texture3D readback, and
  raw per-face TextureCube transfers need format/element-size-aware byte routes.
- Resource create/destroy callbacks need a stable resource identity and round-trippable tag. The
  historical managed two-argument thunk did not match the native three-argument callback and could
  crash immediately; the current managed API safely emits no fake event.
- Dynamic-buffer `ContentLost` needs real renderer loss/recreation notifications.
- The selected backend needs XNA-compatible SpriteBatch treatment for unknown sort values and
  non-finite sprite data, plus compressed DXT texture upload support for the checked font atlas.
- Dynamic audio: `cna_sound_effect_instance_apply_3d_multi_ext` is now bound and used atomically,
  but the native implementation deliberately rejects listener counts other than one. True
  multi-listener mixing therefore requires an implementation change, not a new ABI entry point.
- Compiled effect bytecode needs a renderer/native implementation before `.fx` runtime support can
  be claimed on that renderer.
- The historical SIGTERM `pure virtual method called` shutdown report remains open until reproduced
  or disproved on the current native build.

Custom managed content readers are not listed here because their managed XNB machinery is now
implemented and does not require an upstream CNA change.

## Reproducible commands

```bash
dotnet build CNA.sln -c Debug
dotnet build CNA.sln -c Release
dotnet test tests/CNA.Framework.Tests/CNA.Framework.Tests.csproj
dotnet test tests/CNA.XnaCompat.Tests/CNA.XnaCompat.Tests.csproj

CNA_NATIVE_LIBRARY=/path/to/libcna_c_api.so \
  xvfb-run -a dotnet test tests/CNA.Integration.Tests/CNA.Integration.Tests.csproj -c Debug
CNA_NATIVE_LIBRARY=/path/to/libcna_c_api.so \
  xvfb-run -a dotnet test tests/CNA.Integration.Tests/CNA.Integration.Tests.csproj -c Release

dotnet run -c Release --project tests/CNA.XnaCompat.CompileProbe
CNA_NATIVE_LIBRARY=/path/to/libcna_c_api.so \
  XNA_GRAPHICS_PROBE_DRAW_VALIDATION=1 \
  XNA_GRAPHICS_PROBE_DESTRUCTIVE_LIFECYCLE=1 \
  XNA_GRAPHICS_PROBE_UNSAFE_CONSTRUCTORS=1 \
  xvfb-run -a dotnet run -c Release --project tests/CNA.XnaCompat.GraphicsProbe

CNA_NATIVE_LIBRARY=/path/to/libcna_c_api.so \
  CNA_OWNERSHIP_STRESS_FAMILY=all \
  xvfb-run -a dotnet run -c Release \
  --project tests/CNA.OwnershipStress/CNA.OwnershipStress.csproj -- 100

XNA_REFERENCE_PATH=/path/to/xna-reference-assemblies \
  dotnet run --project tools/api-compat -c Release -- --format json

CNA_ROOT=/path/to/cna python3 tools/coverage/sweep.py
```

On Windows with XNA 4.0 and the .NET Framework 4.8 developer pack:

```powershell
.\scripts\Capture-XnaSnapshots.ps1 `
  -XnaReferencePath 'C:\path\to\XNA\References\Windows\x86'
```
