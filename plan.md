# CNA.NET (`cna-dotnet`) — Implementation Plan

**Status:** Active — Phases 0-3 complete; Phase 4 essentially complete for
the scope this plan actually calls for (pure math/value types,
`Mouse`/`GamePad`, extra `SpriteBatch.Draw` overloads, `RenderTarget2D`,
`SpriteFont`, `SoundEffect`/`SoundEffectInstance`, the zero-ABI
vertex-format layer, `VertexBuffer`/`IndexBuffer`, `GraphicsDevice`'s draw
calls, `Effect`/`BasicEffect`, `Model`, and a scoped `Song`/`MediaPlayer`
done); Phase 5 also essentially complete (`SpriteBatch` command batching
done; bulk-buffer transfer for `Texture2D`/vertex/index data turned out to
already be done by every native-backed type's own original design;
`EffectParameter` handle caching is not applicable — this project has no
name-indexed effect-parameter system for it to apply to). `MediaQueue`/
`SongCollection` (multi-song playlists, shuffle, repeat-driven
auto-advance) and `VertexBuffer`/`IndexBuffer`'s real-XNA `Type`-taking
constructors also now done. What remains: `Model` file-loading and the
real `Album`/`Artist`/`Genre`/`MediaLibrary` scanning subsystem (Phase 4
follow-ups, deliberately deferred, not blocked) and Phase 6
packaging/cross-platform validation, tracked below.
**Date:** 2026-08-16 (see `NEXT.md` for the session-by-session history and
where to pick up)
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
      subclass or thinly wrap their `CNA`-namespace counterparts — no
      logic duplication for reference types; documented, minimal
      duplication for value types via implicit conversion operators (§18,
      §19; see `docs/architecture.md` "Why the XNA value types are not
      literally the same type as the CNA namespace ones").
- [x] `samples/HelloGame` reproducing the reference `HelloGame` from
      `analysis_binding.md` §38 exactly: clear the screen, load a texture,
      draw it with `SpriteBatch`, read `Keyboard`, exit on Escape.
- [ ] Prove `HelloGame` actually runs once `openeggbert/cna` exposes
      `cna-native` — this is the "first major success criterion" from §70
      and cannot be closed from this repository alone.

### Phase 4 — Broaden XNA API coverage

Split by whether the type needs the (still nonexistent) native ABI:

- [x] **Pure math/value types — done, complete, and real (no native
      dependency, so unlike everything below these are 100% functional
      today, not stubs).** `Vector3`, `Vector4`, `Quaternion`, `Matrix`,
      `Rectangle`, `Point`, `Ray`, `Plane`, `BoundingBox`, `BoundingSphere`,
      `BoundingFrustum`, `MathHelper`, the full 139-color XNA/X11
      named-color table, and the 160-member `Keys` enum (Windows
      virtual-key codes). As of the 2026-08-16 (session 5) pass, every gap
      this bullet used to list is closed: `Matrix.Decompose`,
      `CreateBillboard`/`CreateConstrainedBillboard`/`CreateShadow`/
      `CreateReflection`/`CreatePerspective`/`CreatePerspectiveOffCenter`,
      `BoundingFrustum.Intersects(BoundingFrustum)`/`Intersects(Ray)`,
      `Quaternion.Slerp`/`CreateFromRotationMatrix`, spline interpolation
      (`Lerp`/`SmoothStep`/`Barycentric`/`CatmullRom`/`Hermite` across
      `Vector2`/`3`/`4`), and the IME/ChatPad/rare-OEM `Keys` members are
      all implemented — see `NEXT.md`'s session-5 entry for per-method
      confidence notes (most are cross-checked/round-trip-tested with real
      unit tests; the rare-`Keys` ordinals and `CreateConstrainedBillboard`'s
      degenerate branch are recalled/approximated rather than independently
      verified, flagged as such in their own doc comments). That same pass
      also fixed a real pre-existing bug: `Vector3.Transform(Vector3,
      Quaternion)` was rotating by the *inverse* angle (wrong multiplication
      order against this project's `Quaternion.operator *` convention) —
      see `NEXT.md` for how it was caught and the fix. Verified with
      `MatrixTests` (`Invert`/`Decompose`/`CreatePerspective*` round-trips
      and cross-checks, including 9 matrices covering `LookAt`/
      `PerspectiveFieldOfView`), `QuaternionTests` (`CreateFromRotationMatrix`
      round-trips, `Slerp` endpoint/shortest-path checks), and
      `BoundingFrustumTests` (containment, frustum-vs-frustum, ray
      intersection, near/far corner ordering) — see the "Toolchain note"
      below.
- [x] **`Mouse`/`GamePad` — done** (new `CNA.Interop` natives
      `cna_mouse_get_state`/`cna_gamepad_get_state`, same snapshot pattern as
      `Keyboard`). `GamePad.GetCapabilities` — done as of 2026-08-16 (session
      5) via a new, self-designed (no upstream ABI shape exists for it)
      `cna_gamepad_get_capabilities` native call and `GamePadCapabilities`/
      `GamePadType` types; `GamePadType`'s numeric values are
      declaration-order guesses, not confirmed real XNA ordinals — see that
      type's own doc comment. `GamePadState.PacketNumber` (always 0) is
      still not implemented; `Buttons` covers the core d-pad/face/
      shoulder/stick-click flags but not XNA's thumbstick-direction-as-button
      or trigger-as-button flags — see `CNA.Input.Buttons`. `PlayerIndex`
      lives at the *root* `CNA`/`Microsoft.Xna.Framework` namespace, not
      `.Input` — matches real XNA, where it's shared by `GamePad` and the
      GamerServices/Storage APIs. When adding a new type, don't assume its
      real XNA namespace from where it "feels" like it belongs; check.
- [x] **Extra `SpriteBatch.Draw` overloads — done** (source rectangle,
      rotation, origin, scale, `SpriteEffects`, layer depth; both the
      position-based and destination-rectangle-based XNA overload families).
      Backed by one new native primitive, `cna_sprite_batch_draw_ex`, taking
      a `CnaSpriteDrawCommand` struct that matches the `CNA_SpriteDrawCommand`
      example in `analysis_binding.md` §22 field-for-field (that example was
      illustrating Phase 5 batching, not this single-draw call, but the
      field shape carries over exactly). `SpriteEffects` added as a new
      enum (`CNA.Graphics.SpriteEffects` / XnaCompat's own, numerically
      identical, parity-tested) — no bit values exist in the analysis docs,
      so real XNA 4.0's values were used from memory. Destination-rectangle
      overloads resolve to position+scale in C# (no native call of their
      own); everything else funnels through the one native primitive.
- [x] **`RenderTarget2D` — done, self-designed ABI, no doc backing.**
      Unlike the `Draw` overloads above, **no ABI shape for render targets
      exists anywhere in `analysis_binding.md` or
      `analysis_binding_sharp_runtime.md`** — confirmed by a full-text grep
      of both files, not an assumption. `cna_render_target2d_create` and
      `cna_graphics_device_set_render_target` are invented for this
      repository, following the general handle/`CnaResult` conventions used
      everywhere else, but with **no upstream reference to validate
      against** — treat these two functions as the least-trustworthy
      signatures in the whole `CNA.Interop` surface once Track A ships, more
      so than anything else in Phase 4. `RenderTarget2D` reuses
      `Texture2D`'s release/width/height native calls rather than getting
      its own (see `RenderTarget2D.cs` doc comment for why). XnaCompat's
      `RenderTarget2D` inherits from XnaCompat's own `Texture2D` (not this
      project's `RenderTarget2D`) so `Texture2D t = someRenderTarget;`
      compiles in game code — see that file and
      `GraphicsDevice.SetRenderTarget`'s doc comment for the
      accepts-`Texture2D`-not-`RenderTarget2D` looseness this required.
- [x] **`SpriteFont` — done, and needed *zero* new native ABI surface.**
      Real XNA 4.0 exposes a public `SpriteFont` constructor taking raw
      glyph arrays (for third-party font-building tools, not just its
      content pipeline) — reproduced field-for-field, which makes
      `MeasureString` pure managed code (real unit tests, no native
      dependency, same as the math value types) and `SpriteBatch.DrawString`
      a thin loop over the already-implemented `Draw` primitive (one native
      call per glyph, no dedicated draw-string native call). Follows the
      standard XNA/MonoGame "ABC" kerning-triple + cropping-rectangle
      layout algorithm — not invented for this repository, but also not
      verified against a real XNA binary (none available in this
      environment); verified instead against hand-worked expected values
      in `SpriteFontTests.cs`. Known incompleteness: does not implement
      XNA's `SpriteEffects`-driven line/character reversal for flipped
      text. `ContentManager.Load<SpriteFont>` — done as of 2026-08-16
      (session 5 continued): a new, self-designed (no upstream ABI shape,
      same caveat as `RenderTarget2D`) `cna_content_load_spritefont` native
      call returns a fixed-capacity (256 glyphs, using C# 12's `InlineArray`
      for a flat marshalled buffer with no two-call pointer/length dance)
      glyph table; `ContentManager.LoadSpriteFontData` unpacks it into
      exactly the shape `SpriteFont`'s public constructor wants, and each
      of `CNA.Content.ContentManager`/`CNA.XnaCompat`'s `ContentManager`
      builds its own namespace's `SpriteFont` from those same raw pieces —
      the same "return raw pieces, let each layer wrap its own type" split
      already used for `Texture2D`. The 256-glyph cap is a real, documented
      limitation (generous for XNA's default ASCII-range content-pipeline
      output, but a hard cap nonetheless) — a font with more glyphs is
      expected to fail loudly via `CnaResult`, not silently truncate.
- [x] **`SoundEffect`/`SoundEffectInstance` — done, 2026-08-16 (session 5,
      after the weekly reset).** No ABI shape for audio exists anywhere in
      the analysis docs (confirmed by a full-text grep of both — audio gets
      no concrete struct anywhere, unlike `SpriteBatch.Draw`'s §22 example,
      just class names to preserve and one `cna_audio_*` naming-convention
      bullet). Better-grounded than that makes it sound, though: the real
      `openeggbert/cna` C++ engine already has a working (if not yet
      C-ABI-exposed) `Microsoft::Xna::Framework::Audio::SoundEffect`/
      `SoundEffectInstance` implementation over SDL3_mixer
      (`modules/audio/`), and every native function/parameter added here is
      deliberately shaped to match that real implementation's actual method
      surface and documented semantics — not invented from nothing. Follows
      real XNA's public `SoundEffect(byte[], int, AudioChannels)` /
      7-arg-with-loop-points constructors exactly; `SoundEffectInstance` has
      no public constructor (matching real XNA, and the real C++ engine's
      own `private`, `SoundEffect`-friend-only constructor), reachable only
      via `SoundEffect.CreateInstance()`. `Volume`/`Pitch` pass through
      unclamped and `Pan` validates to `[-1, 1]`/`IsLooped` throws if
      already played, both reproduced in managed code specifically because
      the real C++ implementation performs those checks in the same place.
      `GetSampleDuration`/`GetSampleSizeInBytes` are pure arithmetic (no
      native call) and fully tested. Known gaps, deliberately not
      implemented: `SoundEffect`'s fire-and-forget `Play()`/
      `Play(volume,pitch,pan)` convenience methods (would need an
      instance-pooling mechanism this repository doesn't have — use
      `CreateInstance()` explicitly instead), 3D positional audio
      (`Apply3D`/`AudioListener`/`AudioEmitter`), the static
      `MasterVolume`/`DistanceScale`/`DopplerScale`/`SpeedOfSound`
      settings, and `SoundEffect`'s exact sample-rate range validation
      (8,000-48,000 Hz in real XNA — only a positive check is done here,
      lower confidence in the exact bounds). `ContentManager.Load<SoundEffect>`
      is also supported, same split-and-wrap pattern as `Texture2D`/
      `SpriteFont`.
- [x] **`VertexDeclaration`/`VertexElement`/`VertexElementFormat`/
      `VertexElementUsage`/`BufferUsage`/`IVertexType` and the five standard
      vertex structs (`VertexPosition`, `VertexPositionColor`,
      `VertexPositionTexture`, `VertexPositionColorTexture`,
      `VertexPositionNormalTexture`) — done, 2026-08-16 (session 6
      continued), and real (no native dependency).** First slice of the
      3D pipeline, deliberately scoped down from the full
      `Effect`/`VertexBuffer`/`Model` surface: confirmed the real
      `openeggbert/cna` C++ engine's own `VertexDeclaration` "auto-computes
      stride from element offsets/formats" exactly like real XNA's own
      elements-only constructor does — pure data/arithmetic, the same
      "escape hatch" pattern `SpriteFont` found for its own construction.
      Stride auto-compute uses `max(offset + GetTypeSize(format))` across
      elements (not element declaration order, not a running sum) —
      verified against all five standard vertex structs' known real-XNA
      strides (12/16/20/24/32) in `VertexDeclarationTests`. `BufferUsage`
      added alongside these since `VertexBuffer`/`IndexBuffer` (native-backed,
      done next, see below) needed it.
- [x] **`VertexBuffer`/`IndexBuffer`/`IndexElementSize` — done, 2026-08-16
      (session 6 continued yet further).** Second slice of the 3D
      pipeline. No ABI shape for either exists in the analysis docs
      (confirmed by grep — not even a naming-convention bullet, unlike
      audio's `cna_audio_*` mention), but shaped to match the real
      `openeggbert/cna` C++ engine's own working, tested,
      renderer-backend-wired `VertexBuffer`/`IndexBuffer` implementations
      (a renderer-owned GPU handle plus a CPU-side shadow buffer enabling
      `GetData` readback) — same grounding `SoundEffect` had against
      `modules/audio`. `SetData<T>`/`GetData<T>` use `where T : unmanaged`
      (real XNA uses the broader `where T : struct`) — a deliberate
      tightening, since `unmanaged` is what makes the `sizeof(T)`/`fixed`
      pointer marshalling actually valid, and every realistic vertex/index
      type is unmanaged anyway. Both the `VertexDeclaration`/`IndexElementSize`-taking
      constructors *and* real XNA's `Type`-taking overloads are implemented
      (the latter added 2026-08-16, session 6 continued autonomously past
      the original Phase 4/5 checkpoint — `VertexDeclaration.FromType`/
      `IndexBuffer.SizeForType`, reflection-based convenience sugar over
      the exact same native calls, zero new native ABI needed; see
      `NEXT.md` for the detail, including the `CNA.XnaCompat` mirror
      needing its own genuinely separate `FromType` since a
      compat-namespaced vertex struct implements a distinct `IVertexType`
      interface from the base layer's). **Real testability limitation, not just an untested corner:** unlike
      `SoundEffect`, whose constructor validates every argument before
      ever touching native code, `VertexBuffer`/`IndexBuffer`'s
      constructors call native immediately after minimal validation, so
      only their argument-validation failure paths (null/non-positive
      argument checks) are testable here at all — `SetData`/`GetData`
      cannot be exercised without a real `cna-native`, because there is no
      way to reach a successfully-constructed instance to call them on.
- [x] **`GraphicsDevice.SetVertexBuffer`/`Indices`/`DrawPrimitives`/
      `DrawIndexedPrimitives`/`PrimitiveType` — done, 2026-08-16 (session 6
      continued yet further still).** Third slice of the 3D pipeline —
      the actual draw calls, now that buffers exist to draw from.
      `DrawIndexedPrimitives`'s signature matches real XNA's full 6-parameter
      form (`primitiveType, baseVertex, minVertexIndex, numVertices,
      startIndex, primitiveCount`) exactly for API compatibility, but
      `minVertexIndex`/`numVertices` are accepted-and-validated without
      being forwarded to the native call — on modern GPUs they are driver
      hints real XNA/MonoGame themselves mostly ignore, so this project's
      minimal native surface omits plumbing them through the ABI at all.
      `DrawPrimitives`/`DrawIndexedPrimitives`'s own argument validation
      (non-negative indices, positive primitive count) is testable without
      native code, same reasoning as `VertexBuffer`/`IndexBuffer`'s
      constructors; `SetVertexBuffer`/`Indices`'s setter call native
      unconditionally (nothing to validate first) and are not testable
      here, same as `SetRenderTarget`.
- [x] **`Effect`/`BasicEffect`/`EffectTechnique`/`EffectPass`/
      `EffectPassCollection`/`DirectionalLight` — done, 2026-08-16 (session
      6 continued yet further still again).** Fourth slice of the 3D
      pipeline, and — same lucky break `SoundEffect`/`VertexBuffer` had —
      grounded against the real `openeggbert/cna` C++ engine's own working,
      tested `modules/graphics/` `BasicEffect` implementation, not
      invented: every property, `EnableDefaultLighting()`'s exact default
      three-light rig, and `OnApply()`'s parameter-computation algorithm
      are read directly from that implementation's own headers and
      `BasicEffect.cpp`'s `FillGpuDrawParams` method. Confirmed a second
      zero-ABI-until-Apply() escape hatch this session: constructing a
      `BasicEffect` and setting any of its properties is pure managed
      object state, no native call, same as `SpriteFont`'s own — only
      `Apply()` (via `Effect.Apply` → `BasicEffect.OnApply`) crosses into
      native code, through one new `cna_graphics_device_apply_basic_effect`
      native call taking a `CnaBasicEffectParams` struct (a plain mutable
      struct with object-initializer construction, not a large positional
      constructor — see `NativeStructs.cs`'s own doc comment for why that
      shape was chosen over the first draft). `EffectTechnique`/
      `EffectPass`/`EffectPassCollection` are minimal scaffolding so
      `effect.CurrentTechnique.Passes[0].Apply()` compiles and works, not a
      general effect-parameter system (`EffectParameter` itself is not
      implemented — nothing in `BasicEffect`'s own surface needs it).
      `CNA.XnaCompat.BasicEffect` deliberately extends
      `CNA.Graphics.BasicEffect` directly rather than getting its own
      compat `Effect`/`EffectTechnique`/`EffectPass`/`DirectionalLight`
      hierarchy — same "preserve the real logic's lineage over namespace
      purity" trade-off `RenderTarget2D` already made, and required here
      specifically because `DirectionalLight0/1/2` are constructed once
      inside the base class's own constructor with no seam for a compat
      subclass to intervene safely (see `NEXT.md` for the full reasoning,
      including why this is *not* a case for the `Indices`-style
      downcast-passthrough fix). Real, narrow, documented compat gap as a
      result: `effect.CurrentTechnique`/`.Passes`/`DirectionalLight0-2` are
      inherited unchanged and return `CNA.Graphics`-namespaced types, not
      XNA-namespaced ones — ordinary `var`-typed/chained usage
      (`effect.CurrentTechnique.Passes[0].Apply();`,
      `effect.DirectionalLight0.Enabled = true;`) still compiles and works;
      only an explicit XNA-namespaced type declaration for one of those
      three would fail to compile. `OnApply`'s pure-computation halves
      (`ComputeFogVector`/`ComputeLightingParams`'s eye-position) are
      exposed via `internal`-only test-only properties
      (`FogVectorForTests`/`EyePositionWorldForTests`) so they're directly
      unit-testable without a real `cna-native`, same reasoning as
      `VertexBuffer`/`IndexBuffer`'s constructor-validation-only
      testability split.
- [x] **`Model`/`ModelBone`/`ModelMesh`/`ModelMeshPart` and their four
      collection types (`ModelBoneCollection`/`ModelMeshCollection`/
      `ModelMeshPartCollection`/`ModelEffectCollection`), plus
      `IEffectMatrices`/`IEffectFog`/`IEffectLights` — done, 2026-08-16
      (session 6 continued yet further still again once more).** Fifth
      slice of the 3D pipeline, and the first one that needed **zero new
      native ABI surface** — the real `openeggbert/cna` C++ engine's
      `modules/graphics/` `Model`/`ModelMesh`/`ModelMeshPart`/`ModelBone`
      are pure C++ object composition, not renderer-handle-backed types at
      all: `Model.Draw()`/`ModelMesh.Draw()` are just C++ logic that calls
      already-existing native-backed primitives (`SetVertexBuffer`,
      `Indices`, `Effect.Apply()`, `DrawIndexedPrimitives`) — confirmed by
      reading the real engine's own `Model.cpp`/`ModelMesh.cpp`/
      `ModelMeshPart.cpp`, not invented. Every constructor, property, and
      the exact `Draw()` algorithm (absolute-bone-transform composition via
      a reused buffer, then per-mesh-effect `World = boneTransform * world`
      assignment through `IEffectMatrices`) is reproduced from that source.
      `IEffectMatrices`/`IEffectFog`/`IEffectLights` (real XNA interfaces,
      confirmed against the C++ engine's own headers) were added to
      `CNA.Graphics.BasicEffect` specifically because `Model.Draw()` needs
      to set `World`/`View`/`Projection` on whatever effect each mesh part
      uses without knowing its concrete type — `IEffectFog`/`IEffectLights`
      were free additions once `IEffectMatrices` existed, since
      `BasicEffect`'s existing properties already matched their members
      exactly. Real XNA's own `Model`/`ModelBone`/`ModelMesh`/
      `ModelMeshPart` constructors and several setters (`ModelBone.AddChild`,
      `ModelMeshPart.SetVertexOffset`/etc., `ModelEffectCollection.Add`/
      `Remove`) are content-pipeline-only (`internal`) in real XNA — this
      project has no content pipeline / model-file loader (a separate, much
      larger problem), so, matching the real C++ engine's own `CNAEXT`
      markings exactly, all of these are public here: the *only* way to
      obtain a `Model` in this repository right now is hand-building one.
      **No `CNA.XnaCompat` mirror this pass** — deliberate, documented scope
      cut: with no `ContentManager.Load<Model>` to produce a `Model` any
      other way, a compat mirror's practical value is close to zero right
      now, and it would roughly double this pass's surface (four more
      wrapped/extended collection types, three more wrapped interfaces).
      Follow-up once either `ContentManager.Load<Model>` exists or a real
      caller needs `Microsoft.Xna.Framework.Graphics.Model` specifically
      (plain `var`-typed consumption of `CNA.Graphics.Model` already works
      today, same as `EffectTechnique`/`DirectionalLight`'s existing compat
      gap). Verified: `dotnet build` clean across all 6 projects; `dotnet
      test`: 218/218 passing (up from 189 — 28 new tests total across the
      feature and its two review passes, three of which caught real bugs
      fixed after the fact — see `NEXT.md`). `samples/HelloGame`
      re-verified unaffected.
- [x] **`Song`/`MediaPlayer`/`MediaState` — done, scoped to real XNA's
      actual most-used surface, 2026-08-16 (session 6 continued yet
      further still again once more still further again).** No ABI shape
      for media/music playback exists in the analysis docs (confirmed by
      grep, same as audio), but grounded the same way `SoundEffect`/
      `BasicEffect` were: the real `openeggbert/cna` C++ engine already has
      a working (if not yet C-ABI-exposed) `MediaPlayer` implementation
      over SDL3_mixer (`modules/media/`), and this project's six new
      `cna_mediaplayer_*` native functions are shaped to match its actual
      `Play`/`Pause`/`Resume`/`Stop`/`Volume`/`IsMuted` semantics. `Song`
      construction turned out to be **another zero-native-ABI escape
      hatch**: the real C++ constructor is pure managed logic (a file-
      existence check, nothing else) — reproduced in C# the same way, so
      `Song` is real and testable today against real temporary files, a
      rarity among this session's native-backed types. Also found and
      reproduced (not the inaccurate doc, the actual verified behavior) a
      real doc/code mismatch in the upstream C++ header: its own doc
      comment claims an empty `name` "defaults to the file name," but the
      constructor body just stores whatever was passed, even empty.
      **Deliberately scoped down** from the C++ engine's much larger
      surface: no `Album`/`Artist`/`Genre`/`MediaLibrary` scanning
      subsystem, no visualization data — what real XNA games
      overwhelmingly actually use (`MediaPlayer.Play(song)`, `Volume`,
      `IsMuted`, checking `State`) is what's implemented (`MediaQueue`
      followed in a later pass this same session — see below).
      `State`/`Volume`/`IsMuted`/`PlayPosition` are
      plain C# static state (not native queries), matching the real C++
      engine's own architecture exactly — its own position timer uses
      `std::chrono`, a language facility, not an ABI call, so this project
      uses `System.Diagnostics.Stopwatch` the same way. `Song.FromUri`
      uses `System.Uri` for path resolution rather than porting the real
      C++ engine's own hand-rolled percent-decoding/scheme/UNC-path parser
      — the .NET BCL already solves exactly that problem (design invariant
      #7), so reproducing the manual parser would just be duplicating it
      with more room for bugs. Full `CNA.XnaCompat` mirror this time
      (unlike `Model`): `Song` has no construction blocker the way `Model`
      did, so `Microsoft.Xna.Framework.Media.Song` extends
      `CNA.Media.Song` directly (not sealed there, specifically so the
      compat type — sealed, matching real XNA — can extend it), and
      `Microsoft.Xna.Framework.Media.MediaPlayer` is a thin forwarding
      static class, same shape as this compat layer's existing `Mouse`/
      `Keyboard`. Verified: `dotnet build` clean across all 6 projects;
      `dotnet test`: 242/242 passing (up from 218 — 24 new tests total
      across the feature and its own review pass, one of which caught a
      real bug also present in the upstream C++ engine — see `NEXT.md`).
      `samples/HelloGame` re-verified unaffected.
- [x] **`MediaQueue`/`SongCollection` (multi-song playlists,
      `MoveNext`/`MovePrevious`, shuffle, repeat-driven auto-advance) and
      the `ActiveSongChanged`/`MediaStateChanged` events — done, 2026-08-16
      (session 6 continued autonomously past the original "Phase 4/5
      complete" checkpoint).** Closes the one deferral from the
      `Song`/`MediaPlayer` entry above that turned out to be readily
      tractable once actually scoped: `MediaQueue`/`SongCollection`'s real
      C++ shapes (`modules/media/`) are simple (indexer, `Count`, an
      `ActiveSong`/`ActiveSongIndex` pair defaulting to `-1`/null, no
      surprises), and `NextSong`'s repeat-wraparound/shuffle/clamped-
      direction algorithm was already fully read while researching the
      original `MediaPlayer` pass, so no new research was needed, only
      implementation. `Update()` (`CNAEXT`, public here since real XNA
      drives the equivalent through `FrameworkDispatcher.Update()`, which
      this project doesn't implement) is now wired into `CNA.Game`'s own
      base `Update(GameTime)`, so any game calling `base.Update(gameTime)`
      (standard XNA practice) gets song-end detection/auto-advance for
      free — the closest equivalent to real XNA's automatic per-frame
      behavior available without a real `FrameworkDispatcher`.
      `ActiveSongChanged`/`MediaStateChanged` are raised synchronously
      (this project has no per-frame dispatcher to defer through, unlike
      the real C++ engine's own flag-then-dispatcher-raises-later
      mechanism). One real asymmetry reproduced faithfully rather than
      "fixed": `Play(Song)` plays the caller's original `Song` object
      directly, while `Play(SongCollection, index)` plays the queue's own
      defensive copy — a genuinely ambiguous design choice in the real
      C++ engine (which object should own the resulting `PlayCount`
      increment), not a knowably-wrong one like `Model.Draw`'s bone-index
      fallback was, so it wasn't second-guessed. **No `CNA.XnaCompat`
      mirror for `Queue`/`Play(SongCollection)` — a real, structural
      blocker, not a scope cut of convenience:** `LoadSong`'s defensive
      copy always constructs the base `CNA.Media.Song` type regardless of
      the original `Song`'s actual runtime type, and `MediaPlayer` being a
      `static` class means (unlike every other compat type this session
      built) there is no subclassing seam to override that -- a compat
      `Queue` property would return songs that fail an explicit
      compat-typed downcast. `IsShuffled`/`MoveNext`/`MovePrevious`/both
      events have no such problem (no `Song`-typed data crosses their own
      boundary) and got the full compat mirror. Verified: `dotnet build`
      clean across all 6 projects; `dotnet test`: 264/264 passing (up from
      242 — 22 new tests, all passing on first run, most exercising real
      behavior against isolated `MediaQueue` instances or real temp files
      rather than argument validation only — see `NEXT.md` for the
      test-isolation design this needed, since `MediaPlayer`'s shared
      static state makes "which tests are safe to write" a real
      constraint). `samples/HelloGame` re-verified unaffected.
- [ ] **Deliberately deferred follow-ups, not gaps in what's above:**
      `Model` has no file-format loader (parsing a real model format is a
      separate, much larger problem — see `Model`'s own doc comment); the
      real C++ engine's `Album`/`Artist`/`Genre`/`MediaLibrary` scanning
      subsystem (no real XNA game needs this for basic playback, and it
      needs a real on-disk-scan/tag-parsing implementation this project
      has no equivalent for); visualization data (`GetVisualizationData`,
      real-time FFT). None of these are blocked on the native C ABI the
      way everything else in this phase is — they're scoped out because
      each is its own substantial, separable feature, not because
      anything is missing upstream to ground them against.
- [ ] Build the compatibility matrix (§73) from real tests, not from this
      list.

### Phase 5 — Performance passes

- [x] **`SpriteBatch` command buffering + `cna_sprite_batch_draw_many` (§22)
      — done, 2026-08-16 (session 6 continued yet further still again once
      more still further again once more still).** Every `Draw`/
      `DrawString` call now buffers a `CnaSpriteDrawCommand` in managed
      code instead of calling native immediately (`cna_sprite_batch_draw`/
      `cna_sprite_batch_draw_ex`, the old single-draw natives, were
      removed entirely rather than kept alongside the batched form — once
      every draw funnels through one buffer, nothing calls them anymore);
      `End()` flushes the whole batch through one new
      `cna_sprite_batch_draw_many` call — this is the one place in this
      project's ABI surface where `analysis_binding.md` §22 already
      specified the exact batched struct/function shape, not just a
      naming convention to extrapolate from. Also added real `Begin`/`End`
      pairing validation `SpriteBatch` never had before (nothing to
      validate when every `Draw` went straight to native): calling `Draw`
      before `Begin`, `End` before `Begin`, or `Begin` twice without an
      intervening `End` now throw `InvalidOperationException`, matching
      real XNA/MonoGame's own behavior (message text recalled from
      memory, not independently verified). Still not independently
      testable despite being pure managed logic: `SpriteBatch` has no
      raw-handle-wrapping constructor the way `GraphicsDevice`/`Texture2D`
      do (never needed one for a real production reason), so constructing
      *any* instance to exercise this new logic on still requires a real
      `cna-native` — noted rather than adding a test-only constructor with
      no other justification. Verified: `dotnet build` clean across all 6
      projects (0 warnings after fixing one ambiguous-`cref` doc-comment
      warning); `dotnet test`: 242/242 passing, unchanged (no new tests
      possible, see above); `samples/HelloGame` re-verified unaffected.
- [x] **Buffer-based bulk transfer for `Texture2D.SetData` / vertex/index
      data (`analysis_binding_sharp_runtime.md` §40) — already done,
      confirmed 2026-08-16 while scoping the rest of this phase, not new
      work.** §40 asks for exactly one shape: explicit `(pointer,
      byte_length)` native signatures for bulk binary data (texture
      pixels, vertex/index data, audio samples), never a Sharp Runtime
      array/span or `std::vector` crossing the ABI. Checked every
      bulk-data-crossing native call already in this project against that
      bar: `cna_texture2d_set_data`, `cna_vertexbuffer_set_data`/
      `get_data`, `cna_indexbuffer_set_data`/`get_data`, and
      `cna_soundeffect_create` all already take exactly `(handle, void*
      data, size_t byteLength)` (or the C# `fixed`-pointer equivalent),
      built this way from when each type was first added, not
      retrofitted just now. Nothing in this codebase passes a managed
      collection type across the ABI anywhere. This bullet was tracking a
      principle the rest of this session's own work had already been
      quietly honoring throughout — worth recording as done explicitly
      rather than leaving it looking like outstanding work.
- [ ] **`EffectParameter` handle caching (§27) — not applicable, not
      deferred.** §27 is specifically about caching the native identity
      behind a *name-based* lookup (`effect.Parameters["World"].SetValue(...)`)
      so the name string isn't re-marshalled every frame. This project
      has no `EffectParameter`/`Parameters["Name"]` collection at all —
      `BasicEffect`'s fixed C# property surface *is* its parameter
      interface (see that type's and `EffectPass`'s own doc comments:
      "this project's stock effects only ever have exactly one pass",
      no general multi-pass/name-indexed parameter system). There is
      nothing here for this optimization to apply to; it isn't a gap,
      it's a premise this project's `BasicEffect`-only design doesn't
      share. Revisit only if a future custom/name-indexed effect system
      is ever added.

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
all 6 projects, and all unit tests pass (`dotnet test`) — 44 as of the
initial scaffold, 112 as of 2026-08-16 (session 5); the count only grows,
see `NEXT.md`'s per-session entries for the history and `README.md` for the
current number, since this note isn't kept in sync with every session. Also
verified each time: `dotnet run --project samples/HelloGame` fails at
exactly the documented point — a `DllNotFoundException` for `cna-native`
raised from inside `Game`'s constructor — rather than from any code defect.
That confirms the managed callback bridge, the covariant-return
`CreateGraphicsDevice`/`CreateContentManager` factories, the
`Matrix.Invert`/`BoundingFrustum` math, and the value-type implicit
conversions are all wired correctly end to end, ahead of the native ABI
existing. Re-run both commands after cloning if you
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
