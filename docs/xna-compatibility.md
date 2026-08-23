# XNA compatibility status

`CNA.XnaCompat` is intended to let XNA 4.0 game source recompile against CNA. Its public metadata is
now exact for the selected seven-assembly Windows runtime profile; behavioral parity, additional
XNA product profiles, native platform coverage, and release packaging remain incomplete.
Compatibility claims in this repository are evidence-based:

1. source compatibility;
2. exact public metadata compatibility;
3. behavioral compatibility;
4. selected FNA/MonoGame/Kni portability;
5. binary compatibility, which is not a primary goal.

The CNA C/C++ API determines what the backend can do. It is not the authority for Microsoft's
managed type system.

## Authoritative measured profile

The current strict profile is XNA 4.0 Windows runtime and combines seven locally supplied XNA
reference assemblies listed in `tools/api-compat/profiles/xna40-windows-runtime.json`. Reference
assemblies are inspected as metadata only and are not redistributed.

Run the verifier with:

```bash
dotnet build CNA.sln -c Release
XNA_REFERENCE_PATH=/path/to/xna-reference-assemblies \
  dotnet run --project tools/api-compat -c Release --no-build -- --format json
```

As measured on 2026-08-22:

- reference types: 257;
- target types: 257;
- unallowlisted diagnostics: 0;
- accidental `CNA.*` public/protected signature findings: 0;
- reviewed exceptions: 0;
- verifier exit code: 0.

The tool compares type kind/access/base/interfaces/modifiers/generics/layout/attributes, members,
parameters/modifiers/defaults, properties/indexers/accessors, events, fields/constants/enums,
delegates, and nested types. Its allowlist requires an exact diagnostic identity and rationale and
reports stale entries. The older regex/name counter remains useful only for discovery; it cannot
establish XNA parity.

## What has been repaired

The public XNA hierarchy now takes priority over implementation reuse for these coherent groups:

- `DrawableGameComponent : GameComponent` and compat-only `IGameComponent` collections/events;
- dynamic vertex/index buffers and dynamic sound instance inheritance;
- `ResourceContentManager : ContentManager`;
- `GraphicsResource` ancestry for textures, buffers, effects, render targets, vertex declarations,
  SpriteBatch, and graphics states;
- `ModelEffectCollection : ReadOnlyCollection<Effect>`;
- compat-generic `CurveKeyCollection`, including XNA's shallow clone behavior;
- exact generic `DrawUser*` overload families;
- the complete Game/device/window/service and managed content-reader groups;
- model collections and model resource facades;
- the complete strict Audio/XACT, Media, and Storage families;
- graphics exceptions, `BoundingFrustum`, `OcclusionQuery`, `SpriteFont`, `DisplayMode`, and the
  remaining strict graphics collections;
- public implementation helper bases removed from the XNA namespace;
- all public XNA-to-CNA conversion operators replaced by internal conversion helpers;
- CNA renderer diagnostics moved out of the strict namespace to `CNA.XnaCompat.Extensions`.

The final completion pass also added the 13 design converters, `GamerServicesComponent`, the exact
touch enumerator, complete math/geometry overload families, XNA input state/enums, graphics
effects/vertex/viewport contracts, and exact packed-vector metadata. Modifiers, attributes,
accessors, constants, parameter defaults and parameter names are all exact. These changes use
composition, internal adapters, and single-owner backends.

## Remaining compatibility work

- preserve strict metadata, CNA-leak, base-hierarchy, interface, unexpected-member, and allowlist
  counts at zero as hard regression gates;
- execute the combined 299-observation snapshot on a Windows XNA runtime. CNA and MonoGame execute
  all observations on Linux; FNA executes all safe paths and emits 48 deterministic placeholders
  for comparator paths that can abort the process. Direct XNA source/IL resolves implemented strict
  behavior, but the installed XNA C++/CLI assemblies cannot run on Linux;
- continue differential work for device lost/reset/resource event ordering, cross-device graphics
  validation, additional format/mip/rectangle cases, audio/XACT, media, storage, and native input;
- deepen the current 27 malformed/error XNB observations beyond their initial external-reference
  and failed-OpenStream-disposal cases into nested/missing references, successful-stream ownership,
  and broader compressed/built-in-reader failures;
- GamerServices, networking, device/sensor, Xbox/Phone, and Content Pipeline scope needs separate
  authoritative inventories rather than blanket exclusion.

## Source compatibility corpus

`tests/CNA.XnaCompat.CompileProbe` builds with the solution and locks in assignments for the
repaired component, dynamic-buffer, dynamic-audio, content-manager, graphics-resource, curve,
model-effect, state, vertex-declaration, and SpriteBatch relationships. It contains 83
math/geometry, 23 input, and 27 content-error observations. The device-backed
`CNA.XnaCompat.GraphicsProbe` adds 153 graphics and 13 resource/lifetime observations. Output
records IEEE-754 bits, exact hash/string results, state flags, exception kinds, identity, and
lifecycle/collection behavior. The complete 299 observations run on CNA and MonoGame; FNA runs the
133 pure and 118 safe graphics/resource paths, with 48 deterministic opt-in placeholders for
process-fatal comparator paths. Identical source compiles against XNA, whose native C++/CLI runtime
still requires Windows. Direct XNA source/IL adjudicates implemented strict values. This remains a
focused corpus, not proof that arbitrary games compile or behave identically.

