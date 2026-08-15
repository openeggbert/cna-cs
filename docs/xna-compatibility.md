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

Once real API surface exists, this repository should maintain a table like:

```text
API                                 Status
------------------------------------------------
Game                                Full
GameTime                            Full
GraphicsDeviceManager               Full
GraphicsDevice                      Partial
SpriteBatch                         Full
Texture2D                           Full
SpriteFont                          Not started
Keyboard                            Full
Mouse                               Not started
GamePad                             Not started
BasicEffect                         Not started
Model                               Not started
RenderTarget2D                      Not started
```

populated from real tests, not aspiration (`analysis_binding.md` §73, §84).
This does not exist yet — Phase 4 in `../plan.md` is where it starts.

## Simple 2D games are the realistic first target

Typical minimal API surface: `Game`, `GameTime`, `GraphicsDeviceManager`,
`GraphicsDevice`, `Texture2D`, `SpriteBatch`, `SpriteFont`, `Keyboard`,
`Mouse`, `GamePad`, `SoundEffect`, `ContentManager`, `Vector2`, `Rectangle`,
`Color`, `MathHelper`. Complex 3D games (custom `.fx` shaders, `Model`,
instancing, `OcclusionQuery`) expose deeper behavioral differences and are
explicitly a later phase.
