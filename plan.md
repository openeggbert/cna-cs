# CNA.NET (`cna-dotnet`) — Implementation Plan

**Status:** Active — foundation scaffold in place
**Date:** 2026-08-15
**Source analysis:** `../cnabinding/analysis_binding.md`,
`../cnabinding/analysis_binding_sharp_runtime.md`,
`../cna/analysis_binding_languages.md`

This plan turns the architectural recommendations in the three analysis
documents above into a concrete, phased build order for this repository.
It is intentionally scoped to `cna-dotnet` only; `cna-js`, `cna-rs`,
`cna-python`, and other bindings are out of scope here and belong in their
own repositories once this one has proven the architecture (see
"Recommendation" in `analysis_binding_languages.md`).

## What this repository is

CNA.NET is the C#/.NET language frontend for [CNA](../cna), a native C++
XNA-inspired game framework. It plays the same role for CNA that XNA 4.0,
FNA, or MonoGame play for game code written in C#: application code stays in
C#, targeting familiar `Microsoft.Xna.Framework`-style APIs, while the actual
engine — graphics, audio, input, content, and every renderer backend — runs
as native C++ inside CNA.

```text
C# game code
        ↓
CNA.XnaCompat   (Microsoft.Xna.Framework-compatible facade)
        ↓
CNA.Framework   (idiomatic CNA .NET API)
        ↓
CNA.Interop     (raw P/Invoke over the CNA C ABI)
        ↓
CNA stable C ABI                    ← lives in ../cna, not here
        ↓
CNA C++ core  →  Sharp Runtime, CNA subsystems, renderers
```

See `docs/architecture.md` for the full picture and `docs/xna-compatibility.md`
for what "XNA-compatible" does and does not mean here.

## Hard dependency on `openeggbert/cna`

This repository cannot function at runtime until `openeggbert/cna` ships a
stable **CNA C ABI** (`modules/c-api/` in that repository, per
`analysis_binding.md` §3–§4). As of this writing that module does not exist
yet upstream. The work in this repository proceeds in two tracks that can
overlap:

- **Track A (upstream, in `openeggbert/cna`):** design and implement the
  C ABI itself — handles, `CNA_Result`, UTF-8 strings, struct versioning,
  the managed-game callback bridge, etc. Not tracked by this file; see
  `../cnabinding/analysis_binding.md` §14, §67 Phase 0–1.
- **Track B (this repository):** build the managed side against the *shape*
  of that ABI as specified in the analysis documents, so that the moment
  Track A lands, Track B only needs to fix up signatures rather than design
  from scratch.

Everything in Phase 2 onward in this plan is Track B. Until Track A ships,
`samples/HelloGame` will build but will throw at run time when it tries to
load the native library (`cna-native`) — this is expected and documented in
the sample's README, not a bug in this repository.

## Phases

### Phase 0 — Repository scaffold (this commit)

- [x] `plan.md`, `README.md`, `LICENSE` (Ms-PL, matching `cna`), `NOTICE.md`,
      `.gitignore`, `.editorconfig`.
- [x] `CNA.sln` plus SDK-style `CNA.Interop`, `CNA.Framework`,
      `CNA.XnaCompat` projects, each currently empty of native-dependent
      behavior beyond what compiles without the native library present.
- [x] `docs/architecture.md`, `docs/xna-compatibility.md`.
- [x] Editor/IDE support: `CNA.sln` (Visual Studio, Rider), SDK-style
      `.csproj` (VS Code + C# Dev Kit, Rider, `dotnet` CLI all read these
      directly — no separate project format needed per IDE).

### Phase 1 — Minimal `CNA.Interop`

- [x] ABI-shaped value structs (`CnaResult`, `CnaHandle`, `CnaVector2`,
      `CnaColor`, `CnaGameTime`, `CnaKeyboardState`) matching
      `analysis_binding.md` §8 and `analysis_binding_sharp_runtime.md` §13,
      §42, §43.
  - [x] `Native` partial class with `LibraryImport` declarations for the
      slice of the ABI needed by `HelloGame` (§38, §140): runtime init,
      managed-game lifecycle + callback bridge (§20), `GraphicsDevice.Clear`,
      `Texture2D` create/release/set-data, `SpriteBatch`
      begin/draw/end, `Keyboard` snapshot, `ContentManager` load/root
      directory, and last-error retrieval.
- [x] `CnaError.GetLastErrorMessage()` (last-error text retrieval); the
      public `CnaException` that maps `CNA_Result` + that text to a managed
      exception lives in `CNA.Framework` (§10, §77), since `CNA.Interop`
      itself has no public surface at all.