The identical compile-probe source passes against the local Microsoft XNA reference assemblies,
CNA.XnaCompat, FNA, and MonoGame. It fails against Kni at one deliberate XNA assertion:
Kni's `VertexDeclaration` is not assignable to `GraphicsResource`. This is recorded as an
implementation portability difference; the less demanding template source still builds and runs
on Kni.

The same representative template source currently compiles against:

| Target | Compile status | Runtime status in this audit |
| --- | --- | --- |
| CNA.XnaCompat | Pass | Pass: 60 and 600 frames, OPENGLES3, Linux x64 |
| FNA | Pass with explicitly configured FNA.dll | Unavailable: configured assembly could not load; clean exit 2, no frame claim |
| MonoGame DesktopGL 3.8.1.303 | Pass | Pass: 60 frames, llvmpipe, Linux x64 |
| Kni 4.2.9001 + SDL2.GL 4.2.9001.1 | Template pass; strict corpus has one hierarchy failure | Pass: 60 frames, llvmpipe, Linux x64 |
| Microsoft XNA reference assemblies | Strict corpus passes with 0 warnings/errors | Runtime corpus still requires Windows XNA 4.0 |

A build is not a runtime claim.

## Behavior and content

The managed suites currently pass 543 framework and 199 compat tests. A current ABI 0.6.0 CNA
library passes all 114 native integration tests in both Debug and Release under Xvfb. The isolated
ownership runner also passes 100 game teardown/recreation cycles in both configurations. That proves
the exercised routes, not all XNA behavior.

The XNA reader type system and ordinary custom `Content.Load<MyType>()` path are implemented,
including reader activation/versioning, shared resources, existing instances, disposable
tracking, and LZX handling. Twenty-seven malformed/error observations now cover compact invalid
headers, reader tables/indices, partial failures, wrong types, duplicate disposables, and cache/
dispose behavior. Initial external-reference and failed-stream-disposal paths are included.
Remaining content work is differential fixture breadth: nested/missing references, compressed
corruption, built-in tables, successful-stream ownership, and the Windows XNA exception snapshot.

Behavioral differential coverage now includes graphics state/collection identity, transfer
limitations, dynamic buffer options, SpriteBatch ordering/failure recovery, draw validation,
resource/device disposal, and Present argument handling. `GraphicsDevice.Disposing` now has
sender/count/disposed-state/double-dispose observations. Coverage remains incomplete for lost/reset
event order, cross-device/resource-format breadth, native input transitions, audio/XACT, media,
and storage.
Unsupported exceptions are assessed case by case; their presence alone neither proves a bug nor
compatibility. Exact native capability blockers live in `docs/native-behavior-blockers.md`.

The expanded corpus corrected XNA's fixed-adjugate `Matrix.Invert` behavior (including NaNs for a
singular zero matrix), reciprocal-once scalar vector division, XNA-local `Viewport.Project` and
`Unproject`, exact frustum/GJK edge behavior, packed-vector conversion/formatting, curves, and
exact-boundary `BoundingSphere.Contains(Vector3)` behavior. It also confirmed from XNA packing IL
that `Color(0.5f, NaN, +Infinity, -Infinity)` produces `00FF0080`, despite different FNA and
MonoGame results. Direct XNA IL fixes the arithmetic grouping for the observed quaternion/vector
result; the Windows run remains necessary as an independent runtime snapshot, not as a reason to
copy either alternate engine.

The input corpus covers keyboard masking/order, mouse and game-pad state strings/hashes, analog
clamping and virtual-button thresholds, physical-button filtering, and touch equality/clone/
enumerator edge cases. Constructor-supplied virtual or undefined `Buttons` bits are deliberately
filtered like XNA rather than retained as physical button state. Native polling, packet/disconnect,
dead-zone provenance, and multi-player behavior still need fixture-backed coverage.

## Extension boundaries

| Contract | Current status |
| --- | --- |
| Strict XNA 4.0 | Exact public metadata for the selected Windows runtime profile; behavior and additional profiles remain incomplete |
| FNA extensions | No deliberate extension subset promised yet; template baseline compiles |
| MonoGame extensions | No deliberate extension subset promised yet; template baseline compiles |
| Kni portability | Template baseline compiles; no extension or runtime claim |
| CNA extensions | Renderer diagnostics are explicit in `CNA.XnaCompat.Extensions`; strict-profile CNA signature leaks are zero |

New extensions require an authority, a source-portability use case, an explicit status
(`implemented`, `unsupported`, `upstream blocker`, `not applicable`, or `planned`), and a home that
does not alter the strict XNA contract.

## Profile boundaries

The current 257-type measurement is specifically the selected Windows runtime assemblies. It is
not the entire historical XNA product:

- XACT runtime is included;
- GamerServices and networking/session APIs need separate inventory;
- Windows Phone sensor/device APIs need a separate platform profile;
- Xbox-only APIs need a separate platform profile;
- Content Pipeline/build-time assemblies are a separate product surface and roadmap.

An extinct historical service can still have a useful compile-time facade with deterministic
unsupported/platform behavior. Omission is not justified merely because FNA omitted it.

## Binary compatibility

Already-compiled Microsoft XNA binaries expect Microsoft's assembly identities and strong names.
CNA.NET does not counterfeit those identities. Recompile source against `CNA.XnaCompat`; investigate
binary compatibility separately without sacrificing source/API/behavior correctness.
