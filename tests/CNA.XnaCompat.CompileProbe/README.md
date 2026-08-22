# XNA source-assignability compile probe

The source files in this project are the assertion. `XnaAssignabilityProbe.cs` exercises inheritance
and invariant generic contracts that a name/count API inventory cannot prove. The executable
`MathBehaviorCorpus` emits stable IEEE-754 bit-pattern observations for math, color, and geometry,
while `InputBehaviorCorpus` records deterministic keyboard, mouse, game-pad, and touch value
semantics. Capture their combined output for each engine and diff the resulting text files.

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

Measured on 2026-08-22: XNA, CNA, FNA, and MonoGame compile. The combined 106-observation executable
corpus (83 math/geometry and 23 input) runs on CNA, FNA, and MonoGame. Direct XNA source/IL was used
to adjudicate arithmetic grouping, matrix/viewport, color/packed-value, geometry, curve, hash,
string, and input-construction differences; alternate engines remain comparators, not authorities.
The Windows XNA runtime snapshot is still open because the installed XNA assemblies use a C++/CLI
module initializer that cannot execute on Linux. Kni fails the `VertexDeclaration`-to-
`GraphicsResource` assignment because its public hierarchy differs from XNA; do not conditionalize
that assertion away when using this project as the strict corpus.
