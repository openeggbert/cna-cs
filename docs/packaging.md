# Packaging and native distribution plan

No CNA.NET NuGet package is published yet. The three projects intentionally remain
`IsPackable=false`; a source checkout and an explicitly located native CNA library are currently
required. This document describes the acceptance target, not shipped support.

## Proposed package graph

| Package | Purpose | Dependencies |
| --- | --- | --- |
| `CNA.Interop` | Internal ABI assembly; normally transitive, not a user API | RID-native CNA package |
| `CNA.Framework` | Idiomatic `CNA.*` API | `CNA.Interop` exact compatible version |
| `CNA.XnaCompat` | Strict XNA facade plus explicit CNA extension namespace | `CNA.Framework` exact compatible version |
| `CNA.Xna` | Optional convenience metapackage for game authors | `CNA.XnaCompat` plus selected native runtime policy |

Start at `0.x` SemVer. A managed breaking change increments the minor version while pre-1.0. The
minimum compatible CNA C ABI remains an explicit runtime check; package dependency ranges must not
replace that check.

## Native assets

Preferred layout when redistribution/licensing and builds are ready:

```text
runtimes/<rid>/native/<platform library name>
```

Candidate RIDs are added only after real validation, for example `linux-x64`, `linux-arm64`,
`win-x64`, `win-arm64`, `osx-x64`, and `osx-arm64`. Do not publish an empty placeholder RID.

If CNA native binaries remain separately distributed, `CNA_NATIVE_LIBRARY` and `CNA_NATIVE_DIR`
are the supported explicit configuration hooks, and loader errors must name both. Accidental system
library search paths are not an installation story.

## Package acceptance test

For each claimed RID:

1. pack Release assemblies, XML docs, symbols and Source Link metadata;
2. inspect package contents and licenses/notices;
3. restore into an empty temporary consumer with no repository-relative references;
4. build a generated `dotnet new cna-game` project;
5. resolve the intended native library and verify its ABI/symbol set;
6. run 60-frame smoke and 600-frame stability tests;
7. verify a missing/wrong native library produces one actionable diagnostic;
8. repeat in a clean CI image for the claimed OS and architecture.

Package IDs, native redistribution policy, signing, and final versioning must be approved before
changing `IsPackable`. A package that builds but cannot locate a compatible native library is not a
release candidate.
