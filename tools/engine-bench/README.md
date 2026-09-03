# EngineBench

One XNA 4.0 source file, compiled against three implementations, reporting frames per second for a
fixed workload with the timestep and vsync off — so the number is the framework's own cost rather
than a 60 Hz target being met.

```
Engine=CNA        CNA.XnaCompat (this repository)
Engine=MonoGame   MonoGame.Framework.DesktopGL 3.8.1.303
Engine=Kni        nkast.Xna.Framework 4.2.9001
```

`Program.cs` is compiled unchanged for all three. That is the entire design: any difference in the
result is a difference between the frameworks, because there is nothing else left to differ.

## Building and running

```bash
dotnet publish tools/engine-bench/CNA.EngineBench.csproj -c Release -p:Engine=CNA -o /tmp/eb-cna
CNA_NATIVE_LIBRARY=/path/to/libcna_c_api.so /tmp/eb-cna/EngineBench sprites 300
```

Release matters. A Debug build measures the JIT and the C++ optimiser, not the framework.

`scripts/Run-EngineBench.sh` wraps the whole comparison, including the headless X server.

### Workloads

| mode | what it draws |
|---|---|
| `loop` | the clear alone — the host loop and presentation, with no drawing |
| `tris` | 2 000 triangles through `DrawUserPrimitives`, one call per frame |
| `sprites` | 2 000 moving sprites through `SpriteBatch`, one texture |
| `spritesplit` | the `sprites` workload, timing the buffering loop apart from the flush |

Frames 1–30 are warm-up; shader compilation, buffer creation and JIT all land there. Every run also
prints `ALLOC bytesPerFrame=… gen0Collections=…` over the measured window.

`spritesplit` is the mode that answers "whose cost is this?". It reports:

```
SPLIT drawUsPerFrame=…   time inside the 2 000 batch.Draw calls  (managed)
      endUsPerFrame=…    time inside batch.End()                 (one native submit)
```

A managed-side regression moves the first number. It is the one that found the per-sprite
`cna_texture2d_get_info` transition this tool was written to chase — 735 µs/frame of it.

## Reading the result honestly

Three traps, all of which have already produced a wrong conclusion once.

**Check which renderer CNA actually selected.** It prints
`[INFO][RENDER] CNA: graphics renderer: …` at startup. `HEADLESS` validates every call and
maintains every counter and *rasterizes nothing*; a run on it is not comparable with MonoGame or
Kni, which draw. A C API library carries only the renderers it was compiled with, so
`CNA_GRAPHICS_RENDERER=OPENGLES3` fails outright rather than silently falling back when that
renderer is not in the build:

```
CNA::GraphicsRendererSelection: the OPENGLES3 renderer is not compiled into
this build. Available: HEADLESS.
```

**Check how the native library was built.** `libcna_c_api.so` is CNA's C++ core, and a Debug build
of it is several times slower than a Release one. MonoGame and Kni arrive as Release NuGet packages
either way, so a Debug library compares an unoptimised C++ core against optimised managed ones.

**Compare like with like, or say that you did not.** A CNA number from a Debug, `HEADLESS` library
next to a MonoGame number from a Release build doing real rasterization is two measurements, not a
comparison. Report the renderer and the build type alongside the score.

## What it is not

It is not a scored benchmark suite; `openeggbert/cna-benchmark` is that, for the C++ side. This is
a small instrument for one question — *what does the managed binding cost per draw call* — kept in
the repository whose answer it measures.
