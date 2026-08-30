# Packaging and native distribution qualification

No CNA.NET NuGet package has been published. Production builds of all three projects still have
`IsPackable=false`; only the dedicated `CnaPackageAcceptance=true` path enables local preview
packing. The results below prove mechanics for one selected local native build. They do not approve
package IDs/versioning, native redistribution, signing, or an officially supported RID.

## Validated managed graph

| Local package | Purpose | Package dependency |
| --- | --- | --- |
| `CNA.Interop` | Internal ABI assembly; may carry the explicitly selected experimental RID asset | none |
| `CNA.Framework` | Idiomatic `CNA.*` API | aligned local `CNA.Interop` preview version in the isolated feed |
| `CNA.XnaCompat` | Strict XNA facade plus explicit CNA extensions | aligned local `CNA.Framework` preview version in the isolated feed |

The proposed `CNA.Xna` convenience metapackage was not needed to prove this graph and is not
created by the harness. Start-at-`0.x` SemVer and final package-ID policy still require release
approval. Package dependency ranges never replace the explicit
[`cna-cs-native-abi/1`](native-abi-compatibility.md) loader contract.

## Reproducible local acceptance

On the measured Linux x64 host, this command completed the complete acceptance path without
publishing anything:

```bash
DOTNET_COMMAND=/path/to/dotnet \
  scripts/Package-Acceptance.sh \
  --native-library /absolute/path/to/libcna_c_api.so \
  --output /tmp/cna-package-acceptance
```

The native input is mandatory and explicit. The script refuses a non-Linux-x64 host, creates an
isolated feed and package cache, performs a clean Release solution build, packs, inspects, installs,
builds and runs a newly generated consumer, and writes `acceptance-report.json`. Omitting
`--output` uses an automatically removed temporary directory. It never invokes `nuget push`.

The measured preview version was `0.1.0-local.1`, producing:

- `CNA.Interop.0.1.0-local.1.nupkg` and `.snupkg`;
- `CNA.Framework.0.1.0-local.1.nupkg` and `.snupkg`;
- `CNA.XnaCompat.0.1.0-local.1.nupkg` and `.snupkg`.

Every main package contained its managed DLL, XML documentation, `LICENSE`, `NOTICE.md`, and
`README.md`. Every symbol package contained its portable PDB. Nuspec repository metadata recorded
the source URL and base Git commit. Because this acceptance checkpoint has uncommitted changes,
that commit is not claimed to identify the package contents exactly; clean/reproducible Source Link
policy remains a release decision. The local `CNA.Interop` package additionally contained:

```text
runtimes/linux-x64/native/libcna_c_api.so
```

## Measured linux-x64 experiment

The isolated generated `dotnet new cna-game` project restored from the local feed and built with no
`CnaCsRoot`, `CNA_CS_ROOT`, sibling `ProjectReference`, developer absolute path, or source-checkout
reference. Its package-native library resolved from the RID asset with both `CNA_NATIVE_LIBRARY`
and `CNA_NATIVE_DIR` unset. It completed 60 frames and 600 frames with the OPENGLES3 renderer.

Negative/precedence cases also executed in fresh processes:

- removing the packaged native asset produced the actionable no-library/RID diagnostic;
- a dependency-free ELF32 fixture on the x64 host produced the wrong-architecture/binary-format
  diagnostic with platform/RID context;
- the complete ABI fixture matrix accepted exact 0.6.0, additive 0.7.0, additive 0.7.0 with an
  unrelated export, and the reviewed CNA.NET subset of 0.8.0;
- the same matrix rejected a missing required export, testable changed signature, incompatible
  major, structurally incompatible same-major library, malformed 0.0.0 metadata, and unreadable
  metadata;
- a major-version-1 fixture reported detected ABI 1.0.0 versus consumer ABI 0.6.0 and the selected path;
- a loadable fixture missing one of all 854 required imports named the missing symbol;
- an invalid explicit path failed without package fallback;
- two recognized files in `CNA_NATIVE_DIR` produced a conflict diagnostic;
- a valid `CNA_NATIVE_LIBRARY` override took precedence over both the packaged asset and
  `CNA_NATIVE_DIR` and completed 60 frames.

This is `linux-x64` local acceptance evidence only. It does not make the RID officially shipped.
No empty Windows, macOS, or ARM package placeholders exist.

## Native loader policy proven by the harness

Resolution precedence is:

1. absolute `CNA_NATIVE_LIBRARY` (fail fast, no fallback);
2. absolute `CNA_NATIVE_DIR` containing exactly one recognized library (fail fast, no fallback);
3. application/package-native locations beside the managed application, including
   `runtimes/<rid>/native`.

The resolver does not search the source tree, bare system-library names, or accidental process-wide
loader paths. It admits only exact versions in the reviewed matrix, resolves all 854 imported
symbols, and executes core signature/shape canaries before returning the handle. Additional
unrelated exports are allowed. Ordinary diagnostics identify the selected configuration,
detected/consumer ABI when readable, policy, platform/RID and remediation. Set
`CNA_NATIVE_DIAGNOSTICS=1` only when low-level loader details are needed.

## Remaining release decisions

Before public packaging, approve native redistribution/licensing, package IDs and version policy,
signing, Source Link/reproducible-build policy, the qualified CNA revision/configuration for every
RID, and clean CI evidence on every claimed OS and architecture. The candidate matrix is
`eng/platform-matrix.json`; cross-compilation alone is never qualification.
