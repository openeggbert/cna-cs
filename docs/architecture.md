# Architecture

This document summarizes the architecture defined by
`../../cnabinding/analysis_binding.md` and
`../../cnabinding/analysis_binding_sharp_runtime.md`, as applied to this
repository specifically. Read those two documents for the full reasoning;
this page is the "what it means for the code in `src/`" version.

## Layers

```text
your C# game
        ↓
CNA.XnaCompat   (project) → Microsoft.Xna.Framework[.Graphics|.Input|.Content] (namespace)
        ↓
CNA.Framework   (project) → CNA[.Graphics|.Input|.Content]                    (namespace)
        ↓
CNA.Interop     (project, internal) → CnaHandle, CnaResult, raw P/Invoke
        ↓
CNA stable C ABI                                    ← in openeggbert/cna
        ↓
CNA C++ core
        ↓
Sharp Runtime, CNA subsystems, renderers
```

**Project name and C# namespace are deliberately different for the idiomatic
layer.** The project is called `CNA.Framework` (matching the repository
layout `analysis_binding.md` §18 prescribes: `src/CNA.Interop/`,
`src/CNA.Framework/`, `src/CNA.XnaCompat/`), but the types inside it live in
the bare `CNA` namespace (`CNA.Graphics`, `CNA.Input`, `CNA.Content`, or
just `CNA` for core types like `Vector2`/`Game`) — matching the *real* CNA
C++ codebase's own namespace convention, `CNA::Graphics`, `CNA::Input`,
`CNA::Devices`, bare `CNA::`, *not* `CNA::Framework::`. The C++ side has
exactly the same asymmetry: its `modules/graphics-ext/` folder produces
namespace `CNA::Graphics`, not `CNA::GraphicsExt`. `CNA::Internal::*` is the
separate, private-implementation namespace in the C++ codebase — this
project's equivalent is `CNA.Interop`, which is why *that* project's
namespace (`CNA.Interop`) matches its project name, unlike `CNA.Framework`.

Each arrow is a one-way dependency. `CNA.Framework` never references
`CNA.XnaCompat`; `CNA.Interop` never references `CNA.Framework`. This keeps
the C ABI dependency (the only genuinely fragile boundary) confined to one
project.

### `CNA.Interop`

Everything here is `internal`. It exists to give the rest of the solution a
`LibraryImport`-based, source-generated P/Invoke surface over the CNA C ABI,
plus the small set of ABI-shaped structs (`CnaResult`, `CnaHandle`,
`CnaVector2`, `CnaColor`, `CnaGameTime`, `CnaKeyboardState`) needed to call
it. No ergonomics live here — no operators, no properties, no `SafeHandle`.
That belongs one layer up.

### `CNA.Framework` (project) → `CNA` namespace

The idiomatic, CNA-native public API, in the `CNA`/`CNA.Graphics`/
`CNA.Input`/`CNA.Content` namespaces (see above for why the project name and
namespace differ). This is where:

- managed value types (`Vector2`, `Color`, `GameTime`) are implemented
  directly in C#, with no P/Invoke call for trivial operations
  (`analysis_binding.md` §23);
- native-backed resources (`Texture2D`, `SpriteBatch`, `GraphicsDevice`) wrap
  a `SafeHandle` and convert `CnaResult` failures into `CnaException`
  (`analysis_binding.md` §24, §10);
- the `Game` lifecycle bridges to native code through an
  `[UnmanagedCallersOnly]` callback adapter (`analysis_binding.md` §20).

The `CNA` namespace is intentionally not required to look like XNA. It is
free to grow CNA-specific functionality that XNA never had, without that
functionality being permanently constrained by 2010-era XNA naming
(`analysis_binding.md` §18).

### `CNA.XnaCompat` (project) → `Microsoft.Xna.Framework` namespace

The `Microsoft.Xna.Framework`-named compatibility facade. Reference types
(`Game`, `GraphicsDeviceManager`, `GraphicsDevice`, `SpriteBatch`,
`ContentManager`) are thin subclasses of their `CNA`-namespace counterparts —
no duplicated logic, just XNA-shaped constructors and members forwarding to
`base`.

**Two documented exceptions, both forced by C# single inheritance** (Phase 8;
see `plan.md`). Where XNA's own type hierarchy has a base class that this
facade must also expose, a compat leaf type cannot both derive from that compat
base *and* from its `CNA`-namespace counterpart:

- **Textures** (WP3a). `Microsoft.Xna.Framework.Graphics.Texture2D` derives
  from the compat `Texture`, so `Texture t = someTexture2D;` compiles as it does
  in XNA. It reuses `CNA.Graphics.Texture2D`'s `internal static` native helpers
  rather than duplicating any logic — about five call sites.
- **Effects** (WP4c). `Microsoft.Xna.Framework.Graphics.Effect` is a real base
  of the compat stock effects for the same reason. Here the reuse is by
  *composition*: each compat effect holds its `CNA.Graphics` counterpart and
  forwards, roughly 87 members across the five of them. The forwards carry no
  logic, but there are enough of them to be worth calling out. What keeps this
  from being two drifting objects is that the compat `Effect` overrides
  `NativeEffectHandleValue` to report the inner effect's handle — so the pair
  is one native effect, not two.

