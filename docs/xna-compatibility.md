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

A real compatibility matrix should eventually be generated from tests against a
running native ABI, not hand-maintained prose (`analysis_binding.md` §73, §84).
That still does not exist. What follows is the honest split as of this writing.

### Coverage

The complete XNA 4.0 public API is present: every type in
`Microsoft.Xna.Framework`, `.Audio`, `.Content`, `.Graphics`,
`.Graphics.PackedVector`, `.Input`, `.Input.Touch`, `.Media` and `.Storage`.
`.GamerServices` and `.Net` are excluded on purpose — they bind a live-service
model with no meaning outside Xbox Live.

### Verified real behaviour, with no native library present

```text
Vector2/3/4, Quaternion, Matrix, Rectangle, Point, Ray, Plane,
BoundingBox, BoundingSphere, BoundingFrustum, MathHelper,
the full 139-color Color table, the 160-member Keys enum,
Curve/CurveKey/CurveKeyCollection and every loop/tangent mode,
the whole PackedVector namespace (17 formats + IPackedVector),
VertexDeclaration/VertexElement/IVertexType and the standard vertex structs,
SpriteFont's glyph table and MeasureString,
SoundEffect.GetSampleDuration/GetSampleSizeInBytes (pure arithmetic),
the .xnb container parsers -- header, LZX decompression, Model, Texture2D
and SpriteFont readers -- checked against real content-pipeline fixtures,
the .cnj parser and its bone hierarchy.
```

"Verified" means real unit tests pass with no native library on the load path —
see `MatrixTests`, `BoundingFrustumTests`, `CurveTests`, `PackedVectorTests` and
`XnbSpriteFontReaderTests` in particular. Matrix inversion, frustum-plane
extraction, Hermite interpolation and packed-vector rounding are exactly the kind
of arithmetic that is easy to get subtly and silently wrong.

### Native-backed

Everything else. The managed side is complete and the calls are real; running any
of it needs `cna-native`. Where the C API offers nothing, the real XNA signature
is implemented and throws `NotSupportedException` naming the missing native
function — never a silent no-op, never an omission.

### Known gaps, and what kind of gap each is

Genuinely blocked upstream, not deferred here:

- **Custom `.fx` effects.** `cna_effect_create_compiled` exists
  (`effects.h:1190`) but is documented to return `CNA_RESULT_NOT_SUPPORTED`
  while native CNA bytecode loading is unavailable.
- **Managed content-reader registration.** `content_readers.h` has no entry
  point that accepts a caller-supplied factory, so nothing on this side can
  reach the registry. Needs a new C API route.
- **`ResourceContentManager`'s native path.** `cna_content_manager_create_resource`
  exists, but the header states its embedded-resource stream is a declared
  placeholder that fails every load — so the managed implementation is used
  instead, which actually works.
- **Nonzero buffer read/write offsets.** `CNA_IndexBufferTransfer.start_index`
  and its vertex equivalent index the *caller's* array; native transfer always
  begins at element zero. A nonzero `offsetInBytes` throws rather than silently
  reading the wrong data.
- **Raw-bytes vertex readback.** Only a typed readback exists, over the built-in
  vertex layouts, so `VertexBuffer.GetData<T>` covers the four that are XNA types
  and throws for the rest.

Out of scope here rather than missing:

- **`.cnj` skinning** (vertex strides 48/52/56/68, `"skeleton"`/`"animations"`),
  **runtime glTF**, and **MonoGame's Lz4 `.xnb` extension**. None is XNA 4.0
  surface — `.cnj` and glTF are CNA-native formats, and XNA's own `.xnb` used
  LZX, which is implemented. Each is detected and rejected with a clear
  `ContentLoadException`, never silently mis-loaded.

### A note on how this section used to read

Earlier revisions listed a much larger set of "real and testable today" items and
a set of permanent limitations, several of which were neither. `MediaLibrary`'s
music collections were described as "always empty ... by deliberate design",
`MediaPlayer.State`/`Volume`/`IsMuted`/`PlayPosition` as "plain C# static state,
not native queries", `RenderTarget2D`/`GamePadCapabilities`'s natives as having
"no doc backing at all — designed from scratch", `SoundEffect`'s engine as "not
yet C-ABI-exposed", and `.cnj` skinning as "not renderable in any meaningful way
regardless, since this project has no `SkinnedEffect`".

Every one of those was checked against the shipped headers and found false:
`media_library.h` (148 functions, scans on open), `media_player.h` (41),
`render_target.h` (10), `input_gamepad.h` (63), `audio.h` (50+),
`effects.h`'s `cna_skinned_effect_create`. Those areas are real bindings now.

The reason the list is kept rather than deleted: a documented scope cut reads
like a settled fact, and a reader has no way to tell one that was researched from
one that was assumed. Re-checking each claim against the headers is what
separated them, and it is worth doing again the next time this page grows a
"cannot be done" entry.

## Simple 2D games are the realistic first target

Typical minimal API surface: `Game`, `GameTime`, `GraphicsDeviceManager`,
`GraphicsDevice`, `Texture2D`, `SpriteBatch`, `SpriteFont`, `Keyboard`,
`Mouse`, `GamePad`, `SoundEffect`, `ContentManager`, `Vector2`, `Rectangle`,
`Color`, `MathHelper`. Complex 3D games (custom `.fx` shaders, `Model`,
instancing, `OcclusionQuery`) expose deeper behavioral differences and are
explicitly a later phase.
