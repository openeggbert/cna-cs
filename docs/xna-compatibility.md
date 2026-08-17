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
    (see below for the image-dimension/thumbnail fallback reasoning)

Compiles, but blocked on the native CNA C ABI (openeggbert/cna) to run:
    Game, GameTime, GraphicsDeviceManager, GraphicsDevice (Clear,
    SetRenderTarget, SetVertexBuffer, Indices, DrawPrimitives,
    DrawIndexedPrimitives), SpriteBatch (full Draw/DrawString overload
    families), Texture2D, RenderTarget2D, Keyboard/KeyboardState,
    Mouse/MouseState, GamePad/GamePadState/GamePadCapabilities,
    SoundEffect/SoundEffectInstance, VertexBuffer/IndexBuffer,
    ContentManager (RootDirectory, Load<Texture2D>, Load<SoundEffect>,
    Load<SpriteFont> -- capped at 256 glyphs, see plan.md),
    Effect.Apply/BasicEffect.Apply (EffectTechnique/EffectPass/
    DirectionalLight are pure scaffolding around this, no ABI of their own)
    -- and by extension Model.Draw()/ModelMesh.Draw(), since drawing a
    mesh part means calling the effect's Apply() partway through,
    MediaPlayer.Play/Pause/Resume/Stop (six new cna_mediaplayer_* natives,
    shaped to match the real C++ engine's own MediaPlayer over SDL3_mixer)

Not started at all (all deliberately deferred, not blocked -- see plan.md
Phase 4's own follow-up bullet):
    Model file-format loading, MediaPlayer's visualization data
    (GetVisualizationData) -- neither blocked on the native ABI, each its
    own substantial separable feature
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
for the detail. Note also that `Model`/its collection types have **no
`CNA.XnaCompat` mirror yet** -- a deliberate scope cut, not an oversight,
since there is no `ContentManager.Load<Model>` to produce one any other way
right now; `var`-typed/chained consumption of the `CNA.Graphics`-namespaced
types works fine in the meantime, same as `EffectTechnique`/
`DirectionalLight`'s existing compat gap. `Song`/`MediaPlayer` are grounded
against `modules/media/`'s own working implementation the same way
`SoundEffect`/`BasicEffect` were, deliberately scoped down from that
implementation's much larger surface (see the "Not started at all" list
above) -- and, unlike `Model`, `Song` *does* have a full `CNA.XnaCompat`
mirror, since its construction has no equivalent blocker.
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
