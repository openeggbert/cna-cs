# CNA.NET

> **Status: In progress - NOT YET FUNCTIONAL**


CNA.NET is the official C#/.NET language binding for [CNA](https://github.com/openeggbert/cna),
a native C++ implementation of an XNA-inspired game framework with dozens of
renderer backends (Vulkan, Direct3D, OpenGL family, Metal, WebGPU, SDL GPU,
and more).

CNA.NET plays the same role for CNA that **XNA 4.0**, **FNA**, or
**MonoGame** play for C# game code: your game logic stays in C#, targeting
familiar `Microsoft.Xna.Framework`-style types, while CNA's C++ engine does
the actual work underneath.

```text
your C# game
        ↓
CNA.XnaCompat   →  Microsoft.Xna.Framework-compatible facade
        ↓
CNA.Framework   →  idiomatic CNA .NET API
        ↓
CNA.Interop     →  raw P/Invoke over the CNA C ABI
        ↓
CNA's stable C ABI  (lives in openeggbert/cna, not here)
        ↓
CNA C++ core  →  Sharp Runtime, CNA subsystems, and every CNA renderer
```

A high-quality `CNA.XnaCompat` layer aims to let many existing XNA 4.0 game
projects recompile against CNA.NET with little or no source modification —
their gameplay code stays in C#, and CNA does the rendering. See
[`docs/xna-compatibility.md`](docs/xna-compatibility.md) for exactly what
that promise does and does not cover.

## Status

The pure math/value-type layer (`Vector2`/`3`/`4`, `Matrix`, `Quaternion`,
`Color`, `Rectangle`, `Point`, `Ray`, `Plane`, the `Bounding*` types,
`MathHelper`) is complete and fully real today — no native dependency, so
unlike everything else it isn't a stub. `SpriteFont` (glyph table and
`MeasureString`) and the vertex-format layer (`VertexDeclaration`,
`VertexElement`, the standard `VertexPosition*` structs) join that "fully
real today" group too — each has a real-XNA public-API escape hatch that
needs no native ABI at all. `Model`/`ModelBone`/`ModelMesh`/`ModelMeshPart`
and their collection types go a step further: the *entire* feature needs
**zero** native ABI, since `Model.Draw()`/`ModelMesh.Draw()` are pure
managed logic built entirely on top of already native-backed primitives
(`SetVertexBuffer`, `Indices`, `Effect.Apply()`, `DrawIndexedPrimitives`).
`Song` (construction, equality, `FromUri`) is real and testable today
too, against real temporary files — its own escape hatch is a plain
file-existence check, no native call at all. `Album`/`Artist`/`Genre`/
`Playlist`/`MediaLibrary` are real and fully testable too, but by
deliberate design rather than a native escape hatch: every collection
`MediaLibrary` exposes is always empty, since the real C++ engine's own
scanning logic depends on FFmpeg/native tag-parsing infrastructure this
binding has no way to reach (see [`NEXT.md`](NEXT.md) for the detail) —
the full real XNA object model compiles and runs, it just never has
anything real to report.
Everything else native-backed (`Game`, `GraphicsDevice`, `Texture2D`,
`SpriteBatch` including the full extended `Draw`/`DrawString` overload
families, `RenderTarget2D`, `SoundEffect`/`SoundEffectInstance`,
`VertexBuffer`/`IndexBuffer`, `ContentManager`, `Keyboard`, `Mouse`,
`GamePad`) has its managed side built and compiles, but does **not** yet
work end to end, because it depends on a stable C ABI in
[`openeggbert/cna`](https://github.com/openeggbert/cna) that has not been
implemented there yet (`modules/c-api/`). `BasicEffect` straddles both
groups: its constructor and full property surface (`World`/`View`/
`Projection`, lighting, fog, texturing, `EnableDefaultLighting()`) are real
and tested today with no native dependency, same escape hatch `SpriteFont`
found, but `Apply()` itself is native-backed like everything else in this
paragraph — and since `Model.Draw()` ultimately calls `Effect.Apply()` too,
drawing a `Model` end to end is still blocked on the same native ABI as
everything else, even though the model/mesh/bone bookkeeping around it
isn't. `MediaPlayer` straddles the same way: `State`/`Volume`/`IsMuted`/
`PlayPosition` are plain C# static state needing no native call, but
`Play`/`Pause`/`Resume`/`Stop` are native-backed. See
[`plan.md`](plan.md) for the full phase-by-phase status and
[`NEXT.md`](NEXT.md) for the session-by-session history of how it got here
and where to pick up next.

`dotnet build CNA.sln` builds all 6 projects cleanly (0 warnings, 0 errors)
and `dotnet test` passes all 315 unit tests. Running `samples/HelloGame`
builds and starts, then throws a `DllNotFoundException` for `cna-native`
from inside `Game`'s constructor — exactly the expected failure point until
the upstream C ABI ships, not a bug here.

## Why a C# binding, and why first

XNA itself was a C# framework. A high-fidelity `CNA.XnaCompat` facade is the
most direct way to let an existing library of XNA/MonoGame/FNA-era C# game
code run on CNA's engine without a manual C++ rewrite — turning ports that
might otherwise take thousands of hours into ports that mostly need to
address genuine API or content incompatibilities. This is why C#/.NET is the
first official CNA language binding, ahead of JavaScript/TypeScript, Rust,
Python, and the rest — see
[`../cnabinding/analysis_binding.md`](../cnabinding/analysis_binding.md) and
[`../cna/analysis_binding_languages.md`](../cna/analysis_binding_languages.md)
for the full reasoning.

## Repository layout

```text
cna-dotnet/
├── src/
│   ├── CNA.Interop/      internal, low-level P/Invoke over the CNA C ABI
│   ├── CNA.Framework/    idiomatic public CNA .NET API
│   └── CNA.XnaCompat/    Microsoft.Xna.Framework-compatible facade
├── tests/
│   ├── CNA.Framework.Tests/
│   └── CNA.XnaCompat.Tests/
├── samples/
│   └── HelloGame/        the minimal end-to-end game from the design docs
├── tools/
│   └── binding-generator/  (planned) codegen for repetitive ABI wrappers
├── docs/
│   ├── architecture.md
│   └── xna-compatibility.md
├── CNA.sln
└── plan.md
```

## Building

Requires the [.NET SDK](https://dotnet.microsoft.com/download) (net8.0 or
later).

```bash
dotnet build CNA.sln
dotnet test CNA.sln
```

Any IDE that understands SDK-style `.csproj`/`.sln` projects works without
extra configuration: Visual Studio and JetBrains Rider open `CNA.sln`
directly; VS Code works via the C# Dev Kit / C# extension (a minimal
`.vscode/` is included); the `dotnet` CLI itself is the common denominator
for everything else (e.g. Neovim + an LSP, or CI).

