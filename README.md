# CNA.NET

> **Status: complete XNA 4.0 API surface; needs the `cna-native` shared library
> to run.**


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

**Coverage: the complete XNA 4.0 public API.** Every type in
`Microsoft.Xna.Framework`, `.Audio`, `.Content`, `.Graphics`,
`.Graphics.PackedVector`, `.Input`, `.Input.Touch`, `.Media` and `.Storage`
is present, bound against the real
[`openeggbert/cna`](https://github.com/openeggbert/cna) C API headers.
`.GamerServices` and `.Net` are deliberately excluded: they bind a live-service
model that has no meaning outside Xbox Live and are not part of what "write an
XNA game" means.

Two groups, by what runs without a native library:

**Real and fully testable today, with no native dependency.** The math and
value types (`Vector2`/`3`/`4`, `Matrix`, `Quaternion`, `Color`, `Rectangle`,
`Point`, `Ray`, `Plane`, the `Bounding*` types, `MathHelper`), the `Curve`
system, the whole `PackedVector` namespace, the vertex-format layer
(`VertexDeclaration`, `VertexElement`, the `VertexPosition*` structs),
`SpriteFont`'s glyph table and `MeasureString`, and the `.xnb`/`.cnj` content
*parsers* — including LZX decompression, which is checked against real
content-pipeline fixtures.

**Native-backed.** Everything else. The managed side is complete and the calls
are real; running any of it needs the `cna-native` shared library on the load
path. `samples/HelloGame` builds and starts, then throws `DllNotFoundException`
from `Game`'s constructor without it — the expected failure point, not a bug
here.

The binding is grounded in the headers rather than designed alongside them:
every signature is read out of `modules/c-api/include/CNA/C/*.h`, and where the
C API offers nothing, the real XNA signature is implemented and throws
`NotSupportedException` naming the missing native function. Never a silent
no-op, never an omission.

`dotnet build CNA.sln` builds all 6 projects cleanly (0 warnings, 0 errors) and
`dotnet test` passes 661 unit tests. See [`plan.md`](plan.md) for the
phase-by-phase status and [`NEXT.md`](NEXT.md) for the session-by-session
history and where to pick up next.

### A note on the earlier status text

This section used to say that the native-backed half "does **not** yet work end
to end, because it depends on a stable C ABI in `openeggbert/cna` that has not
been implemented there yet", that `MediaLibrary`'s music collections were
"always empty" because the scan "depends on FFmpeg/native tag-parsing
infrastructure this binding has no way to reach", and that `MediaPlayer`'s
`State`/`Volume`/`IsMuted`/`PlayPosition` were "plain C# static state needing no
native call".

All three were false, and an audit of every such claim against the shipped
headers is what established it. The C API ships 60 headers;
`media_library.h` alone is 148 functions and scans on open; `media_player.h` is
41 and owns the queue, the state machine and the playback clock. Each of those
areas is now a real binding, and the reimplementations they had accumulated are
gone. The lesson is recorded here rather than quietly edited away: a documented
scope cut is a claim about the world, and it goes stale like any other.

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
