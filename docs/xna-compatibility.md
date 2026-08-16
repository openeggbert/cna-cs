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
    the full 139-color Color table, ~150-member Keys,
    SpriteFont (glyph table + MeasureString only -- DrawString needs
    SpriteBatch, which is native-backed, see below)

Compiles, but blocked on the native CNA C ABI (openeggbert/cna) to run:
    Game, GameTime, GraphicsDeviceManager, GraphicsDevice (Clear,
    SetRenderTarget), SpriteBatch (full Draw/DrawString overload families),
    Texture2D, RenderTarget2D, Keyboard/KeyboardState, Mouse/MouseState,
    GamePad/GamePadState, ContentManager (RootDirectory, Load<Texture2D>
    only -- Load<SpriteFont> not supported yet, see plan.md)

Not started at all:
    BasicEffect/Effect, Model, VertexBuffer, IndexBuffer, SoundEffect,
    SoundEffectInstance, Song, MediaPlayer, GamePad.GetCapabilities
```

Note on trust level: the `SpriteBatch.Draw` overloads and `RenderTarget2D`
above are *not* equally well-grounded. The extended `Draw` primitive matches
a concrete struct shape given in `analysis_binding.md` §22.
`RenderTarget2D`'s two native functions have **no doc backing at all** —
they were designed from scratch this session, following this project's
general ABI conventions but with nothing upstream to check them against.
See `plan.md` Phase 4 and `NEXT.md`'s 2026-08-16 session-4 entry for detail.

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