`samples/HelloGame` builds as an ordinary `dotnet run` executable once a
`cna-native` shared library for your platform is available — see
[`samples/HelloGame/README.md`](samples/HelloGame/README.md).

## Relationship to Sharp Runtime

CNA may use [Sharp Runtime](https://github.com/openeggbert/sharp-runtime)
internally as a native C++ dependency (it implements a practical subset of
`System.*` in C++23). **CNA.NET applications run on the normal .NET runtime
and the real .NET Base Class Library** — `System.String`,
`System.Collections.Generic.List<T>`, `System.Threading.Tasks.Task`, and so
on. Sharp Runtime is not exposed anywhere in CNA.NET's managed API, is not a
CLR, and does not execute your C# code. See
[`docs/architecture.md`](docs/architecture.md) for the full explanation —
this distinction is spelled out in detail because the name "Sharp Runtime"
otherwise invites the wrong assumption.

## License

CNA.NET is licensed under the [Microsoft Public License (Ms-PL)](LICENSE),
matching `openeggbert/cna`. See [`NOTICE.md`](NOTICE.md) for the project's
relationship to Microsoft XNA Framework naming, Sharp Runtime, and FNA.

## See also

- [`openeggbert/cna`](https://github.com/openeggbert/cna) — the native C++
  engine this binding wraps.
- [`openeggbert/sharp-runtime`](https://github.com/openeggbert/sharp-runtime) —
  the native .NET-like C++ library CNA may use internally.
- `../cnabinding/analysis_binding.md`,
  `../cnabinding/analysis_binding_sharp_runtime.md`,
  `../cna/analysis_binding_languages.md` — the design analysis this
  repository's architecture is built from.
