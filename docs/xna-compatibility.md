# XNA compatibility status

`CNA.XnaCompat` is intended to let XNA 4.0 game source recompile against CNA. The current build is
useful but **not API-complete**. Compatibility claims in this repository are evidence-based:

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
- target types: 239;
- unallowlisted diagnostics: 1,467;
- accidental `CNA.*` public/protected signature findings: 118;
- reviewed exceptions: 0;
- verifier exit code: 1.

The tool compares type kind/access/base/interfaces/modifiers/generics/layout/attributes, members,
parameters/modifiers/defaults, properties/indexers/accessors, events, fields/constants/enums,
delegates, and nested types. Its allowlist requires an exact diagnostic identity and rationale and
reports stale entries. The older regex/name counter remains useful only for discovery; it cannot
establish XNA parity.

## What was repaired in this audit

The public XNA hierarchy now takes priority over implementation reuse for these coherent groups:

- `DrawableGameComponent : GameComponent` and compat-only `IGameComponent` collections/events;
- dynamic vertex/index buffers and dynamic sound instance inheritance;
- `ResourceContentManager : ContentManager`;
- `GraphicsResource` ancestry for textures, buffers, effects, render targets, vertex declarations,
  SpriteBatch, and graphics states;
- `ModelEffectCollection : ReadOnlyCollection<Effect>`;
- compat-generic `CurveKeyCollection`, including XNA's shallow clone behavior;
- exact generic `DrawUser*` overload families;
- CNA renderer diagnostics moved out of the strict namespace to `CNA.XnaCompat.Extensions`.

These changes use composition, internal adapters, and single-owner backends. They do not make the
remaining 1,467 differences less real.

## Principal remaining contract failures

- 55 wrong base types and 39 interface mismatches, concentrated in Game/device management,
  models, audio/XACT, media, storage, and collections;
- 118 CNA type leaks through strict-profile public/protected signatures;
- 688 missing members and 23 missing types;
- `ContentReader` has the wrong base and the generic `ContentTypeReader<T>` machinery is absent;
- several kinds/layouts are wrong (`AudioCategory`, `RendererDetail`, `DisplayMode`);
- public helper base types and inherited CNA-only members remain visible;
- GamerServices, networking, device/sensor, Xbox/Phone, and Content Pipeline scope needs separate
  authoritative inventories rather than blanket exclusion.

## Source compatibility corpus

`tests/CNA.XnaCompat.CompileProbe` builds with the solution and currently locks in assignments for
the repaired component, dynamic-buffer, dynamic-audio, content-manager, graphics-resource, curve,
model-effect, state, vertex-declaration, and SpriteBatch relationships. It is the seed of a larger
corpus, not proof that arbitrary games compile.

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
| Microsoft XNA reference assemblies | Metadata authority; compile-corpus expansion pending | Not run |

A build is not a runtime claim.

## Behavior and content

The managed suites currently pass 532 framework and 169 compat tests. A current ABI 0.6.0 CNA
library passes all 103 native integration tests in both Debug and Release under Xvfb. That proves
the exercised routes, not all XNA behavior.

Known high-priority content limitation: `ContentManager.Load<T>` handles selected built-ins, but
the XNA reader type system and ordinary custom `Content.Load<MyType>()` path are not implemented.
`LoadForeign<T>` is a CNA extension, not a source-compatible substitute. Managed reader machinery,
shared resources, reader activation/versioning, external references, and exception behavior remain
P0 work.

Behavioral differential coverage is also incomplete for validation order, disposed behavior,
lifecycle/event order, graphics state transitions, SpriteBatch rules, input transitions, audio,
media, and storage. Unsupported exceptions are assessed case by case; their presence alone neither
proves a bug nor compatibility.

## Extension boundaries

| Contract | Current status |
| --- | --- |
| Strict XNA 4.0 | Baseline; measured, incomplete, no allowlisted differences |
| FNA extensions | No deliberate extension subset promised yet; template baseline compiles |
| MonoGame extensions | No deliberate extension subset promised yet; template baseline compiles |
| Kni portability | Template baseline compiles; no extension or runtime claim |
| CNA extensions | Renderer diagnostics are explicit in `CNA.XnaCompat.Extensions`; inherited CNA pollution remains a verifier failure |

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
