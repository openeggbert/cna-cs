# XNA compatibility goals

Source: `../../cnabinding/analysis_binding.md` §31–§36. This page exists so
the distinction below is stated once, clearly, in this repository, rather
than assumed.

## Four different kinds of "compatible"

1. **Source compatibility** — an existing XNA `.cs` file recompiles against
   `CNA.XnaCompat` with few or no changes. **This is the primary goal of
   this repository.**
2. **API compatibility** — namespaces, class names, method names,
   properties, constructors, overloads, and enums match XNA 4.0. Necessary
   for (1) to work at all.
3. **Behavioral compatibility** — code that compiles also *runs the same
   way*: blend-state behavior, sampler behavior, matrix conventions, input
   timing, and so on. This is harder than (2) and is proven with tests, not
   asserted.
4. **Binary compatibility** — an old, precompiled XNA `.exe`/`.dll` runs
   unmodified against CNA.NET assemblies. **Not a goal.** Old binaries
   expect Microsoft's original assembly identity, strong names, and
   possibly Xbox Live/GFWL services that may no longer exist. Do not build
   toward this or advertise it as supported.

## What CNA.NET does not promise

Directly from `analysis_binding.md` §72 — restated here because it is easy
to accidentally overclaim in a README or release note:

- Every historical XNA binary running unchanged.
- Every XNA game working.
- Every custom Content Pipeline extension (`ContentImporter`,
  `ContentProcessor`, `ContentTypeWriter`/`ContentTypeReader`) working
  without adaptation.
- Every Windows-specific dependency (`System.Windows.Forms`,
  `Microsoft.Win32`, raw `user32.dll` P/Invoke, DirectInput, Windows Media)
  becoming portable — those are not XNA APIs and are outside CNA's control.
- Every historical Xbox Live / Games for Windows Live service being
  reproduced.
- Zero-overhead FFI for every call.
- Every CNA API being available in C# on day one.

## What "publish a compatibility matrix" means here

A real compatibility matrix should eventually be generated from tests
against a running native ABI, not hand-maintained prose (`analysis_binding.md`
§73, §84) — that does not exist yet. In the meantime, the honest three-way
split as of this writing is:

```text
Compiles + verified real behavior (no native dependency; see plan.md Phase 4):
    Vector3, Vector4, Quaternion, Matrix, Rectangle, Point, Ray, Plane,
    BoundingBox, BoundingSphere, BoundingFrustum, MathHelper,
    the full 139-color Color table, the 160-member Keys enum,
    SpriteFont (glyph table + MeasureString only -- DrawString needs
    SpriteBatch, which is native-backed, see below),
    SoundEffect.GetSampleDuration/GetSampleSizeInBytes (pure arithmetic;
    SoundEffect itself is native-backed, see below),
    VertexDeclaration/VertexElement/IVertexType, the five standard vertex
    structs (VertexPosition/PositionColor/PositionTexture/
    PositionColorTexture/PositionNormalTexture),
    VertexDeclaration.FromType/IndexBuffer.SizeForType (the Type-taking
    VertexBuffer/IndexBuffer constructors' reflection logic -- pure, no
    native call, though VertexBuffer/IndexBuffer themselves stay
    native-backed, see below),
    BasicEffect's constructor and full property surface (World/View/
    Projection, DiffuseColor/EmissiveColor/SpecularColor/SpecularPower,
    AmbientLightColor/DirectionalLight0-2/EnableDefaultLighting, fog,
    TextureEnabled/Texture, VertexColorEnabled, Alpha, LightingEnabled) --
    Apply() itself is native-backed, see below,
    Model/ModelBone/ModelMesh/ModelMeshPart and their collection types
    (ModelBoneCollection/ModelMeshCollection/ModelMeshPartCollection/
    ModelEffectCollection) -- unlike everything else in this list, this
    entire feature needs zero native ABI, not just a construction-time
    escape hatch: Model.Draw()/ModelMesh.Draw() are pure managed logic
    built on top of already native-backed primitives (see below for why
    drawing a Model end to end is still blocked anyway),
    IEffectMatrices/IEffectFog/IEffectLights (interfaces, no state of
    their own to be native-backed),
    Song (construction, Equals/GetHashCode/ToString, FromUri) -- a file-
    existence check, no native call, real and testable against real temp
    files, same "escape hatch" shape as SpriteFont/BasicEffect's
    construction. MediaPlayer.State/Volume/IsMuted/PlayPosition/IsRepeating/
    IsShuffled too -- plain C# static state, not native queries, matching
    the real C++ engine's own architecture (see below for why
    Play/Pause/Resume/Stop themselves are still blocked). MediaQueue/
    SongCollection (indexer, Count, ActiveSong/ActiveSongIndex,
    Add/Clear/enumeration) and MediaPlayer.DetectSongEndedByElapsedTime,
    MoveNext/MovePrevious's shuffle/repeat/clamped-direction logic -- also
    zero native ABI, all pure managed logic and math,
    Album/Artist/Genre/Playlist/MediaLibrary/MediaSource and their music
    collections -- real and fully testable, but by deliberate design, not
    a native escape hatch: every music collection MediaLibrary exposes is
    always empty (see below for why), so there is genuinely no native
    call anywhere in that part of the feature at all, not even a blocked
    one. MediaLibrary's picture side (Picture/PictureAlbum/
    PictureCollection/PictureAlbumCollection, GetPictureFromToken/
    SavePicture) -- genuinely real, not always-empty: saving a picture
    needs only plain file I/O (SavedPictureStore), no native call at all
    (see below for the image-dimension/thumbnail fallback reasoning).
    Reading a real, uncompressed OR real, LZX-compressed .xnb Model
    asset's bytes into bones/meshes/vertex-and-index-buffer data
    (CNA.Content.Xnb) -- zero native ABI, pure C#/BCL logic, confirmed
    byte-for-byte against real MonoGame-compiled fixtures (LZX
    decompression itself -- LzxDecoder/XnbLzxDecompression, a direct
    port of the real C++ engine's own LzxDecoder -- confirmed
    byte-for-byte against an independently FNA-produced reference
    decompressed output too, not just self-consistency; see below for
    why only the final VertexBuffer/IndexBuffer construction step is
    blocked). Reading a
    real, minimal-scope subset of the real engine's own .cnj JSON Model
    format (CNA.Content.Cnj: JSON envelope + an optional real "bones"
    rigid scene-graph hierarchy (cnjVersion 2), BasicEffect only, vertex
    sidecar strides 16/20/24/32 only) -- also zero native ABI, same
    "parse pure data, block only on final VertexBuffer/IndexBuffer
    construction" split as the .xnb path.
    VisualizationData's own construction (two 256-element float[] arrays,
    all-zero until MediaPlayer.GetVisualizationData populates them) --
    zero native ABI, pure managed data (see below for why populating them
    is still blocked)

Compiles, but blocked on the native CNA C ABI (openeggbert/cna) to run:
    Game, GameTime, GraphicsDeviceManager, GraphicsDevice (Clear,
    SetRenderTarget, SetVertexBuffer, Indices, DrawPrimitives,
    DrawIndexedPrimitives), SpriteBatch (full Draw/DrawString overload
    families), Texture2D, RenderTarget2D, Keyboard/KeyboardState,
    Mouse/MouseState, GamePad/GamePadState/GamePadCapabilities,
    SoundEffect/SoundEffectInstance, VertexBuffer/IndexBuffer,
    ContentManager (RootDirectory, Load<Texture2D>, Load<SoundEffect>,
    Load<SpriteFont> -- capped at 256 glyphs, see plan.md; Load<Model>'s
    own final VertexBuffer/IndexBuffer construction step for both the
    .xnb and .cnj paths, see above for the real, unblocked parsing steps
    these sit on top of),
    Effect.Apply/BasicEffect.Apply (EffectTechnique/EffectPass/
    DirectionalLight are pure scaffolding around this, no ABI of their own)
    -- and by extension Model.Draw()/ModelMesh.Draw(), since drawing a
    mesh part means calling the effect's Apply() partway through,
    MediaPlayer.Play/Pause/Resume/Stop (six new cna_mediaplayer_* natives,
    shaped to match the real C++ engine's own MediaPlayer over SDL3_mixer),
    MediaPlayer.IsVisualizationEnabled/GetVisualizationData (two more new
    cna_mediaplayer_* natives -- the real FFT/ring-buffer capture work
    lives entirely in native code, matching a real, working, unusually
    well-engineered implementation the real C++ engine already has, see
    below)

Not started at all (all deliberately deferred, not blocked -- see plan.md
Phase 4's own follow-up bullet):
    Model's own .cnj skinning surface (vertex strides 48/52/56/68,
    "skeleton"/"animations" runtime animation playback, and every
    SkinnedEffect-family effect type -- confirmed architecturally
    separate from the "bones" rigid hierarchy, which is now supported;
    not renderable in any meaningful way regardless, since this project
    has no SkinnedEffect anywhere to consume skinning data), morph
    targets, runtime glTF content paths, and MonoGame's own Lz4 .xnb
    extension (see above/below for why only a minimal-scope .cnj subset
    and LZX, not Lz4, were in scope for their respective formats),
    ModelMeshPart's own ModelEffectCollection/ModelMesh.Effects compat gap
    (a real, permanent structural limitation, not a temporary scope cut --
    see plan.md Phase 4 for why) -- none of these are blocked on the
    native ABI, each its own substantial separable feature (or, for
    ModelEffectCollection, structurally unfixable)
```

