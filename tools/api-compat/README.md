# CNA XNA API contract verifier

This tool compares compiled public/protected metadata. It does not parse C# source and it does not
use CNA's native headers as the definition of the XNA managed API. The default profile aggregates
the seven split XNA 4.0 Windows runtime assemblies listed in
`profiles/xna40-windows-runtime.json`.

Build `CNA.XnaCompat`, then point `XNA_REFERENCE_PATH` at a directory containing legally obtained
XNA 4.0 assemblies:

```bash
dotnet build src/CNA.XnaCompat/CNA.XnaCompat.csproj -c Release
XNA_REFERENCE_PATH=/path/to/xna/v4.0/assemblies \
  dotnet run --project tools/api-compat -c Release --no-build -- \
  --target src/CNA.XnaCompat/bin/Release/net8.0/CNA.XnaCompat.dll
```

During an audit, `--report-only --format json` records the complete measured difference without
turning it into a passing quality gate. Normal mode exits 1 for every unreviewed difference. An
allowlist entry must match a diagnostic's code and subject exactly and include a rationale;
optional `expected` and `actual` fields can make the match even narrower. Stale and duplicate
entries fail so the allowlist cannot quietly accumulate obsolete exceptions.

The reference-independent CI guard is:

```bash
dotnet run --project tools/api-compat -c Release --no-build -- --leak-only \
  --target src/CNA.XnaCompat/bin/Release/net8.0/CNA.XnaCompat.dll
```

It rejects any `CNA.*` type in a public/protected base type, interface, parameter, return/property/
event/field type, or generic constraint. It does not claim XNA parity by itself.

The verifier currently compares type identity/kind/accessibility, base classes, interfaces,
abstract/sealed state, generic arity and constraints, layout, selected semantic attributes,
constructors and methods (including return types, parameter names/types/ref-out-in/defaults,
generic constraints and virtual state), properties/indexers and accessor visibility, events,
fields/constants, enum values, delegates, and nested types. JSON and GitHub-annotation output are
available for CI and archived evidence.