Both were taken deliberately, over the alternative of leaving the XNA base type
absent, once the project's scope mandate made complete XNA 4.0 coverage a
requirement rather than a goal.

#### Why the XNA value types are not literally the same type as the `CNA` namespace ones

C# structs cannot inherit from another struct, so `Microsoft.Xna.Framework.Vector2`
cannot simply be a subclass of `CNA.Vector2` the way
`Microsoft.Xna.Framework.Game` can subclass `CNA.Game`. For the handful of
math value types, `CNA.XnaCompat` defines its own small struct with the same
field layout and implicit conversion operators to/from the `CNA`-namespace
version, so a `CNA.Graphics.GraphicsDevice.Clear(Color)` call still works
seamlessly from XNA-style code. This is the one place in
the solution with intentional, documented small duplication — see
`analysis_binding.md` §74 ("simple value structs" and "repetitive resource
wrappers" are exactly the kind of thing a future codegen tool
(`tools/binding-generator/`) should generate instead of hand-writing).

## The C ABI is not part of this repository

The CNA C ABI — `CNA_Result`, opaque generation-checked handles, UTF-8
string conventions, struct versioning, ABI version checks — is designed and
implemented in `openeggbert/cna` (`modules/c-api/`), not here. This
repository only *consumes* that ABI from `CNA.Interop`. See
`analysis_binding.md` §3–§16 for the ABI design itself; this repository
treats it as an external contract, not something to redesign locally.

## Sharp Runtime is invisible here, on purpose

CNA's native C++ implementation may use
[Sharp Runtime](https://github.com/openeggbert/sharp-runtime) internally —
a C++23 library providing .NET-like APIs (`System::String`,
`System::Collections::Generic::List<T>`, `System::Threading::Tasks::Task`,
and similar) for native code that benefits from .NET-shaped semantics.

That is a native implementation detail of CNA. It must never leak into this
repository, for a simple reason: **C# already has the real .NET Base Class
Library.** `CNA.Framework` and `CNA.XnaCompat` use `System.String`,
`System.Collections.Generic.List<T>`, `System.Threading.Tasks.Task`,
`System.TimeSpan`, and so on directly — never a CNA- or Sharp-Runtime-flavored
reimplementation of them.

Concretely, this means:

- `CNA.Interop` never references, includes, or links Sharp Runtime headers
  or libraries. It only talks to CNA's stable C ABI, which itself hides
  Sharp Runtime (`analysis_binding_sharp_runtime.md` §7).
- Native strings cross the ABI as UTF-8 (`byte*` + length), not as any
  `System::String`-shaped object; `CNA.Interop`/`CNA.Framework` convert
  to/from `System.String` at the boundary.
- Native collections cross the ABI as count/copy or pointer+count APIs, not
  as any native list/dictionary type; `CNA.Framework` exposes
  `System.Collections.Generic.IReadOnlyList<T>` and similar to callers.
- Native exceptions never cross the ABI; they become `CNA_Result` +
  structured error info, which `CNA.Interop` converts into `CnaException`.
- Native async operations, once CNA has any, become a neutral CNA async
  handle at the ABI, which `CNA.Framework` maps to `System.Threading.Tasks.Task<T>` —
  never to a native task type.

If a change in this repository seems to require binding directly to Sharp
Runtime, that is a sign the design has gone wrong somewhere upstream in the
C ABI, not a reason to add a Sharp Runtime reference here. See
`analysis_binding_sharp_runtime.md` §31 ("Why wrapping Sharp Runtime
directly would be a mistake") before doing so.

## Native resource lifetime

Every native-backed `CNA.Framework` type owns (or is) a `System.Runtime.InteropServices.SafeHandle`
subclass whose `ReleaseHandle()` calls the matching `cna_*_release` native
function. `IDisposable.Dispose()` on the public type disposes the handle.
This is the pattern from `analysis_binding.md` §24, and it is required
because .NET's GC lifetime and CNA's native C++ resource lifetime are two
different systems that only agree to meet at an explicit handle — never
assume they are otherwise in sync (`analysis_binding_sharp_runtime.md` §61).

## Threading

Callbacks from native CNA into managed code (`Update`, `Draw`, etc.) arrive
through `[UnmanagedCallersOnly]` static methods that resolve a `GCHandle`
back to the managed `Game` instance, on the thread that called `cna_game_run`
(or `cna_game_run_one_frame`/`cna_game_tick`). The real, shipped
`openeggbert/cna` C API does document a thread-affinity contract now (unlike
when this note was first written): most `cna_game_*` routes are refused with
`CNA_RESULT_THREAD` when called from any thread other than the one that
created the game — confirmed directly against a real test
(`LifecycleSmoke.c`'s `set_title_on_wrong_thread`), not just inferred. Treat
every native call in this repository as main-thread-only until each
individual function's own real doc comment is actually read, rather than
assuming the general rule covers every case.