Note on trust level: the items above are *not* all equally well-grounded.
The extended `SpriteBatch.Draw` primitive matches a concrete struct shape
given in `analysis_binding.md` §22. `RenderTarget2D`/`GamePadCapabilities`'s
native functions have **no doc backing at all** — designed from scratch,
following this project's general ABI conventions but with nothing upstream
to check them against. `SoundEffect`/`SoundEffectInstance` also have no doc
backing, but are better-grounded than that: the real `openeggbert/cna` C++
engine already has a working (if not yet C-ABI-exposed) implementation of
both over SDL3_mixer, and every function here was shaped to match its real
method surface and documented semantics. `BasicEffect` is grounded the same
way, against `modules/graphics/`'s own working `BasicEffect` implementation
-- every property, `EnableDefaultLighting()`'s exact default light values,
and `Apply()`'s parameter-computation algorithm were read from its real
source, not invented. `Model`/`ModelMesh`/`ModelMeshPart`/`ModelBone` are
grounded the strongest of any Phase 4 item so far: not just "shaped to
match a real implementation" but built entirely out of already-native-backed
primitives, needing no new native function at all -- see `plan.md` Phase 4
for the detail. `Model`/`ModelBone`/`ModelMesh`/`ModelMeshPart` and their
collections now all have a `CNA.XnaCompat` mirror -- `ModelMeshPart`
turned out a fully trivial subclass, since this compat layer has no
separate compat `Effect` hierarchy at all and compat `VertexBuffer`/
`IndexBuffer` already subclass their base counterparts, so nothing about
it actually needed overriding. `ModelEffectCollection`/`ModelMesh.Effects`
is the one real, permanent exception, not a temporary scope cut: it's
constructed at field-initializer time inside the base `ModelMesh`, with
no override seam at all, structurally closer to `MediaPlayer.Queue`'s own
documented non-mirror than to anything else in this feature -- ordinary
`var`-typed/`foreach` consumption still works fine either way (compat
`BasicEffect` instances added to it upcast correctly), so only an
explicit `Microsoft.Xna.Framework.Graphics.ModelEffectCollection` type
declaration is affected. `ContentManager.Load<Model>()` on the compat
`ContentManager` also returns a real, compat-typed `Model` for both
content formats -- `XnbCompatModelBuilder`/`CnjCompatModelBuilder`,
each reusing the base class's own `.xnb`/`.cnj`-parsing directly rather
than duplicating it, with the identical `.xnb`-then-`.cnj` dispatch
order the base class's own `LoadModel` uses -- see `NEXT.md` for the
full design reasoning on all of this. `Song`/`MediaPlayer` are grounded
against `modules/media/`'s own working implementation the same way
`SoundEffect`/`BasicEffect` were, deliberately scoped down from that
implementation's much larger surface (see the "Not started at all" list
above) -- and, unlike `Model`, `Song` *does* have a full `CNA.XnaCompat`
mirror, since its construction has no equivalent blocker.
`MediaPlayer`'s own visualization data (`IsVisualizationEnabled`/
`GetVisualizationData`/`VisualizationData`) is grounded the same way, but
turned out the best-case outcome of any deferred feature this session
researched: a real, working, *dependency-free* implementation (a
from-scratch 512-point FFT over a lock-free ring buffer fed from
SDL3_mixer's own post-mix callback) rather than something partial or
infrastructure-blocked -- see `NEXT.md` for the full detail. Has a full
`CNA.XnaCompat` mirror too, and a trivial one: `VisualizationData`
references no other `CNA` type at all, so the compat type is an empty
subclass.
`MediaPlayer.Queue` is the one part of this whole feature *without* a
compat mirror, for a different, more structural reason than `Model`'s:
`LoadSong` always constructs a base `CNA.Media.Song` internally, and
`MediaPlayer` being a `static` class means (unlike every other compat
type this session built) there's no subclassing seam to override that --
a compat `Queue` property would return songs that fail an explicit
compat-typed downcast, not just an inconvenience. `Play(SongCollection)`
itself *is* supported despite this -- it only needs an upcast of the
input, not a downcast of any output, so `Queue`'s own blocker never
applied to it.
`Album`/`Artist`/`Genre`/`Playlist`/`MediaLibrary`/`MediaSource` are
grounded differently from everything else in this list: not "shaped to
match a real implementation" at all, since the real implementation's
actual scanning logic depends on FFmpeg/native tag-parsing infrastructure
with no equivalent on either side of this binding -- what's grounded here
is the real XNA *public API surface* (every type/property/constructor
validation, read from the real headers), deliberately not the scanning
behavior behind it, which stays permanently empty by design. This *does*
have a full `CNA.XnaCompat` mirror despite the structural caution
`MediaPlayer.Queue` needed, because every collection here is provably
always empty -- there's no real data that could ever diverge between a
base-typed and compat-typed empty collection, unlike `LoadSong`'s always-
non-empty, always-base-typed song copies.
`MediaLibrary`'s picture side is grounded the same way `Album`/`Artist`/
etc. are (real public API surface, read from the real headers/source) but
needed yet another different compat-mirror shape, because unlike the
music side its data is genuinely real and growing. A covariant-return
factory-hook design (the same pattern `Game.CreateGraphicsDevice` uses)
was tried first and does not fit: `PictureCollection`/`PictureAlbumCollection`
are independent reimplementations of their `CNA.Media` counterparts, not
subclasses (same reason as `SongCollection`/`AlbumCollection`), and a
covariant-return override requires the override's return type to actually
be a subtype of the base's declared return type -- an independent
reimplementation by definition is not one. `CNA.XnaCompat`'s `MediaLibrary`/
`Picture`/`PictureAlbum` are instead full independent reimplementations,
built directly on `CNA.Media.SavedPictureStore` (the shared low-level,
security-sensitive file-I/O helper) rather than on the base class's own
picture-tracking. See `NEXT.md`'s picture-library entry for the full
reasoning trail, including the abandoned covariant-hook attempt.
`CNA.Content.Xnb` (real `.xnb` `Model` loading) is grounded the same way
`SoundEffect`/`BasicEffect` are (a real, working C++ reference
implementation, not invented), but confirmed to an unusual depth for this
project: not just read, but hand-traced byte-by-byte against a real,
independently-produced MonoGame-compiled fixture, catching exactly the
kind of subtle format misunderstanding reading source alone can miss.
Deliberately scoped to real, uncompressed `.xnb` files only for that
first pass -- LZX/LZ4 decompression and the real engine's own
`.cnj`/glTF content paths were all out of scope then (each is its own
large, separable feature; see `plan.md` Phase 4). `XnbBasicEffectReader`
reads every field a real `BasicEffect` `.xnb` entry serializes (so the
stream position stays correct for whatever follows) but doesn't apply
them to a real `BasicEffect` instance -- doing so needs its external
texture reference resolved via `ContentManager.Load<Texture2D>()`,
itself native-ABI-blocked, so this is recorded as a known gap rather
than half-wired.

Real, LZX-compressed `.xnb` support (`LzxDecoder`/`XnbLzxDecompression`)
closed most of that first deferral -- grounded at least as strongly as
anything else in this project: a direct, line-by-line C# port of the
real C++ engine's own `LzxDecoder` (itself a from-scratch C++ port of
FNA's `LzxDecoder.cs`, preserving FNA's own variable names/control flow
specifically to stay verifiable against the original), confirmed
byte-for-byte against two real, LZX-compressed MonoGame fixtures
(`Explosion.xnb`/`FontCalibri14.xnb`) *and* an independently-produced
reference decompressed output (the exact bytes FNA's own unmodified
decoder produces, run under Mono) -- not a self-consistency check, and
not just "parses without throwing." `Lz4` (a MonoGame-only `.xnb`
extension original XNA/FNA never produced or read) stays rejected with a
clear exception -- confirmed no byte-level framing details for it exist
anywhere reachable to implement it correctly, the same conclusion the
real C++ engine's own maintainers independently reached first.