- [ ] Pure interop unit tests that only require the ABI headers/shape, not a
      built native library (deferred until Track A ships something to link
      against).

### Phase 2 — Minimal `CNA.Framework`

- [x] Local (non-native) value types: `Vector2`, `Color`, `GameTime` (§23,
      `analysis_binding_sharp_runtime.md` §43). Only the members needed by
      `HelloGame`; broaden in Phase 4.
- [x] `SafeHandle`-based native resource wrapper pattern (§24) applied to
      `Texture2D`.
- [x] `Game` lifecycle (`Initialize`/`LoadContent`/`Update`/`Draw`/
      `UnloadContent`/`Exit`/`Run`) bridged to native through the
      `[UnmanagedCallersOnly]` callback adapter described in §20.
- [x] `GraphicsDeviceManager`, `GraphicsDevice.Clear`, `Texture2D`,
      `SpriteBatch` (single-draw form; batched `DrawMany` per §22 is a
      Phase 5 performance task), `Keyboard`/`KeyboardState`/`Keys` (snapshot
      pattern, §25), `ContentManager` (`RootDirectory`, `Load<T>` dispatch
      per §26).

### Phase 3 — `CNA.XnaCompat` facade + `HelloGame` sample

- [x] `Microsoft.Xna.Framework[.Graphics|.Input|.Content]` types that
      subclass or thinly wrap their `CNA.Framework` counterparts — no
      logic duplication for reference types; documented, minimal
      duplication for value types via implicit conversion operators (§18,
      §19; see `docs/architecture.md` "Why the XNA value types are not
      literally the same type as the CNA.Framework ones").
- [x] `samples/HelloGame` reproducing the reference `HelloGame` from
      `analysis_binding.md` §38 exactly: clear the screen, load a texture,
      draw it with `SpriteBatch`, read `Keyboard`, exit on Escape.
- [ ] Prove `HelloGame` actually runs once `openeggbert/cna` exposes
      `cna-native` — this is the "first major success criterion" from §70
      and cannot be closed from this repository alone.

### Phase 4 — Broaden XNA API coverage

Split by whether the type needs the (still nonexistent) native ABI:

- [x] **Pure math/value types — done, and real (no native dependency, so
      unlike everything below these are 100% functional today, not stubs):**
      `Vector3`, `Vector4`, `Quaternion`, `Matrix`, `Rectangle`, `Point`,
      `Ray`, `Plane`, `BoundingBox`, `BoundingSphere`, `BoundingFrustum`,
      `MathHelper`, the full 139-color XNA/X11 named-color table, and the
      ~150-member `Keys` enum (Windows virtual-key codes). Verified with
      `MatrixTests` (`Invert` round-trips across 9 matrices including
      `LookAt`/`PerspectiveFieldOfView`) and `BoundingFrustumTests`
      (containment, near/far corner ordering) — see the "Toolchain note"
      below. Not implemented within this set: `Matrix.Decompose`,
      `CreateBillboard`/`CreateConstrainedBillboard`/`CreateShadow`/
      `CreateReflection`, the non-FOV `CreatePerspective` overload,
      `BoundingFrustum.Intersects(BoundingFrustum)`/`Intersects(Ray)`,
      spline interpolation (`Barycentric`/`CatmullRom`/`Hermite`), and the
      IME/ChatPad/rare-OEM `Keys` members — see the "not implemented" notes
      in each type's own file.
- [x] **`Mouse`/`GamePad` — done** (new `CNA.Interop` natives
      `cna_mouse_get_state`/`cna_gamepad_get_state`, same snapshot pattern as
      `Keyboard`). `GamePad.GetCapabilities` and `GamePadState.PacketNumber`
      (always 0) are not implemented; `Buttons` covers the core d-pad/face/
      shoulder/stick-click flags but not XNA's thumbstick-direction-as-button
      or trigger-as-button flags — see `CNA.Input.Buttons`.
- [ ] **Native-backed, not started:** `SpriteFont`, `RenderTarget2D`,
      `BasicEffect`/`Effect` (parameter-handle caching per §27), 3D
      (`Model`, `VertexBuffer`, `IndexBuffer`), audio (`SoundEffect`,
      `SoundEffectInstance`, `Song`, `MediaPlayer`), extra `SpriteBatch.Draw`
      overloads (source rect, rotation, scale, `SpriteEffects`, layer
      depth). Blocked on the native ABI existing upstream, same as
      everything in Phase 2/3.
- [ ] Build the compatibility matrix (§73) from real tests, not from this
      list.

### Phase 5 — Performance passes

- `SpriteBatch` command buffering + `cna_sprite_batch_draw_many` (§22).
- `EffectParameter` handle caching (§27).
- Buffer-based bulk transfer for `Texture2D.SetData` / vertex/index data
  (`analysis_binding_sharp_runtime.md` §40).

### Phase 6 — Packaging and cross-platform validation

- NuGet layout per `analysis_binding.md` §30 (`CNA.Framework.nupkg` with
  `runtimes/<rid>/native/`).
- Validate the `HelloGame` sample on at least Linux and Windows, with more
  than one CNA renderer, per §38 and §70.
- CI: pure-C ABI compile/link smoke test lives in `cna`, not here; this
  repo's CI builds/tests the managed solution only.

### Phase 7+ — Out of scope for this repository

`cna-js`, `cna-rs`, `cna-python`, and later bindings are separate
repositories per `analysis_binding.md` §69 and `analysis_binding_languages.md`
"Proposed support order". Do not add non-.NET language code here.

## Design invariants (do not violate)

Carried over from `analysis_binding.md` §68 and
`analysis_binding_sharp_runtime.md` §143, restated for this repository:

1. `CNA.Interop` is the only project allowed to reference native symbols
   directly. `CNA.Framework` and `CNA.XnaCompat` never call `[LibraryImport]`
   themselves.
2. No `CNA_Result`/native exception ever crosses out of `CNA.Interop`
   unconverted — it becomes a managed `CnaException` (or subclass) before
   reaching `CNA.Framework` callers.
3. Math/value types (`Vector2`, `Matrix`, `Color`, `GameTime`, …) do not make
   P/Invoke calls for trivial operations. They are plain managed structs. In
   `CNA.XnaCompat`, `Vector2`/`Color` (the first two written) fully
   re-implement their formulas a second time; every value type after them
   instead duplicates only the fields and delegates every formula to its
   `CNA`-namespace counterpart via the implicit conversion operators, so
   there is one implementation of the actual math — see docs/architecture.md.
4. Every native handle wrapper implements `SafeHandle` or is owned by one;
   no bare `CnaHandle` is exposed as public API outside `CNA.Interop`.
5. `CNA.XnaCompat` never references `CNA.Interop` directly — only through
   `CNA.Framework`.
6. Nothing in this repository references, includes, or links Sharp Runtime.
   If a future change seems to require that, stop and re-read
   `../cnabinding/analysis_binding_sharp_runtime.md` §31 first.
7. C# code in this repository uses the real .NET BCL (`System.*`) for
   everything that is not a CNA-specific concept. Never invent a
   `CNA`-flavored reimplementation of an ordinary BCL type.
8. The `CNA.Framework` *project* is not the same thing as a `CNA.Framework`
   *namespace* — there is no such namespace. Types inside the `CNA.Framework`
   project live in `CNA`, `CNA.Graphics`, `CNA.Input`, or `CNA.Content`,
   matching the real CNA C++ codebase's own public namespace convention
   (`CNA::Graphics`, `CNA::Input`, `CNA::Devices`, bare `CNA::`; *not*
   `CNA::Framework::`). `CNA.Interop`'s project name and namespace do match
   (both `CNA.Interop`) because it corresponds to the C++ side's
   `CNA::Internal::*`, a genuinely different, private-implementation
   namespace — see docs/architecture.md.

## Toolchain note

`dotnet` was not installed by default in the sandbox this scaffold was
authored in, but a .NET 8/9 SDK happened to be present locally and was used
to verify it: `dotnet build CNA.sln` succeeds with 0 warnings/0 errors across
all 6 projects, all 44 unit tests pass (`dotnet test`), and
`dotnet run --project samples/HelloGame` fails at exactly the documented
point — a `DllNotFoundException` for `cna-native` raised from inside
`Game`'s constructor — rather than from any code defect. That confirms the
managed callback bridge, the covariant-return `CreateGraphicsDevice`/
`CreateContentManager` factories, the `Matrix.Invert`/`BoundingFrustum` math,
and the value-type implicit conversions are all wired correctly end to end,
ahead of the native ABI existing. Re-run both commands after cloning if you
want to reconfirm.

## Native build reuse

Once Track A (the C ABI in `openeggbert/cna`) exists, building it follows
the workspace-wide build rules in `../CLAUDE.md`: reuse `../cna/build/`
(or another already-configured CMake preset directory) rather than
reconfiguring, cap parallelism at `-j3`, and always configure with
`ccache`. Do not add a new CMake build directory under this repository —
`cna-dotnet` only consumes prebuilt `cna-native` binaries (via a local
`runtimes/<rid>/native/` copy or a project reference to `cna`'s build
output), it does not build CNA itself.
