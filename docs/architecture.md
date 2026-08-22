# Architecture

This document summarizes the architecture defined by
`openeggbert/cna`'s `analysis_binding.md` and
`openeggbert/cna`'s `analysis_binding_sharp_runtime.md`, as applied to this
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

The `Microsoft.Xna.Framework`-named compatibility facade. Its public classes,
interfaces, generic collections and inheritance must reproduce the selected XNA
profile even when the corresponding `CNA.*` implementation hierarchy differs.
C# single inheritance makes subclassing CNA implementations unsuitable for many
facade types: inherited public members and base identity become part of the XNA
contract whether intended or not.

The target pattern is therefore:

- public compat types define the XNA type system;
- internal composition/adapters delegate to `CNA.Framework`;
- an owned native handle has exactly one owner;
- borrowed and parent-owned handles are never promoted to owners;
- CNA-only APIs live in `CNA.Framework` or an explicit extension namespace.

Components, Game/device/window services, content readers/managers, models, audio/XACT, media,
storage, curves, textures, render targets, effects, graphics states, vertex declarations,
SpriteBatch, and the remaining strict collections now use this pattern. No exported type in the
selected strict profile inherits a `CNA.*` type, and `tools/api-compat --leak-only` reports zero
public/protected CNA-type signatures. The full seven-assembly strict comparison also reports
257/257 types and zero metadata diagnostics with an empty allowlist.

#### Why the XNA value types are not literally the same type as the `CNA` namespace ones

C# structs cannot inherit from another struct, so `Microsoft.Xna.Framework.Vector2`
cannot be a subtype of `CNA.Vector2`. For the math and graphics value types,
`CNA.XnaCompat` therefore defines its own struct with the XNA public contract. Internal conversion
helpers translate to and from the `CNA` implementation at delegation boundaries. These helpers
are deliberately not public operators: Microsoft XNA did not expose conversions to `CNA.*`, and
doing so formerly added 120 strict metadata/leak findings. This is intentional, documented small
duplication — see
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

Native handles are classified rather than treated uniformly:

- **owned** — one managed owner releases the handle through `SafeHandle`/`Dispose`;
- **borrowed** — valid for the documented owner/callback lifetime and never released by the view;
- **adopted** — ownership is transferred from one wrapper to another, invalidating the source;
- **parent-owned** — a child view cannot outlive or destroy the parent resource.

This distinction is required because .NET reachability and CNA's C++ resource
lifetime are independent. Facades may share an internal backend, but must not
create a second owning wrapper. Game destruction is also an ownership test:
CNA correctly rejects it while owned child handles remain. A real regression
found here was `StockEffect.Dispose()` omitting its base reflection handles;
the next game could not be created until that disposal chain was corrected.

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
