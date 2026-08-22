# CNA.NET

> **Status: functional but not XNA API-complete or release-ready.** The current strict XNA 4.0
> Windows runtime metadata comparison reports 1,246 unallowlisted differences, including 101
> accidental `CNA.*` signature leaks.

CNA.NET is the C#/.NET binding for [CNA](https://github.com/openeggbert/cna), a native C++ game
framework. Its intended path is:

```text
C# XNA-style game
        ↓
CNA.XnaCompat   Microsoft.Xna.Framework public facade
        ↓
CNA.Framework   idiomatic CNA managed implementation
        ↓
CNA.Interop     internal P/Invoke boundary
        ↓
CNA C ABI → CNA C++
```

XNA source compatibility is the priority. CNA's native headers determine available backend
capabilities; authoritative XNA assemblies/documentation determine the managed public contract.
Binary compatibility with Microsoft's strong-named assemblies is not the primary goal.

## Measured state

As of 2026-08-22:

- Debug and Release solution builds: 0 warnings, 0 errors;
- managed XNA-compat tests: 171/171 passing;
- native facade tests: 10/10 passing on the available Linux x64 headless CNA runtime; its
  Texture3D-specific test is capability-limited and is not counted;
- strict metadata profile: 257 reference types versus 244 target types, 1,246 failures, 0
  allowlisted;
- compile-time hierarchy corpus: passes unchanged on XNA, CNA, FNA, and MonoGame; records one Kni
  hierarchy difference (`VertexDeclaration` is not a `GraphicsResource` there);
- sibling template: CNA build, generated-project build, and 60/600-frame CNA runs pass;
- FNA, MonoGame, and Kni template source builds pass; 60-frame MonoGame and Kni runs pass, while the
  configured FNA assembly reports unavailable at runtime with a clean exit code 2;
- NuGet/RID packages: not yet available.

The earlier name-level coverage claim that the XNA API was complete was false. The metadata tool
now checks type identity and hierarchy, interfaces, modifiers, generics, parameters, properties,
events, constants/enums, delegates, nested types, and relevant layout/attributes. See
[`docs/xna-compatibility.md`](docs/xna-compatibility.md) for the exact profile and remaining groups.

## Build and test

Requires .NET 8 or later.

```bash
dotnet restore CNA.sln
dotnet build CNA.sln -c Debug --no-restore
dotnet build CNA.sln -c Release --no-restore
dotnet test tests/CNA.Framework.Tests/CNA.Framework.Tests.csproj
dotnet test tests/CNA.XnaCompat.Tests/CNA.XnaCompat.Tests.csproj
```

Native integration tests skip cleanly when CNA is unavailable. To run them explicitly:

```bash
CNA_NATIVE_LIBRARY=/path/to/libcna_c_api.so \
  xvfb-run -a dotnet test tests/CNA.Integration.Tests/CNA.Integration.Tests.csproj
```

The loader also accepts `CNA_NATIVE_DIR`. An ABI-incompatible library is rejected with a diagnostic
before missing symbols fail later.

## Strict API verification

Supply legally obtained XNA 4.0 reference assemblies without adding them to this repository:

```bash
dotnet build CNA.sln -c Release
XNA_REFERENCE_PATH=/path/to/xna-reference-assemblies \
  dotnet run --project tools/api-compat -c Release --no-build -- --format text
```

Exit codes are 0 for a clean/reviewed contract, 1 for unallowlisted differences, and 2 for bad
configuration. `--format json` and `--format github` support automation; `--leak-only` requires no
XNA reference assembly. The checked-in allowlist is intentionally empty.

## Repository layout

```text
src/CNA.Interop/                  internal C ABI declarations and marshalling
src/CNA.Framework/                public CNA.* API and native ownership
src/CNA.XnaCompat/                Microsoft.Xna.Framework facade
tests/CNA.Framework.Tests/        managed CNA behavior tests
tests/CNA.XnaCompat.Tests/        managed strict-facade behavior tests
tests/CNA.XnaCompat.CompileProbe/ source-assignability corpus
tests/CNA.Integration.Tests/      real native ABI/runtime tests
tools/api-compat/                 signature-aware XNA metadata verifier
tools/coverage/                   portable header/symbol discovery tools
samples/HelloGame/                small managed sample
```

The sibling `cna-cs-template` is the richer CNA-first demonstration and installable `dotnet new`
template (`cna-game`).

## Architecture and packaging

Public XNA hierarchy takes priority over implementation inheritance. Corrected families use
composition and internal adapters so one native resource has one owner; remaining public
inheritance from `CNA.*` is a tracked compatibility failure, not the intended end state.

- [`docs/architecture.md`](docs/architecture.md) — layer and ownership rules.
- [`docs/xna-compatibility.md`](docs/xna-compatibility.md) — measured profile and extension boundaries.
- [`docs/packaging.md`](docs/packaging.md) — proposed package/RID graph and release tests.
- [`plan.md`](plan.md) — current measurable roadmap.
- [`NEXT.md`](NEXT.md) — chronological engineering history.

## License

CNA.NET is licensed under the [Microsoft Public License (Ms-PL)](LICENSE). See
[`NOTICE.md`](NOTICE.md) for naming and upstream notices.
