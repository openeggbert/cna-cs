# XNA graphics/resource behavior probe

This executable captures device-backed XNA behavior that the pure compile probe cannot exercise:
state and collection identity, transfer limitations, dynamic-buffer options, SpriteBatch ordering,
effect reflection identity, draw validation, graphics resource lifecycle/events, and `Present`.
The identical `Program.cs` compiles against CNA.XnaCompat, Microsoft XNA 4.0, FNA, and MonoGame.
Comparator output is evidence of a difference, not authority for XNA behavior.

On Linux, run the CNA target inside a display server with a current CNA C ABI library:

```bash
CNA_NATIVE_LIBRARY=/path/to/libcna_c_api.so \
  XNA_GRAPHICS_PROBE_DRAW_VALIDATION=1 \
  XNA_GRAPHICS_PROBE_DESTRUCTIVE_LIFECYCLE=1 \
  XNA_GRAPHICS_PROBE_UNSAFE_CONSTRUCTORS=1 \
  xvfb-run -a dotnet run -c Release --project tests/CNA.XnaCompat.GraphicsProbe
```

Three destructive groups are explicit opt-in so a comparator defect cannot abort the remainder of
the corpus. FNA's current invalid-array draw path copies from a null vertex pointer, disposing an
active SpriteBatch and then ending it can fault in FNA3D, and invalid native constructor/readback
arguments can reach Vulkan with an invalid extent or staging-buffer size. With a variable unset,
the affected names are still emitted deterministically as `not-run(opt-in-required)`. The draw
group contains 29 names. The Windows XNA capture script enables all three groups; CNA CI should do
the same. These gates isolate process-fatal comparator behavior and do not treat FNA as XNA
authority.

## One-command Windows XNA snapshot

Use Windows with the XNA Framework Redistributable 4.0 Refresh, the .NET Framework 4.8 developer
pack, a working Direct3D display, and the seven legally obtained XNA Windows reference/runtime
assemblies. Do not copy those DLLs into this repository. From a PowerShell prompt at the repository
root, run:

```powershell
.\scripts\Capture-XnaSnapshots.ps1 `
  -XnaReferencePath 'C:\Program Files (x86)\Microsoft XNA\XNA Game Studio\v4.0\References\Windows\x86'
```

If the runtime assemblies are not installed in the GAC, also pass `-XnaRuntimePath` with their
external directory. Both probes are built as 32-bit .NET Framework 4.8 executables, then run
unchanged. Output is written below `artifacts/xna-snapshots/`:

- `xna-math-input-audio-content.txt` — 187 pure observations;
- `xna-graphics-resource.txt` — 166 device-backed observations;
- `xna-audio-xact-media-video-storage-lifecycle.txt` — 104 runtime observations;
- `xna-all.txt` — all 457 observations in source order.

Normalization retains only lowercase dotted `name=value` observation lines; preserves source order
and values verbatim; writes LF endings and UTF-8 without a BOM; and rejects an unexpected line
count. Renderer/log output is excluded.

Compare a capture with a reviewed golden file using either:

```powershell
.\scripts\Capture-XnaSnapshots.ps1 -XnaReferencePath $refs -CompareFile snapshots\xna-all.txt
```

or:

```powershell
git diff --no-index -- snapshots\xna-all.txt artifacts\xna-snapshots\xna-all.txt
```

The snapshot text contains only normalized test results and may be reviewed or checked in; the
Microsoft assemblies must remain external.
