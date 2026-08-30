# CNA.NET

> **Status: exact public metadata for the selected XNA 4.0 Windows runtime profile; not yet
> behaviorally complete or release-ready.** The strict comparison reports 257/257 types, zero
> differences, zero CNA leaks, and an empty allowlist. The binding targets CNA C ABI 0.20.0.

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

As of 2026-08-30, against CNA `next` `e178282fc` (C ABI 0.20.0), OPENGLES3, Linux x64:

- Debug and Release solution builds: 0 warnings, 0 errors;
- managed tests: 560/560 framework and 199/199 XNA-compat passing;
- native integration tests: 122/122 passing in Debug and Release;
- strict metadata profile: 257 reference types versus 257 target types, 0 differences, 0
  allowlisted;
- standalone public/protected CNA-type leak gate: 0 findings;
- compile-time hierarchy corpus: passes unchanged on XNA, CNA, FNA, and MonoGame; records one Kni
  hierarchy difference (`VertexDeclaration` is not a `GraphicsResource` there);
- deterministic behavior corpora: one manifest defines 470 observations: 83 Math, 23 Input, 153
  Graphics, 13 Resource, 46 Content, 83 Audio, 7 XACT, 20 Media, 17 Video, 20 Storage, and 5
  DeviceLifecycle. CNA executes all 470 on Linux: 199 pure, 166 device, and 105 runtime. FNA
  completes all 199 pure observations before its own SoundEffect
  finalizer aborts the process; its native runtime is unavailable here. MonoGame compiles the pure
  probe but its audio initialization aborts before the new audio group, and its profile omits the
  XACT/Storage runtime surface. Direct XNA IL establishes implemented strict semantics; the
  independent Windows XNA snapshot remains pending;
- isolated native ownership stress: 100/100 game create/use/finalize/destroy/recreate cycles pass
  in both Debug and Release, now including the authored DXT3 `SpriteFont` the cycle used to
  exclude. This is not an allocator-level leak claim;
- portable C-authority ABI verification passes 86 native/managed measurements and prototype
  compilation against the ABI 0.20.0 headers on Linux ELF x64; Windows PE and macOS Mach-O workflow
  execution remains pending, and 86 is a floor rather than a coverage claim -- it measures 13 of the
  80 interop structs;
- the upstream ABI diff over the consumed surface (`tools/coverage/baselinediff.py`) reports
  0.8.0 → 0.19.0 as purely additive, and 0.19.0 → 0.20.0 as twelve renderer-identity constant
  differences and nothing else -- none of which this binding consumes, because it reads the
  renderer's name rather than its identity;
- native loading follows the reviewed `cna-cs-native-abi/1` matrix, resolves all 849 imports, and
  passes 11 isolated compatibility fixtures (2 accepted, 9 rejected). The matrix accepts exactly
  0.20.0: 0.6.0/0.7.0/0.8.0 were retired when this binding began importing routes CNA added after
  them, 0.19.0 when 0.20.0 superseded it, and fixtures prove both kinds of retired generation are
  actually refused;
- sibling template: the checked-in repository project, the generated development project and the
  isolated package consumer all build; the package-generated source has no checkout/sibling path
  and 60/600-frame native runs pass;
- FNA, MonoGame, and Kni template source builds pass; 60-frame MonoGame and Kni runs pass, while the
  configured FNA assembly reports unavailable at runtime with a clean exit code 2;
- NuGet/RID packages: none published. An explicit local-only harness successfully creates and
  consumes `CNA.Interop`, `CNA.Framework`, and `CNA.XnaCompat` previews with an experimental
  `linux-x64` native layout; this is not a shipping-RID claim.

The earlier name-level coverage claim that the XNA API was complete was false. The metadata tool
now proves exactness for the selected runtime profile by checking type identity and hierarchy,
interfaces, modifiers, generics, parameters, properties, events, constants/enums, delegates,
nested types, and relevant layout/attributes. Broader XNA product profiles and behavioral parity
remain separate work; see [`docs/xna-compatibility.md`](docs/xna-compatibility.md).

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

The loader also accepts `CNA_NATIVE_DIR`. Explicit configuration is fail-fast and takes precedence
over package-native lookup. Admission follows
[`cna-cs-native-abi/1`](docs/native-abi-compatibility.md), not a same-major range: the version must
have a reviewed matrix entry, all 849 imports must exist, and signature/shape canaries must pass.
The one accepted entry today is C ABI 0.20.0.
Wrong ABI, missing symbols, conflicts, wrong architecture/load failure, and missing-library cases
report the attempted configuration, consumer/detected ABI where available, RID, and remediation;
`CNA_NATIVE_DIAGNOSTICS=1` enables low-level loader details.

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
tools/abi-verify/                 portable C-authority ABI/layout/prototype verifier
tools/native-abi-probe/           isolated managed native-loader admission probe
tools/behavior-corpus/            authoritative corpus manifest/count/snapshot tooling
tools/coverage/                   portable header/symbol discovery tools
tools/profile-inventory/          separate future-XNA-profile inventory generator
samples/HelloGame/                small managed sample
```

The sibling `cna-cs-template` is the richer CNA-first demonstration and installable `dotnet new`
template (`cna-game`).

## Architecture and packaging

Public XNA hierarchy takes priority over implementation inheritance. Corrected families use
composition and internal adapters so one native resource has one owner. The selected strict
profile now has no public `CNA.*` base types or public/protected CNA-type signature leaks.

- [`docs/architecture.md`](docs/architecture.md) — layer and ownership rules.
- [`docs/xna-compatibility.md`](docs/xna-compatibility.md) — measured profile and extension boundaries.
- [`docs/packaging.md`](docs/packaging.md) — proposed package/RID graph and measured local acceptance harness.
- [`plan.md`](plan.md) — current measurable roadmap.
- [`NEXT.md`](NEXT.md) — chronological engineering history.

## License

CNA.NET is licensed under the [Microsoft Public License (Ms-PL)](LICENSE). See
[`NOTICE.md`](NOTICE.md) for naming and upstream notices.
