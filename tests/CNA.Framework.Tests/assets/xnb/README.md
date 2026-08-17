# `.xnb` test fixtures

`BlenderDefaultCube.xnb` is a real, uncompressed, MonoGame-compiled `Model` content asset (embeds
real `ModelReader`/`VertexBufferReader`/`VertexDeclarationReader`/`IndexBufferReader`/
`StringReader`/`BasicEffectReader` type-reader names, confirmed via `strings`). Used to test
`CNA.Content.Xnb`'s real `.xnb` reader against real bytes, not just hand-constructed ones.

Copied unmodified from `openeggbert/cna`'s own test fixtures
(`tests/assets/xnb/monogame/windows/uncompressed/BlenderDefaultCube.xnb`), which are themselves
MonoGame content-pipeline test output (MonoGame is MIT-licensed).