`CNA.Content.Cnj` (a minimal-scope subset of `.cnj` `Model` loading) is
grounded the same way, against the real engine's own `ModelTypeReader::Read`
-- confirmed field-by-field, plus a real gtest fixture reproduced
byte-for-byte as a C# test (see `tests/CNA.Framework.Tests/assets/cnj/README.md`).
Its own `BasicEffect` field application is a real, load-bearing
divergence from `XnbBasicEffectReader`'s, not an oversight: `.cnj`'s
`BasicEffect` JSON has no material-color fields at all, only `texture`/
`vertexColorEnabled`, so `CnjModelBuilder` doesn't reuse
`XnbModelBuilder.ApplyBasicEffectData`. Every sidecar path a `.cnj`
document names is validated by `CnjPathContainment` (a direct port of
the real engine's own `PathContainment.hpp` component-wise containment
check) before it is ever opened, since it's untrusted, file-supplied
input, distinct in shape from `SavedPictureStore.SanitizePictureName`'s
own bare-filename check. Real, multi-entry `"bones"` hierarchies (cnjVersion
2) are supported -- grounded against `ParseCnjBoneArrayEXT`'s own real
parent-index/transform encoding, confirmed a single forward pass suffices
(unlike `.xnb`'s own child-index-list encoding, which needs two passes).
Skinning (vertex strides 48/52/56/68, `"skeleton"`/`"animations"`) stays
deliberately out of scope, confirmed architecturally *independent* of the
now-supported `"bones"` hierarchy, not a smaller slice of it -- and
confirmed to have no real payoff without a `SkinnedEffect` type, which
doesn't exist anywhere in this project. `BasicEffect` stays the only
supported effect (no `PbrEffect`/`SkinnedEffect`/`DualTextureEffect`) --
each excluded surface is rejected with a clear, documented exception, never silently
mis-loaded, the same discipline `.xnb`'s own Lz4 rejection already
established. `CnjCompatModelBuilder` is this path's own `CNA.XnaCompat`
mirror, exactly `XnbCompatModelBuilder`'s shape: reuses the shared
native-free `.cnj` parsing step, builds compat-typed `Model`/`ModelBone`/
`ModelMesh`/`ModelMeshPart`/buffers/`BasicEffect` throughout, and reuses
`CnjModelBuilder.ApplyBasicEffectData` directly for its (trivial,
one-line) effect field-assignment logic rather than duplicating it, and
now also links a document's own real `"bones"` hierarchy exactly like
the base path does (its own bone-construction control flow near-duplicates
`CnjModelBuilder.Build`'s, the same trade-off `XnbCompatModelBuilder`
already accepts for its own relationship to `XnbModelBuilder`).
See `plan.md` Phase 4 and `NEXT.md`'s per-session entries for the full
detail on each.

"Compiles + verified" means real unit tests pass with no native library
present — see `tests/CNA.Framework.Tests/MatrixTests.cs` and
`BoundingFrustumTests.cs` in particular, since matrix inversion and frustum
plane extraction are exactly the kind of math that's easy to get subtly
wrong.

## Simple 2D games are the realistic first target

Typical minimal API surface: `Game`, `GameTime`, `GraphicsDeviceManager`,
`GraphicsDevice`, `Texture2D`, `SpriteBatch`, `SpriteFont`, `Keyboard`,
`Mouse`, `GamePad`, `SoundEffect`, `ContentManager`, `Vector2`, `Rectangle`,
`Color`, `MathHelper`. Complex 3D games (custom `.fx` shaders, `Model`,
instancing, `OcclusionQuery`) expose deeper behavioral differences and are
explicitly a later phase.
