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
constructors also now done. `Album`/`Artist`/`Genre`/`Playlist`/`MediaLibrary`'s
real XNA object model is also now done, deliberately scoped to always-empty
collections (the real C++ engine's actual scanning logic depends on
FFmpeg/native tag-parsing infrastructure with no equivalent on either side
of this binding -- see `NEXT.md` for the detail). The picture-library
surface (`Picture`/`PictureAlbum`/`PictureCollection`/`PictureAlbumCollection`,
`GetPictureFromToken`/`SavePicture`) is also now done, genuinely real (not
scoped to always-empty) since its write path needs only plain file I/O --
see `NEXT.md` for the detail, including why its `CNA.XnaCompat` mirror
ended up an independent reimplementation rather than a subclass. `Model`
file-loading is also now done for the well-grounded subset this pass
scoped it down to: real, uncompressed `.xnb` binary assets (`CNA.Content.Xnb`),
confirmed byte-for-byte against the real openeggbert/cna C++ engine's own
reference implementation and a real MonoGame-compiled fixture -- LZX/LZ4
compression and the real engine's own `.cnj`/glTF content paths are all
deliberately out of scope (see `NEXT.md` for why -- each is its own large,
separable feature, and the real engine hasn't even wired its own
`ModelReader` into `ContentManager::Load<Model>()` yet either). `Model`'s
`CNA.XnaCompat` mirror (`Model`/`ModelBone`/`ModelBoneCollection`/
`ModelMesh`/`ModelMeshCollection`) is also now done. `ContentManager.Load<Model>()`
on the compat `ContentManager` now also returns a real, compat-typed
`Model` (`XnbCompatModelBuilder`, reusing the base class's own
`.xnb`-parsing directly rather than duplicating it). `ModelMeshPart`/
`ModelMeshPartCollection` are now compat-typed too, closing most of the
mirror's own original "deliberately deferred" gap -- but a closer look
showed `ModelEffectCollection`/`ModelMesh.Effects` genuinely can't be
made safe the same way (constructed at field-initializer time inside the
base `ModelMesh`, with no override seam at all, unlike everything else
in this feature), so that one specific piece stays a real, documented,
permanent gap, not a temporary scope cut -- see `NEXT.md` for the full
design reasoning on all of this. `MediaPlayer.GetVisualizationData`/
`IsVisualizationEnabled`/`VisualizationData` are also now done -- this
one turned out to be the best-case scoping outcome of the whole session:
the real C++ engine has a genuinely real, working, dependency-free FFT
implementation for it (a from-scratch 512-point radix-2 FFT over a
lock-free ring buffer fed from SDL3_mixer), not blocked or partial the
way most of this session's deferred features turned out to be. `Model`
file-loading now also covers a real, minimal-scope subset of the real
engine's own `.cnj` JSON format (`CNA.Content.Cnj`) -- JSON envelope plus
flat mesh list, `BasicEffect` only, vertex sidecar strides 16/20/24/32
only -- on the base `CNA.Framework.Content.ContentManager` (`.xnb` still
always wins first if both exist for the same asset name, matching the
real engine's own dispatch order); the `.cnj` document's own bone
hierarchy/skinning/PBR/morph-target surface and runtime glTF both remain
explicitly out of scope. The `.cnj` path's own `CNA.XnaCompat` mirror
(`CnjCompatModelBuilder`) is also now done, closing that deferred
follow-up -- same "reuse the shared native-free parsing step, reimplement
only the thin native-backed compat-typed assembly around it" pattern
`XnbCompatModelBuilder` already established for the `.xnb` side (see
`NEXT.md` for the detail, including the load-bearing finding that
`.cnj`'s `BasicEffect` JSON has no material-color fields at all, unlike
`.xnb`'s). Real, LZX-compressed `.xnb` `Model` loading (`LzxDecoder`/
`XnbLzxDecompression`) is also now done -- a direct, line-by-line C#
port of the real openeggbert/cna C++ engine's own `LzxDecoder`, confirmed
byte-for-byte against two real MonoGame fixtures and an independently
FNA-produced reference decompressed output, closing most of the `.xnb`
loading feature's own original LZX/LZ4 deferral (`Lz4` -- a MonoGame-only
extension with no local format grounding -- stays out of scope). `.cnj`'s
own real `"bones"` rigid scene-graph hierarchy (cnjVersion 2) is also now
supported on the base `ContentManager` -- research confirmed this is
architecturally independent of skinning in the real format (a flat,
parent-index-encoded array, closely analogous to `.xnb`'s own bone
convention already ported), so it was carved out and built as its own
increment; `.cnj`'s skinning surface (vertex strides 48/52/56/68,
`"skeleton"`/`"animations"`) stays explicitly out of scope, confirmed to
have no real payoff without a `SkinnedEffect` type, which doesn't exist
anywhere in this project. `CnjCompatModelBuilder` (the `.cnj` path's
`CNA.XnaCompat` mirror) now also links a document's own real bone
hierarchy, closing that deferred follow-up -- previously it explicitly
rejected such a document rather than silently producing a wrong bone
structure.

