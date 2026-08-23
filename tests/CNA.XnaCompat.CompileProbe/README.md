# XNA source-assignability compile probe

The source files in this project are the assertion. `XnaAssignabilityProbe.cs` exercises inheritance
and invariant generic contracts that a name/count API inventory cannot prove. The executable
`MathBehaviorCorpus` emits stable IEEE-754 bit-pattern observations for math, color, and geometry,
`InputBehaviorCorpus` records deterministic keyboard, mouse, game-pad, and touch value semantics,
`AudioBehaviorCorpus` records device-independent audio enums/defaults/validation/arithmetic, and
`ContentErrorCorpus` exercises compact in-memory malformed XNB/error paths. Capture their combined
output for each engine and diff the resulting text files.

The solution builds it against CNA by default. The identical source can be compiled against other
available implementations:

```bash
dotnet build -p:CompatibilityTarget=CNA
XNA_REFERENCE_PATH=/path/to/xna/dlls dotnet build -p:CompatibilityTarget=XNA
FNA_FRAMEWORK_PATH=/path/to/FNA.dll dotnet build -p:CompatibilityTarget=FNA
dotnet build -p:CompatibilityTarget=MonoGame
dotnet build -p:CompatibilityTarget=Kni
```

Run the CNA corpus locally with:

```bash
dotnet run -c Release -p:CompatibilityTarget=CNA
```

The native C++/CLI XNA runtime cannot execute on Linux even though the probe compiles there. Capture
the XNA snapshot on a Windows machine with XNA 4.0 installed; FNA, MonoGame, and Kni snapshots can
be captured wherever those runtimes support the selected target.

XNA/FNA assemblies are supplied externally and are never committed. A passing target proves only
the relationships and observations explicitly written in this project; expand both corpora from
metadata and differential findings rather than treating them as general API parity.

Measured on 2026-08-23: CNA and FNA compile; MonoGame compiles this pure project while reporting
XACT `RendererDetail` as absent at runtime. The executable contains 187 observations: 83
math/geometry, 23 input, 47 Audio, and 34 Content. CNA emits all 187. FNA emits all 187 normalized
observations, then its own SoundEffect finalizer aborts because its native audio library is absent.
MonoGame aborts during audio initialization after the old 106 lines. Together with 166 graphics/
resource and 104 native runtime lines, the complete CNA snapshot contains 457 observations. Direct
XNA source/IL adjudicates known behavior; the net48/x86 target and capture remain pending on a
proper Windows XNA environment. Alternate engines remain comparators, not authorities. Kni still
fails the `VertexDeclaration`-to-`GraphicsResource` assignment because its public hierarchy differs
from XNA; do not conditionalize that assertion away when using this project as the strict corpus.
