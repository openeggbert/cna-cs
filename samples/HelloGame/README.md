# HelloGame

The minimal end-to-end CNA.NET game: clear the screen, load a texture, draw
it with `SpriteBatch`, read the keyboard, exit on <kbd>Escape</kbd>.
Reproduces the reference example from
`../../../cnabinding/analysis_binding.md` §38/§140 exactly.

## Status

This sample **builds** today (`dotnet build`), because `CNA.Interop`'s
`LibraryImport` declarations don't need the native library to exist at
compile time. It does **not run** yet: `dotnet run` will throw a
`DllNotFoundException` (or similar) the moment `Game1`'s constructor calls
into `CNA.Interop.Native`, because `cna-native` — the shared library built
from `openeggbert/cna`'s C ABI — does not exist upstream yet. See
`../../plan.md` ("Hard dependency on `openeggbert/cna`").

Once that native library exists for your platform, place it where the
runtime can find it — either next to this project's build output, or under
a `runtimes/<rid>/native/` folder once `../../plan.md` Phase 6 (NuGet
packaging) is implemented — and `dotnet run` should produce a window
clearing to cornflower blue and drawing a texture, matching the first major
success criterion in `analysis_binding.md` §70.

## Content

This sample expects a texture asset named `eggbert` under a `Content/`
directory (matching `Content.RootDirectory = "Content";` in `Game1`'s
constructor). No content pipeline exists yet in this repository (see
`plan.md` Phase 4) — for now, `ContentManager.Load<Texture2D>` simply asks
native CNA to load `Content/eggbert` by whatever asset-loading convention
`openeggbert/cna` implements first.