**Scope change, 2026-08-18 (supersedes every "out of scope" call above):**
the user directed that `cna-cs` must cover **the complete XNA 4.0 API**, not
a subset chosen by whatever the current sample happened to need. Phase 8
below is now the primary outstanding work; the deferrals recorded in the
paragraph above (`SkinnedEffect`, `.cnj` skinning, name-indexed effect
parameters, and the rest) are **no longer deferrals** -- they are Phase 8
line items. Phase 6 packaging/cross-platform validation still remains too.
**Date:** 2026-08-18 (see `NEXT.md` for the session-by-session history and
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

## Scope mandate: the complete XNA 4.0 API

**Directive (user, 2026-08-18): `cna-cs` must cover the whole XNA 4.0 API
surface, not a narrow subset of it.**

Up to and including 2026-08-17 this repository was built "vertical slice"
style: a type was added when a sample or test actually needed it, and
anything else was recorded as a documented, deliberate scope cut. That
produced a solid but incomplete layer -- a concrete inventory taken
2026-08-18 measured **94 of 201** public XNA 4.0 types present in
`CNA.XnaCompat`, i.e. 47%. That inventory is the baseline Phase 8 works
against; re-run it (see `Phase 8` below) rather than trusting this number
once work starts landing.

This mandate changes three standing rules:

1. **"No caller needs it yet" is no longer a valid reason to omit a type.**
   Real XNA 4.0 shipped it, so this project implements it. Coverage is
   driven by the XNA 4.0 API reference, not by `samples/`.
2. **A documented scope cut is no longer a terminal state.** Every
   "deliberately out of scope" note already written in this file and in
   `NEXT.md` is now a Phase 8 backlog item. Those notes stay valuable as
   *why it was hard*, not as *why it will never exist*.
3. **Fidelity still beats speed.** This mandate is about breadth, not about
   lowering the bar. Every rule in "Design invariants" still holds, every
   type is still grounded in the real, shipped `openeggbert/cna` C ABI
   headers (never a guessed shape), and the existing build-clean +
   tests-pass + `/code-review high` discipline still applies to each
   increment. A wide layer of wrong bindings would be worse than the
   narrow, correct one this replaces.

What genuinely cannot be honored, and why that is a small list: a
2026-08-18 survey of the real C ABI found upstream headers for essentially
every remaining area -- `curve.h`, `input_touch.h`, `storage.h`,
`runtime_components.h`, `texture_volume.h`, `texture.h`, `graphics_state.h`,
`effects.h`, `video.h`, `xact.h`, `display.h`, `net.h`/`net_gamers.h`/
`gamer_services.h`. Full coverage is therefore mostly *binding work against
an ABI that already exists*, not inventing stubs for a nonexistent engine.
Where a specific member still has no native counterpart, implement the type
with real XNA signatures and make the unbacked member throw
`NotSupportedException` with a message naming the missing native function --
never silently no-op, and never fake a plausible return value.

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
- [x] **`Album`/`Artist`/`Genre`/`Playlist`/`MediaLibrary`/`MediaSource` and
      their collections — done, scoped to an always-empty object model,
      2026-08-17 (session 6 continued autonomously past the original
      "Phase 4/5 complete" checkpoint, per explicit user request to keep
      going, then an explicit scoping decision after research revealed
      the real feature's actual dependency shape).** Reading the real C++
      engine's own `MediaLibrary::BuildFromRoots` showed its scanning
      logic depends on infrastructure with no equivalent anywhere in this
      binding and no C ABI exposure to build against: real ID3v2/Vorbis/FLAC
      tag parsing, FFmpeg-based audio duration probing
      (`avformat_find_stream_info`), a native directory-scanning index,
      and a native cover-art image loader. Unlike `BasicEffect`/`Model`/
      `Song` (each "shaped to match a real implementation, just needs
      porting"), this is not portable at any reasonable scope -- it would
      need either a large new native ABI surface upstream (itself needing
      FFmpeg-equivalent decoding exposed through a C API) or reimplementing
      binary audio-tag/container parsing from scratch in pure C#.
      Implemented instead: the real XNA public API surface in full (every
      type, every property, `MediaLibrary`'s own real constructor
      validation) but every collection is always empty, since nothing
      ever scans anything -- an honest, documented scope decision (see
      `MediaLibrary`'s own doc comment), not a silent stub. `Album`/
      `Artist`/`Genre`/`Playlist`'s constructors stay `MediaLibrary`-only
      (matching real XNA's own choice, unlike `Song`'s `CNAEXT` public
      one) since they only make sense as part of a coherent scan this
      project can't perform. Full `CNA.XnaCompat` mirror, safe in a way
      `MediaPlayer.Queue` wasn't: since every collection is *provably*
      always empty, a `new`-shadowed compat-typed collection can never
      diverge from the base one there's no real data to disagree on.
      Verified: `dotnet build` clean across all 6 projects; `dotnet test`:
      311/311 passing (up from 280 — 31 new tests, all passing on first
      run, exercising real construction/validation/equality behavior with
      no native dependency at all). `samples/HelloGame` re-verified
      unaffected.
- [x] **The real C++ engine's picture-library surface (`Picture`/
      `PictureAlbum`/`PictureCollection`/`PictureAlbumCollection`,
      `GetPictureFromToken`/`SavePicture`) — done, 2026-08-17 (session 6
      continued autonomously past the `MediaLibrary` checkpoint, per
      explicit user request to start this subsystem next).** Unlike the
      music side above, this is genuinely real, not scoped to
      always-empty: reading `MediaLibrary::SavePicture`'s own C++ source
      confirmed *saving* a picture needs nothing beyond plain file I/O
      (`SavedPictureStore`, a faithful port including its security-relevant
      path-traversal filename sanitization) — the only infrastructure-bound
      sub-pieces (real image-dimension detection, real thumbnail
      generation) already have real, working fallback paths in the
      upstream engine itself (`width=0,height=0` on decode failure;
      full-size image on thumbnail-generation failure), taken
      unconditionally here rather than invented. `RootPictureAlbum` starts
      as a single empty root node (no pre-existing-photo scan, same reason
      as the music side) rather than null, matching real XNA's own
      "always a valid album" contract. **`CNA.XnaCompat` mirror needed a
      genuinely different design from every other feature this session:**
      a covariant-return factory-hook design (the same pattern
      `Game.CreateGraphicsDevice` uses) was tried first and does not fit,
      because `PictureCollection`/`PictureAlbumCollection` are — like
      `SongCollection`/`AlbumCollection` — independent reimplementations of
      their `CNA.Media` counterparts, not subclasses, and a covariant
      override requires the override's return type to actually be a
      subtype of the base's declared return type. With no safe downcast
      available for the collections, `Picture`/`PictureAlbum` became
      independent reimplementations too, and `CNA.XnaCompat`'s own
      `MediaLibrary` maintains fully independent, compat-typed picture
      state built directly on `CNA.Media.SavedPictureStore` (the shared
      low-level, security-sensitive helper — reused, not duplicated)
      instead of on the base class's own bookkeeping — see `NEXT.md` for
      the full reasoning trail, including the dead end. **Critical safety
      finding, load-bearing for all testing in this area:**
      `Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)`
      resolves to the real current user's actual Pictures folder in this
      environment — confirmed, not assumed — so no test anywhere calls
      `SavePicture` with real data; `SavedPictureStore` (the actual
      file-writing logic) is tested directly against a throwaway temp
      directory instead. Verified: `dotnet build` clean across all 6
      projects, 0 warnings; `dotnet test`: 359/359 passing (up from 315 —
      44 new tests, including a follow-up `/code-review high` pass that
      fixed a stream-draining-order bug and added a missing `IsDisposed`
      guard to `SavePicture` — see `NEXT.md`). `samples/HelloGame`
      re-verified unaffected.
- [x] **`Model` file-loading via real, uncompressed `.xnb` binary assets
      (`CNA.Content.Xnb`, `ContentManager.Load<Model>()`) — done, 2026-08-17
      (session 6 continued autonomously past the picture-library checkpoint,
      per explicit user selection of "Start Model file-loading," then an
      explicit scope decision — "attempt full scope anyway" — once research
      revealed the true dependency shape: three separate content formats,
      not one).** Research first (this session's own standing discipline)
      found the real openeggbert/cna C++ engine supports `Model` content
      three different ways — real XNA's own `.xnb` binary format
      (`modules/content/src/Xnb/`), a custom `.cnj` JSON format, and a
      runtime glTF importer (`modules/content/src/GltfImport/`, ~2,500
      lines, skeletal animation/morph targets/PBR materials, native
      `cgltf`-dependent) — `ContentManager.cpp` alone is 3,227 lines. Only
      the real-XNA `.xnb` path is in scope for this pass: it's the one
      path that's genuinely about *XNA source compatibility* (goal #1),
      it's pure C#/BCL logic with **zero** native ABI dependency (unlike
      `Texture2D`/`SoundEffect`/`SpriteFont`, which this project's native
      engine loads on their behalf), and — confirmed by a dedicated
      research pass reading the real reference implementation in full and
      hand-tracing a real, uncompressed, MonoGame-compiled `Model` fixture
      byte-for-byte — it's genuinely tractable at this scope. LZX/LZ4
      decompression and the `.cnj`/glTF paths are explicitly deferred (see
      `NEXT.md` for the full reasoning); real, *uncompressed* `.xnb` files
      are the only ones this reader accepts, rejecting compressed ones
      with a clear, documented exception rather than attempting either
      decompressor. **Split into two layers, mirroring
      `ContentManager.LoadSpriteFontData`'s existing "return raw pieces,
      let the caller build the native-backed object" pattern:** parsing
      the `.xnb` bytes into an intermediate `XnbModelData` tree
      (`CNA.Content.Xnb`) is pure C#, no native call anywhere, and fully
      unit-testable — confirmed against a real fixture, not just
      hand-constructed bytes; building the final, real `Model` from that
      tree (`XnbModelBuilder`) needs a real, native-backed `GraphicsDevice`
      to construct `VertexBuffer`/`IndexBuffer` instances, so *that* part
      is native-ABI-blocked, the same situation as this project's other
      content types. `System.IO.BinaryReader.Read7BitEncodedInt()`/
      `.ReadString()` are used directly for the format's own 7-bit-encoded
      ints/length-prefixed strings — confirmed byte-for-byte identical to
      the real format, not just "close enough" (design invariant #7).
      `ContentManager.GraphicsDevice` is a new settable property, wired by
      `Game.EnsureGraphicsDevice()` once its own device becomes available.
      Verified: `dotnet build` clean across all 6 projects, 0 warnings;
      `dotnet test`: 380/380 passing (up from 359 — 21 new tests,
      including a full end-to-end parse of a real MonoGame-compiled
      `Model` `.xnb` fixture, vendored into `tests/CNA.Framework.Tests/assets/`
      from `openeggbert/cna`'s own MIT-licensed MonoGame test fixtures —
      see that directory's own `README.md`, and a follow-up `/code-review
      high` pass that fixed a real "loaded models render nothing" bug
      plus four corrupt-input safety gaps — see `NEXT.md`).
      `samples/HelloGame` re-verified unaffected.
- [x] **`Model`'s `CNA.XnaCompat` mirror (`Model`/`ModelBone`/
      `ModelBoneCollection`/`ModelMesh`/`ModelMeshCollection`) — done,
      2026-08-17 (session 6 continued autonomously past the `.xnb`
      loading checkpoint, per explicit user selection of "Build Model's
      CNA.XnaCompat mirror").** Real design work, not a small addition:
      `ModelBoneCollection`/`ModelMeshCollection` are independent
      reimplementations (same "extending directly would inherit the
      wrong element type" reasoning as `SongCollection`), `ModelBone`'s
      `Children` needed its own independently-tracked, `AddChild`-synced
      storage (the base class's own `Children` is built inside the base
      constructor with no override seam — the same wall the
      picture-library's `PictureAlbum` hit), and `Model`'s own
      `CopyAbsoluteBoneTransformsTo`/`CopyBoneTransformsFrom`/
      `CopyBoneTransformsTo` needed real element-wise-converting overloads
      despite taking "just a `Matrix`" — their parameter is a `Matrix[]`,
      and C#'s implicit conversion operators (which handle every other
      inherited-unchanged scalar member in this compat layer for free)
      don't apply across arrays. **Deliberately scoped down, matching
      `BasicEffect.CurrentTechnique`/`DirectionalLight0-2`'s own precedent
      exactly:** `ModelMeshPart`/`ModelMeshPartCollection`/
      `ModelEffectCollection` stay base-typed -- `ModelMeshPart`'s own
      `VertexBuffer`/`IndexBuffer`/`Effect` can already legitimately
      *hold* compat-typed instances (compat `VertexBuffer`/`IndexBuffer`/
      `BasicEffect` all subclass their base counterparts directly), so
      ordinary `var`-typed/`foreach` consumption already works; only an
      explicit `Microsoft.Xna.Framework.Graphics.ModelMeshPart` type
      declaration would fail to compile — a real, narrow, documented gap,
      not an oversight. `ContentManager.Load<Model>()` on the compat
      `ContentManager` also does **not** yet return a compat-typed
      `Model` (would need its own `XnbModelBuilder`-equivalent, reusing
      `CNA.Content.Xnb`'s shared parsing layer the same way the
      picture-library's compat `MediaLibrary` reuses
      `SavedPictureStore`) — a separate, deliberately deferred follow-up,
      not attempted in this pass. Verified: `dotnet build` clean across
      all 6 projects, 0 warnings; `dotnet test`: 391/391 passing (up from
      380 — 11 new tests, all for `ModelBone`/`ModelBoneCollection`, the
      only new compat type reachable from `CNA.XnaCompat.Tests` without a
      real `cna-native` — `Model`/`ModelMesh` both need a `GraphicsDevice`,
      unreachable there, same pre-existing limitation already documented
      on compat `VertexBuffer`/`IndexBuffer`/`BasicEffect`).
      `samples/HelloGame` re-verified unaffected.
- [x] **`ContentManager.Load<Model>()` on the compat `ContentManager` —
      done, 2026-08-17 (session 6 continued autonomously past the compat
      mirror checkpoint, per explicit user selection of "Wire compat
      ContentManager.Load<Model>()").** Closes the one deferral flagged
      in the entry above. Split `CNA.Content.ContentManager.LoadModel`
      into `LoadXnbModelData` (pure `.xnb` parsing, unchanged behavior)
      and `LoadModel` (parsing + base-typed assembly), so
      `CNA.XnaCompat.ContentManager` can reuse the parsing step directly
      without duplicating any `.xnb` format logic, then hand the result
      to a new `XnbCompatModelBuilder` (this compat layer's own
      counterpart to `XnbModelBuilder`, reusing its now-`internal`
      `BuildVertexBuffer`/`BuildIndexBuffer`/`BuildBasicEffect` directly
      rather than duplicating them, since `ModelMeshPart` stays
      base-typed regardless of which builder constructs it). One real
      compiler-caught correction along the way: `LoadXnbModelData` first
      tried `protected` (matching every other load-helper), which failed
      with `CS0050` since its `internal`-typed return value isn't visible
      to a hypothetical third-party subclass of the *public*
      `ContentManager` outside this project's own `InternalsVisibleTo`
      grant — fixed by using `internal` instead (matching
      `SavedPictureStore`'s own accessibility). Verified: `dotnet build`
      clean across all 6 projects, 0 warnings; `dotnet test`: 391/391
      passing, unchanged — no new testable surface (constructing a
      working `ContentManager` at all needs a real `cna-native`, the
      same pre-existing limitation `XnbModelBuilder` itself already has).
      `samples/HelloGame` re-verified unaffected.
- [x] **`ModelMeshPart`/`ModelMeshPartCollection` compat mirror — done,
      2026-08-17 (session 6 continued autonomously past the compat
      `Load<Model>()` checkpoint, per explicit user selection of "Mirror
      ModelMeshPart and its collections").** Turned out simpler than
      expected for `ModelMeshPart` itself (a fully trivial subclass --
      this compat layer has no separate compat `Effect` hierarchy at
      all, so `Effect`'s declared type never differs, and compat
      `VertexBuffer`/`IndexBuffer` already subclass their base
      counterparts, so `SetVertexBuffer`/`SetIndexBuffer`'s inherited,
      base-typed parameters already accept a compat-typed argument via
      ordinary upcasting), but harder than expected for `ModelEffectCollection`:
      a closer look showed `ModelMesh.Effects` is constructed at
      *field-initializer* time inside the base `ModelMesh`
      (`public ModelEffectCollection Effects { get; } = new();`), with no
      override seam at all -- unlike everything else in this feature,
      there is no point after construction where a subclass could ever
      substitute a compat-typed collection. Making it safe would need
      `ModelMeshPart.Effect`'s setter overridden with its own
      parallel-tracking logic *and* `ModelMesh` maintaining a second,
      independent `Effects` collection kept in sync with it -- closer in
      shape to why `MediaPlayer.Queue` stays a documented non-mirror than
      to anything else in this feature, and not attempted here.
      `ModelMesh.MeshParts` is now compat-typed (the same "single
      construction seam" pattern `Model.Bones`/`.Meshes` already use);
      `ModelMesh.Effects`/`ModelEffectCollection` remain a real,
      permanent, documented gap, not a temporary scope cut.
      `XnbCompatModelBuilder` needed real rework (not just a signature
      update) since it can no longer reuse `XnbModelBuilder`'s own
      buffer/effect builders now that a compat `ModelMeshPart` expects
      compat-typed buffers -- reverted those back to `private` and gave
      `XnbCompatModelBuilder` its own, including a new base-to-compat
      `VertexDeclaration` converter. Verified: `dotnet build` clean
      across all 6 projects, 0 warnings; `dotnet test`: 401/401 passing
      (up from 391 — 10 new tests, all for compat `ModelMeshPart`, the
      only new type in this pass reachable from `CNA.XnaCompat.Tests`
      without a real `cna-native`). `samples/HelloGame` re-verified
      unaffected.
- [x] **`MediaPlayer.GetVisualizationData`/`IsVisualizationEnabled`/
      `VisualizationData` — done, 2026-08-17 (session 6 continued
      autonomously past the `ModelMeshPart` checkpoint, per explicit user
      selection of "Start MediaPlayer visualization data").** Research
      first (this session's own standing discipline) found the best-case
      outcome of the whole session: a real, working, dependency-free
      implementation already exists in the real C++ engine
      (`modules/media/src/Internal/VisualizationCapture.cpp`/
      `VisualizationFFT.cpp` -- a lock-free ring buffer fed from
      SDL3_mixer's post-mix callback, and a from-scratch 512-point
      radix-2 FFT the real engine's own authors deliberately built rather
      than pulling in a DSP dependency for 256 bins), all corresponding
      tickets checked complete in the real engine's own `plan_media.md`.
      Since the DSP work lives entirely in native code, this followed the
      same "build against the ABI shape a real implementation already
      has" methodology as `Play`/`Pause`/`Resume`/`Stop`: two new
      `cna_mediaplayer_*` native functions, one to toggle the real
      post-mix callback (`set_visualization_enabled`), one to read the
      FFT result (`get_visualization_data`, plain raw `float*` pointers
      matching `cna_vertexbuffer_set_data`/`get_data`'s own "explicit
      buffer" convention). `IsVisualizationEnabled` needed a real native
      call on write either way (unlike `Volume`/`IsMuted`, it installs/
      removes a real callback), but still caches the value in C# for
      reads, matching `Volume`/`IsMuted`'s own shape. `VisualizationData`'s
      own `CNA.XnaCompat` mirror is fully trivial -- `Frequencies`/
      `Samples` are plain `float[]` referencing no other `CNA` type at
      all, so the compat type is an empty subclass and
      `MediaPlayer.GetVisualizationData` forwards with zero conversion.
      Verified: `dotnet build` clean across all 6 projects, 0 warnings;
      `dotnet test`: 413/413 passing (up from 401 — 12 new tests,
      including full, native-free coverage of `VisualizationData`'s own
      construction in both projects, a rarity for anything
      `MediaPlayer`-adjacent). `samples/HelloGame` re-verified
      unaffected.
- [x] **`Model` file-loading via a real, minimal-scope subset of the real
      engine's own `.cnj` JSON format (`CNA.Content.Cnj`,
      `ContentManager.Load<Model>()`) — done, 2026-08-17 (session 6
      continued autonomously past the visualization-data checkpoint, per
      explicit user selection of "Attempt Model's .cnj/glTF content
      paths," then two further explicit continuations narrowing and then
      resolving the format's own undocumented vertex-layout convention,
      then "Attempt a minimal .cnj Model reader").** Research (three
      sequential, increasingly-targeted passes) ruled runtime glTF out
      entirely (hard-blocked on the vendored `cgltf` C library, no
      algorithm-only slice avoids it) and found `.cnj` genuinely
      tractable at a deliberately narrowed scope: JSON envelope + flat
      mesh list (no `"bones"` hierarchy, no `"skeleton"`/`"animations"`,
      no morph targets) + `BasicEffect` only + vertex sidecar strides
      16/20/24/32 only (48/52/56/68, the PBR/skinned shapes, excluded).
      The vertex sidecar's byte layout (no header, raw floats, stride
      JSON-authoritative only) was cross-verified against both the real
      reader and its writer, and confirmed field-for-field identical to
      this project's own existing `CNA.Graphics.VertexPosition*` structs
      -- so, like the `.xnb` path, sidecar bytes pass straight through to
      `VertexBuffer.SetData(byte[])` with zero marshaling. **A real,
      load-bearing finding that changed the implementation plan:**
      `.cnj`'s `BasicEffect` JSON has *no material-color fields at all*
      (no `diffuseColor`/`specularColor`/`alpha`/`specularPower`, unlike
      `.xnb`'s own `BasicEffectReader`) -- only `texture`/
      `vertexColorEnabled` are ever read, so `CnjModelBuilder`
      deliberately does *not* reuse `XnbModelBuilder.ApplyBasicEffectData`,
      which applies a field set this format simply doesn't have; it has
      its own, much smaller effect-application step. Also new,
      dedicated infrastructure: `CnjPathContainment`, a direct port of
      the real engine's own `PathContainment.hpp` component-wise (not
      string-prefix) containment check, since every sidecar path
      (`"vertices"`/`"indices"`/`"texture"`) a `.cnj` document names is
      untrusted, file-supplied input that must stay inside
      `ContentManager.RootDirectory` before it's ever opened -- a
      different shape from `SavedPictureStore.SanitizePictureName`'s
      existing bare-filename check, since a sidecar path legitimately
      contains subdirectories. Every deliberately out-of-scope input is
      rejected with a clear `ContentLoadException` naming the reason
      (unsupported `cnjVersion`, `"sourceFile"`/`"skeleton"`/
      `"morphTargets"` present, a multi-entry `"bones"` array, an
      unsupported vertex stride, a non-`BasicEffect` effect, a sidecar
      path escaping the content root) rather than silently mis-loading
      it, matching the `.xnb` path's own LZX/LZ4 precedent; two cases
      (an empty `"vertices"`/`"indices"` field, a non-positive
      `vertexStride`) are silently skipped, matching the real engine's
      own behavior exactly. `LoadModel` now tries `.xnb` first (matching
      the real engine's own dispatch order -- a real `.xnb` file always
      shadows a `.cnj` of the same asset name), falling back to `.cnj`
      only when no `.xnb` exists; runtime glTF stays fully out of scope.
      Real fixture: `quad.cnj`/`quad_verts.bin`/`quad_idx.bin`, byte-for-byte
      reproducing the real engine's own gtest fixture (`CnjModelTests.cpp`'s
      `WriteQuadModelFixture`), vendored into
      `tests/CNA.Framework.Tests/assets/cnj/` (regenerated from that
      test's own field values, not copied binary, since the source is
      C++ test code, not a binary asset -- see that directory's own
      `README.md`). This feature does *not* serve this project's own
      stated "XNA source compatibility" goal #1 the way `.xnb` loading
      does -- `.cnj` is CNA's own self-rolled format with no XNA
      equivalent -- the same goal-alignment caveat already surfaced to
      and accepted by explicit user choice when this work was started.
      `CnjModelReader` has zero native dependency, fully unit-testable
      (same rare "fully real, testable today" status `XnbModelReader`
      already has); `CnjModelBuilder` is native-ABI-blocked like every
      other final-assembly step in this project. The `CNA.XnaCompat`
      mirror of this path (`CnjCompatModelBuilder`) was deliberately
      deferred as its own, separate follow-up in this pass -- matching
      the `.xnb` path's own "narrow reader/builder split first, compat
      mirror as a distinct, separately-reviewed follow-up" cadence; see
      the entry below for that follow-up's own completion.
      Verified: `dotnet build` clean across all 6 projects, 0 warnings;
      `dotnet test`: 459/459 passing (up from 413 — 46 new tests, all
      for `CnjModelReader`/`CnjPathContainment`, reachable without a
      real `cna-native` the same way `XnbModelReaderTests` already is;
      includes two follow-up `/code-review high` passes that fixed a
      real vertex/index sidecar byte-overrun gap (a first attempt
      silently truncated instead of rejecting -- a second pass caught
      that as inconsistent with this reader's own "detect and throw"
      discipline, fixed to reject instead, matching `XnbIndexBufferReader`'s
      own precedent exactly; `XnbVertexBufferData`/`XnbIndexBufferData`'s
      own invariant is now also enforced structurally in their
      constructors, not just documented), a path-containment
      false-rejection bug, a malformed-`"bones"`-field gap, and an
      over-rejection of `"bones": null` the first fix introduced — see
      `NEXT.md`). `samples/HelloGame` re-verified unaffected.
- [x] **The `.cnj` path's own `CNA.XnaCompat` mirror
      (`CnjCompatModelBuilder`) — done, 2026-08-17 (session 6 continued
      autonomously past the `.cnj` reader's own review-cycle checkpoint,
      per explicit user selection of "Mirror .cnj in CNA.XnaCompat").**
      Closes the one deferral flagged in the entry above. Exactly
      `XnbCompatModelBuilder`'s own shape: reuses
      `ContentManager.LoadCnjModelData` (the shared, native-free parsing
      step) directly rather than re-parsing anything, builds
      compat-typed `Model`/`ModelBone`/`ModelMesh`/`ModelMeshPart`/
      `VertexBuffer`/`IndexBuffer`/`BasicEffect` throughout (so it has
      its own `VertexDeclaration` converter, the same reason
      `XnbCompatModelBuilder` has one), and reproduces
      `CnjModelBuilder`'s own "no bone hierarchy, synthesize one root
      bone plus one child bone per mesh" control flow rather than
      sharing it (the same near-duplication trade-off
      `XnbCompatModelBuilder`'s own doc comment already accepts for its
      relationship to `XnbModelBuilder`). Extracted
      `CnjModelBuilder.ApplyBasicEffectData` (mirroring
      `XnbModelBuilder.ApplyBasicEffectData`'s own extraction) so the
      compat builder reuses the base one's (trivial, one-line) effect
      field-assignment logic rather than duplicating it -- nothing about
      applying `VertexColorEnabled` is compat-specific, since compat
      `BasicEffect` subclasses the base one directly. `ContentManager.LoadCompatModel`
      now has the identical `.xnb`-then-`.cnj` dispatch order the base
      class's own `LoadModel` already has. Verified: `dotnet build`
      clean across all 6 projects, 0 warnings; `dotnet test`: 459/459
      passing, unchanged — no new testable surface (constructing a
      working compat `ContentManager`/`GraphicsDevice` at all needs a
      real `cna-native`, the same pre-existing limitation
      `XnbCompatModelBuilder`'s own compat wiring already has); includes
      a follow-up `/code-review high` pass that deduplicated
      `BuildVertexBuffer`/`BuildIndexBuffer`/`ToCompat` (byte-for-byte
      identical to `XnbCompatModelBuilder`'s own, since `.cnj`'s buffer
      data reuses the exact same format-agnostic types) into shared,
      `internal` members on `XnbCompatModelBuilder` — see `NEXT.md`.
      `samples/HelloGame` re-verified unaffected.
- [x] **Real, LZX-compressed `.xnb` `Model` loading (`LzxDecoder`/
      `XnbLzxDecompression`) — done, 2026-08-17 (session 6 continued
      autonomously past the `CnjCompatModelBuilder` checkpoint, per
      explicit user selection of "Attempt LZX/LZ4-compressed .xnb
      decompression").** Research first (this session's own standing
      discipline) found the best-grounded outcome of any deferred
      feature this session tackled: the real openeggbert/cna C++ engine
      has its own complete, working, already-tested `LzxDecoder`
      (`modules/content/src/Xnb/LzxDecoder.cpp`, 680 lines) -- itself a
      from-scratch C++ port of FNA's `LzxDecoder.cs` (a C# port of
      libmspack's `lzxd.c`), preserving FNA's own variable names and
      control flow specifically so it stays verifiable against the
      original. That C++ port is cross-verified against two real,
      vendored, Ms-PL-licensed MonoGame fixtures
      (`Explosion.xnb`/`FontCalibri14.xnb`) **and** an independently
      FNA-produced reference decompressed output (the exact bytes FNA's
      own unmodified decoder produces, run under Mono) -- confirmed
      SHA-256-identical. This C# port ported the C++ port back to C#
      (its natural home, since the C++ was itself ported from C#),
      preserving the same field names/control flow, and passed the
      identical byte-exact differential test against both real fixtures
      on the very first clean build -- no port bugs needed fixing.
      MonoGame's own `Lz4` `.xnb` extension (confirmed, by contrast, to
      be something original XNA/FNA never produced or read, with **zero**
      byte-level framing details grounded anywhere reachable) stays
      exactly as before: detected, rejected with a clear
      `ContentLoadException` -- the real C++ engine's own maintainers
      independently reached the identical "not now, no reference to
      ground it against" conclusion first. "Intel E8" call-address
      translation (a general LZX/CAB feature, essentially irrelevant to
      game asset payloads) is reproduced exactly as FNA's own original
      left it -- genuinely unfinished upstream (its own loop never
      advances the output position) -- rather than "completed," since
      that would diverge from, not match, the reference this port is
      grounded against; in practice this option is essentially never set
      for ordinary game-asset `.xnb` files. `XnbHeader` now accepts
      `Lzx` as a real, supported compression value (previously it
      rejected any non-`None` compression outright); `ContentManager.LoadXnbModelData`
      branches on it, decompressing before constructing the
      `XnbContentReader` the uncompressed path already used unchanged.
      `LzxDecoder`/`XnbLzxDecompression` have zero native dependency,
      fully unit-testable (same rare "fully real, testable today" status
      every other `.xnb`/`.cnj` reader in this project already has).
      Verified: `dotnet build` clean across all 6 projects, 0 warnings;
      `dotnet test`: 464/464 passing (up from 459 — 5 new tests,
      including the byte-exact differential check against both real
      fixtures' independently-produced reference output, and an
      integration check that a real compressed non-`Model` fixture
      decompresses correctly then fails cleanly on its unsupported root
      type reader, not a crash or a hang); includes a follow-up
      `/code-review high` pass that fixed a real unhandled-`EndOfStreamException`
      gap (a too-short-for-its-payload `.xnb` header previously crashed
      instead of throwing `ContentLoadException`) and a real
      discarded-error-return-value gap in `LzxDecoder.MakeDecodeTable`'s
      four call sites (matching the reference implementations' own
      control flow, but inconsistent with every other error condition
      `Decompress` already checks) — see `NEXT.md`. `samples/HelloGame`
      re-verified unaffected.
- [x] **`.cnj`'s own real `"bones"` rigid scene-graph hierarchy (cnjVersion
      2) — done, 2026-08-17 (session 6 continued autonomously past the
      LZX decompression review-cycle checkpoint, per explicit user
      selection of "Attempt .cnj's bone-hierarchy/skinning surface,"
      then a dedicated research pass narrowed that to bone hierarchy
      only).** Research first (this session's own standing discipline)
      confirmed the real `.cnj` format keeps bone hierarchy and skinning
      **architecturally separate already**, not one feature split for
      convenience: `"bones"` is a flat, parent-before-child scene-graph
      array (`ParseCnjBoneArrayEXT`) used to position rigid mesh pieces,
      closely analogous to `.xnb`'s own bone convention already ported
      in this project (`XnbModelBuilder`) — if anything simpler to link,
      since `.cnj` encodes each bone's own parent index, needing only a
      single forward pass, unlike `.xnb`'s child-index-list encoding.
      Skinning (vertex strides 48/52/56/68, `"skeleton"`/`"animations"`)
      was confirmed to have **no real payoff to attempt even partially**:
      this project has no `SkinnedEffect` type anywhere (not stubbed,
      not native-ABI-blocked, simply never started), and a real `.cnj`
      mesh using a skinned vertex stride will, in every practically-occurring
      case, also specify a `SkinnedEffect`-family `"effect"` value this
      reader already rejects — so loading skinned vertex *bytes* in
      isolation has nothing downstream to connect to, unlike this
      project's own established "data loads now, native rendering comes
      later" pattern (which needs the *managed* type to already exist,
      just blocked on native — not true here). `CnjModelData`/`CnjMeshData`
      gained `Bones`/`ParentBoneIndex`; `CnjModelBuilder.Build` gained a
      second bone-construction branch (real hierarchy) alongside its
      existing "no hierarchy, synthesize a bone per mesh" fallback,
      selected by whether the document has more than one `"bones"` entry
      (matching the real engine's own `hasBoneHierarchy` convention
      exactly). `CnjCompatModelBuilder` was **not** extended to link real
      bone hierarchies in this pass (its own separate, deliberately
      deferred follow-up, matching this whole feature's established
      cadence) — it now explicitly rejects a bone-hierarchy document with
      a clear `ContentLoadException` instead of silently falling back to
      its own (now genuinely *wrong*, not just incomplete) synthesized-bone
      shape. A real, honest testing gap, stated plainly rather than
      glossed over: unlike `quad.cnj`'s own byte-exact upstream-test
      port, no real `.cnj` fixture with a multi-bone array exists
      anywhere in the reference C++ engine's own test suite to
      cross-check against, so this increment's own tests use
      hand-authored fixtures verified against manually-derived expected
      structure from the confirmed source logic, not an independent
      reference. Verified: `dotnet build` clean across all 6 projects,
      0 warnings; `dotnet test`: 476/476 passing (up from 464 — 12 new
      tests covering real multi-bone parsing, field defaults, malformed/
      out-of-range bone and mesh-parent-bone rejection, and the
      no-hierarchy fallback's continued correctness; includes two
      follow-up `/code-review high` passes — the first fixed a real
      null-vs-absent inconsistency (a bone's own `"parent"`/`"transform"`
      fields and a mesh's own `"parentBone"` field all originally
      rejected JSON `null` instead of falling back to their defaults);
      the second caught that first fix's own overreach, where making the
      shared `GetInt` helper universally null-tolerant silently changed
      `"vertexStride": null` from a clean rejection into a silent
      default of 16 -- a materially worse outcome (real stride-32 vertex
      bytes, since 32 is itself an exact multiple of 16, would be
      reinterpreted as twice as many stride-16 vertices with a
      completely wrong field layout), fixed by splitting `GetInt` (kept
      strict) from a new `GetOptionalInt` (null-tolerant, used only
      where that's safe) and extracting one shared `IsAbsentOrNull`
      check instead of three independently-reimplemented ones; a third
      pass then deduplicated `GetOptionalInt` itself (it delegates to
      `GetInt` now) and corrected a mathematically overstated doc-comment
      claim ("any multiple of 32/48/52/56/68 is also a multiple of 16" --
      only 32 and 48 actually do; the severity conclusion didn't change,
      32 alone already justifies it) — see `NEXT.md`). `samples/HelloGame`
      re-verified unaffected.
- [x] **`CnjCompatModelBuilder`'s own bone-hierarchy follow-up — done,
      2026-08-17 (session 6 continued autonomously past the `.cnj`
      bone-hierarchy review-cycle checkpoint, per explicit user selection
      of "Extend CnjCompatModelBuilder for bone hierarchy").** Closes the
      one deferral flagged when the base `.cnj` bone-hierarchy feature
      landed. Mirrors `CnjModelBuilder.Build`'s own dual-path bone
      construction exactly (real hierarchy when `CnjModelData.Bones` is
      non-empty, the original synthesize-a-bone-per-mesh fallback
      otherwise) -- the explicit rejection this builder previously threw
      for a bone-hierarchy document is gone, replaced by the real
      linking logic. Verified: `dotnet build` clean across all 6
      projects, 0 warnings; `dotnet test`: 476/476 passing, unchanged —
      no new testable surface (constructing a working compat
      `ContentManager`/`GraphicsDevice` at all needs a real `cna-native`,
      the same pre-existing limitation this builder's own compat wiring
      already has). `samples/HelloGame` re-verified unaffected.
- [ ] **Deliberately deferred follow-ups, not gaps in what's above:**
      `Model`'s own `.cnj` skinning surface (vertex strides 48/52/56/68,
      `"skeleton"`/`"animations"`, every `SkinnedEffect`-family effect
      type -- confirmed architecturally separate from the now-supported
      bone hierarchy, and requiring new `Effect` subclasses plus new
      native ABI work regardless), runtime glTF content paths, and
      MonoGame's own `Lz4` `.xnb` extension (see the `.xnb`/`.cnj`
      loading entries above), and `ModelMeshPart`'s own
      `ModelEffectCollection`/`ModelMesh.Effects` gap (see that entry
      above, a real permanent gap, not deferred pending further work).
      None of these are blocked on the native C ABI the way everything
      else in this phase is — they're scoped out because each is its own
      substantial, separable feature (or, for
      `ModelEffectCollection`, structurally unfixable, and for `Lz4`, not
      grounded against any reachable format reference).
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
      is ever added. **Superseded 2026-08-18:** Phase 8 WP4 adds exactly
      that name-indexed effect-parameter system, so this reopens as real
      work once WP4 lands — re-read §27 then.

### Phase 6 — Packaging and cross-platform validation

- NuGet layout per `analysis_binding.md` §30 (`CNA.Framework.nupkg` with
  `runtimes/<rid>/native/`).
- Validate the `HelloGame` sample on at least Linux and Windows, with more
  than one CNA renderer, per §38 and §70.
- CI: pure-C ABI compile/link smoke test lives in `cna`, not here; this
  repo's CI builds/tests the managed solution only.

### Phase 8 — Complete XNA 4.0 API coverage (primary outstanding work)

Driven by the "Scope mandate" section above. Baseline inventory taken
2026-08-18: **94 of 201** tracked public XNA 4.0 types present in
`CNA.XnaCompat` (47%); **107 missing**, of which **18 already exist in
`CNA.Framework`** and need only a compat mirror, and **89 need building on
both sides**.

Reproduce the inventory (do this at the start of each work package rather
than trusting the counts above once work lands):

```bash
# list every public type CNA.XnaCompat currently declares
grep -rhoP '^\s*public\s+(?:sealed\s+|abstract\s+|static\s+|readonly\s+|partial\s+)*(?:class|struct|enum|interface|record)\s+\K\w+' \
  src/CNA.XnaCompat --include=*.cs | sort -u
```

Ordering rationale: WP1–WP4 first because they unblock the most other
types (the texture hierarchy and effect system are dependencies of several
later packages); WP8/WP9 are self-contained and can be done any time;
WP11–WP14 are the largest new subsystems and come last.

- [x] **WP1 — Graphics enums + `GraphicsResource` base — done 2026-08-18.**
      Coverage 94/201 → 115/201. `GraphicsResource` landed but nothing
      derives from it yet (WP3's job, see its own doc comment).
      `SurfaceFormat` deliberately carries CNA's seven `_EXT` values beyond
      XNA's 20 so a native value never casts to an undefined member. The
      compat side gained a reflection-driven enum-parity test that pairs
      every `Microsoft.Xna.Framework` enum with its `CNA.*` counterpart and
      compares members + `[Flags]`, so later enums are covered with no test
      edit. Tests 440 → 591. Original scope, for reference:
      <details>Build:
      `SurfaceFormat`, `DepthFormat`, `SetDataOptions`, `CubeMapFace`,
      `RenderTargetUsage`, `PresentInterval`, `GraphicsDeviceStatus`,
      `SpriteSortMode`, `TextureAddressMode`, `TextureFilter`,
      `GraphicsResource`, `GamePadDeadZone`, `DisplayOrientation`.
      Mirror-only into `CNA.XnaCompat`: `Blend`, `BlendFunction`,
      `ColorWriteChannels`, `CompareFunction`, `CullMode`, `FillMode`,
      `StencilOperation`, `ContentLoadException`. Ground the enums in
      `graphics_state.h` / `graphics.h` / `display.h` `CNA_*` constants.</details>
- [x] **WP2 — `SamplerState` + sampler collections — done 2026-08-18.**
      `SamplerState` (six XNA presets, all native-seeded via
      `cna_sampler_state_init`), `SamplerStateCollection`, and
      `GraphicsDevice.SamplerStates`/`.VertexSamplerStates`, both stages,
      16 slots each. The collection is deliberately stateless — every read
      and write goes through to `cna_graphics_device_get/set_sampler_state`
      rather than caching per slot, unlike the single-valued
      `BlendState`/`DepthStencilState`/`RasterizerState` properties.
      `TextureCollection` and `GraphicsDevice.Textures` **moved to WP3**:
      the indexer's element type is XNA's `Texture` base class, which does
      not exist until WP3 introduces it, and a collection typed on
      `Texture2D` today would need re-typing immediately afterwards.
- [x] **WP3 — Texture hierarchy — done 2026-08-18 (WP3a+WP3b).** Coverage
      →122/201. Texture base + reparent, Texture3D, TextureCube,
      RenderTargetCube, TextureCollection. SetData/GetData for the 3D/cube
      forms deliberately deferred (they need the CNA_Texture3DTransfer/
      CNA_TextureCubeTransfer descriptors, which have no 2D counterpart) --
      tracked in WP15. Original scope:
      <details>**WP3 — Texture hierarchy.** Introduce the real XNA `Texture` base
      class and reparent `Texture2D`/`RenderTarget2D` onto it (a breaking
      internal refactor — do it before more texture types exist, not
      after), then `Texture3D` (`texture_volume.h`), `TextureCube`
      (`texture.h`), `RenderTargetCube` (`render_target.h`, whose
      `_set_render_target_cube` this project already noticed but never
      used). Also carries `TextureCollection` and
      `GraphicsDevice.Textures`/`.VertexTextures`, moved here from WP2 —
      the indexer is typed on the `Texture` base this package introduces.</details>
- [x] **WP4 — Full effect system — done 2026-08-18 (WP4a+WP4b).** Coverage
      →163/201. The fabricated one-pass `EffectTechnique` is gone; techniques,
      passes, parameters and annotations are now the effect's real native
      objects. Note §27's `EffectParameter` handle caching (Phase 5) is now
      genuinely applicable again and remains **not done** — the collections
      re-resolve on every access by design; revisit if profiling shows it
      matters. `BasicEffect` still carries its own handle/helper copy instead
      of deriving from `StockEffect` — tracked in WP15. Original scope:
      <details>**WP4 — Full effect system.** Name-indexed `EffectParameter` /
      `EffectParameterCollection` / `EffectParameterClass` /
      `EffectParameterType`, `EffectAnnotation(Collection)`,
      `EffectTechniqueCollection`, plus the four remaining stock effects
      `AlphaTestEffect`, `DualTextureEffect`, `EnvironmentMapEffect`,
      `SkinnedEffect`, and `EffectMaterial`. Native: `effects.h`. This
      also retires the Phase 5 "`EffectParameter` handle caching — not
      applicable" note, which was only true while no such collection
      existed; once it does, §27's caching becomes real work again.
- [x] **WP5 — Display / adapter / presentation — done 2026-08-18.** Coverage
      →145/201. One documented deviation: `GraphicsAdapter.Adapters`/
      `.DefaultAdapter` cannot be static (every adapter call needs a device
      handle), so they take a device. Original scope:
      <details>**WP5 — Display / adapter / presentation.** `GraphicsAdapter`,
      `DisplayMode`, `DisplayModeCollection`, `PresentationParameters`,
      `GraphicsDeviceInformation`, `PreparingDeviceSettingsEventArgs`,
      `ResourceCreatedEventArgs`, `ResourceDestroyedEventArgs`. Native:
      `display.h` (already read once for `CNA_GraphicsProfile`; the
      adapter/display-mode half of that header is still unbound).</details>
- [x] **WP6 — Real `GraphicsDeviceManager` — done 2026-08-18.** The
      placeholder (a `Game` property and nothing else) is gone; every
      preference, `ApplyChanges`, and `ToggleFullScreen` now bind
      `runtime_graphics_manager.h`. Original scope:
      <details>**WP6 — Real `GraphicsDeviceManager`.** Replace the placeholder
      (currently only a `Game` property) with the real XNA surface:
      `PreferredBackBufferWidth`/`Height`/`Format`,
      `PreferredDepthStencilFormat`, `IsFullScreen`, `GraphicsProfile`,
      `PreferMultiSampling`, `SynchronizeWithVerticalRetrace`,
      `SupportedOrientations`, `ApplyChanges()`, `ToggleFullScreen()`,
      and the `IGraphicsDeviceService`/`IGraphicsDeviceManager` contracts.
      Native: `runtime_graphics_manager.h` — already fully surveyed
      2026-08-18, every function confirmed to exist.</details>
- [x] **WP7 — Game component / service model — done 2026-08-18 (WP7a+WP7b).** Coverage →194/201.
      WP7b bound `GameComponent`/`DrawableGameComponent`/
      `GameComponentCollection` against `runtime_components.h`, as predicted:
      the native game owns the collection and drives components through a
      callback table, so a managed model would have compiled and never run.
      `Game.IsActive`/`.IsFixedTimeStep`/`.TargetElapsedTime`/`.SuppressDraw()`
      and the `Activated`/`Deactivated`/`Exiting` events remain — moved to
      WP15. Original scope:
      <details>**WP7 — Game component / service model.** `IGameComponent`,
      `IUpdateable`, `IDrawable`, `GameComponent`,
      `DrawableGameComponent`, `GameComponentCollection`,
      `GameServiceContainer`, `LaunchParameters`, `FrameworkDispatcher`,
      and `Game.Components`/`.Services`/`.IsActive`/`.IsFixedTimeStep`/
      `.TargetElapsedTime`/`.SuppressDraw()`/`.ResetElapsedTime()` plus
      the `Activated`/`Deactivated`/`Exiting`/`Disposed` events. Native:
      `runtime_components.h` + the unbound half of `runtime.h`. Note
      `FrameworkDispatcher.Update()` finally gives `MediaPlayer.Update`
      its real XNA home (today `Game.Update` calls it directly as a
      documented stand-in — see that method's own doc comment).
- [x] **WP8 — `Curve` system — done 2026-08-18.** Coverage →128/201.
      Decision recorded: implemented **managed**, not bound to `curve.h`
      (which does have a full native Curve) — design invariant #3 plus
      testability, and the tests immediately caught two real math errors in
      the first draft. Original scope:
      <details>**WP8 — `Curve` system.** `Curve`, `CurveKey`, `CurveKeyCollection`,
      `CurveContinuity`, `CurveLoopType`, `CurveTangent`. Native:
      `curve.h`, though this is pure math and may be better implemented
      managed-side per design invariant #3 — decide by reading the header
      first, and record the decision.</details>
- [x] **WP9 — Touch input — done 2026-08-18.** Coverage →135/201. All seven
      types; CNA-only extensions (`pressure`, `finger_id_ext`) deliberately
      not surfaced, for XNA-shape fidelity. Original scope:
      <details>**WP9 — Touch input.** `TouchPanel`, `TouchPanelCapabilities`,
      `TouchCollection`, `TouchLocation`, `TouchLocationState`,
      `GestureSample`, `GestureType`. Native: `input_touch.h`.</details>
- [x] **WP10 — Remaining buffer/query graphics types — done 2026-08-18.**
      Coverage →149/201. `DynamicVertexBuffer`/`DynamicIndexBuffer` turned out
      to be the *same* native resource with a `dynamic` create-info flag, not
      separate bindings, so they are thin subclasses. `DrawUserIndexedPrimitives`
      still outstanding (needs `CNA_UserIndices`) — moved to WP15.
      Original scope:
      <details>**WP10 — Remaining buffer/query graphics types.**
      `DynamicVertexBuffer`, `DynamicIndexBuffer`, `VertexBufferBinding`,
      `OcclusionQuery`, and `GraphicsDevice.SetVertexBuffers(params
      VertexBufferBinding[])` / `DrawUserIndexedPrimitives` /
      `DrawInstancedPrimitives`. Native: `vertex_resources.h`,
      `index_resources.h`, `graphics_ext.h`, and
      `graphics_device.h`'s already-surveyed `cna_graphics_device_draw_user_
      indexed_primitives` / `_draw_instanced_primitives` / `CNA_UserIndices`.
- [x] **WP5b — remaining Framework/Graphics mirrors — done 2026-08-18.**
      Coverage →180/201. Compat `DirectionalLight`, `IEffectMatrices`/
      `IEffectFog`/`IEffectLights`, `ModelEffectCollection`,
      `ResourceCreatedEventArgs`/`ResourceDestroyedEventArgs`, plus
      `GraphicsDeviceInformation`, `PreparingDeviceSettingsEventArgs`,
      `IGraphicsDeviceService`, `IGraphicsDeviceManager` and `Game.Services`.
      `GraphicsDeviceManager` now implements both service contracts and
      registers itself, matching XNA. **Closes the `ModelEffectCollection`
      "permanent gap"** recorded before the mandate: the fix was to wrap the
      already-constructed base collection rather than to find an override seam
      that does not exist.
- [x] **WP4c — compat `Effect` base — done 2026-08-18. Coverage 201/201.**
      Resolved by composition rather than the `internal static` route WP3a used:
      each compat stock effect holds its `CNA.Graphics` counterpart and forwards
      (~87 members), with the compat `Effect` overriding `NativeEffectHandleValue`
      so the pair remains one native effect. docs/architecture.md updated --
      this is now a documented exception to its "no duplicated logic" rule, taken
      deliberately. Folding `BasicEffect` onto `StockEffect` remains, in WP15.
      Original note:
      <details>The one type in this area still missing.
      `Microsoft.Xna.Framework.Graphics.Effect` cannot simply be added: the
      compat stock effects derive from their `CNA.Graphics` counterparts to
      inherit ~30 native-backed properties each, and C# single inheritance
      means they cannot also derive from a parallel compat `Effect`. The fix is
      the one WP3a used for `Texture2D` — expose the leaf effects' native calls
      as `internal static` helpers and reparent the compat classes onto a compat
      `Effect` — which is a real refactor of already-reviewed code and wants its
      own increment. Also fold `BasicEffect` onto `StockEffect` while there.</details>
- [x] **WP11 — Full audio — done 2026-08-18 (WP11a+WP11b).** Coverage →191/201.
      XACT (`AudioEngine`, `AudioCategory`, `AudioStopOptions`, `WaveBank`,
      `SoundBank`, `Cue`) plus 3D audio (`AudioListener`, `AudioEmitter`).
      WP11b added `Microphone`, `MicrophoneState`,
      `DynamicSoundEffectInstance`. `DynamicSoundEffectInstance.BufferNeeded`
      and `SoundEffect.Apply3D` remain — both need native callback/3D wiring;
      moved to WP15. Original scope:
      <details>**WP11 — Full audio.** XACT (`AudioEngine`, `SoundBank`, `WaveBank`,
      `Cue`, `AudioCategory`, `AudioStopOptions`) from `xact.h`; 3D audio
      (`AudioEmitter`, `AudioListener`, `SoundEffect.Apply3D`) and
      `DynamicSoundEffectInstance`, `Microphone`, `MicrophoneState` from
      `audio.h`.</details>
- [x] **WP12 — Video playback + media completion — done 2026-08-18.** Coverage
      →167/201. `Video`/`VideoPlayer`/`VideoSoundtrackType` bound, `MediaQueue`
      mirrored. The always-empty `MediaLibrary` re-check is **still open** and
      moves to WP15. Original scope:
      <details>**WP12 — Video playback + media completion.** `Video`,
      `VideoPlayer`, `VideoSoundtrackType` from `video.h`; mirror
      `MediaQueue` into `CNA.XnaCompat` (it exists in `CNA.Framework`
      already). Revisit the always-empty `MediaLibrary` collections from
      the 2026-08-17 pass — `media_library.h` may now support more than
      that scoping note assumed.</details>
- [x] **WP13 — Storage — done 2026-08-18.** Coverage →169/201.
      `StorageDevice`/`StorageContainer` plus an internal `StorageStream :
      System.IO.Stream`. Both the `Begin`/`End` pairs and plain synchronous
      methods are offered — see below. Original scope:
      <details>**WP13 — Storage.** `StorageDevice`, `StorageContainer`, and the
      `IAsyncResult`-based `BeginOpenContainer`/`EndOpenContainer` API
      shape. Native: `storage.h`.</details>
- [x] **WP14 — Content pipeline reader API — done 2026-08-18.** Coverage
      →200/201. `ContentReader`, `ContentTypeReader`, `ContentTypeReaderManager`,
      `ContentSerializerAttribute`, `ResourceContentManager`, plus
      `EffectMaterial`. Registering a *managed* type reader is not possible
      against this ABI (the registry is keyed by canonical name with no managed
      callback route) — moved to WP15. Original scope:
      <details>**WP14 — Content pipeline reader API.** `ContentReader`,
      `ContentTypeReader`/`ContentTypeReader<T>`,
      `ContentTypeReaderManager`, `ContentSerializerAttribute`,
      `ResourceContentManager`. Native: `content_readers.h`. This is the
      extensibility half of the content system — the existing
      `ContentManager.Load<T>` covers only the built-in types.</details>
- [ ] **WP15 — Close the pre-mandate deferrals.** `.cnj` skinning (vertex
      strides 48/52/56/68, `"skeleton"`/`"animations"`), unblocked once
      WP4 lands `SkinnedEffect`; `ModelEffectCollection`'s compat mirror,
      previously called a "permanent gap" for want of an override seam —
      re-solve it, since "no seam exists" is a fixable design problem now
      that omitting it is no longer allowed; `.xnb` LZ4; runtime glTF.
- [ ] **WP16 — Re-audit to 201/201.** Re-run the inventory command above,
      drive the missing list to zero, then close out with a full
      `/code-review high` pass over everything Phase 8 added.

Explicitly still excluded from this mandate (not XNA 4.0 API surface, or
genuinely unbackable): `Microsoft.Xna.Framework.Net` /
`.GamerServices` (Xbox Live session/gamer services — `net.h`,
`net_gamers.h`, `gamer_services.h` do exist upstream, so revisit if the
user wants them, but they bind a live-service model that has no meaning
outside Xbox Live and are not part of what "write an XNA game" means
today), and the XNA *content pipeline build tooling*
(`Microsoft.Xna.Framework.Content.Pipeline.*`, a build-time assembly that
never shipped in the runtime profile).

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
