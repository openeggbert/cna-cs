# XNA source-assignability compile probe

The source file in this project is the assertion. It exercises inheritance and invariant generic
contracts that a name/count API inventory cannot prove.

The solution builds it against CNA by default. The identical source can be compiled against other
available implementations:

```bash
dotnet build -p:CompatibilityTarget=CNA
XNA_REFERENCE_PATH=/path/to/xna/dlls dotnet build -p:CompatibilityTarget=XNA
FNA_FRAMEWORK_PATH=/path/to/FNA.dll dotnet build -p:CompatibilityTarget=FNA
dotnet build -p:CompatibilityTarget=MonoGame
dotnet build -p:CompatibilityTarget=Kni
```

XNA/FNA assemblies are supplied externally and are never committed. A passing target proves only
the relationships written in `XnaAssignabilityProbe.cs`; expand this corpus from metadata findings
rather than treating it as general API parity.

Measured on 2026-08-22: XNA, CNA, FNA, and MonoGame pass. Kni fails the
`VertexDeclaration`-to-`GraphicsResource` assignment because its public hierarchy differs from XNA;
do not conditionalize that assertion away when using this project as the strict corpus.
