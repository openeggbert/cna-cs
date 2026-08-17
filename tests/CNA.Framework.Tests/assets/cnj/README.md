# `.cnj` test fixtures

`quad.cnj`/`quad_verts.bin`/`quad_idx.bin` reproduce the real openeggbert/cna C++ engine's own gtest
fixture (`modules/content/tests/Microsoft/Xna/Framework/Content/CnjModelTests.cpp`,
`WriteQuadModelFixture`/`LoadsRealCnjFixture`) byte-for-byte: a single-mesh, `BasicEffect`,
no-bones `.cnj` document referencing a `VertexPositionNormalTexture`-layout (stride 32) vertex
sidecar (4 vertices) and a 16-bit index sidecar (2 triangles, indices `0,1,2,0,2,3`). The vertex
data is a unit quad centered on the origin in the XY plane, facing `+Z`. Regenerated here, not
copied as opaque binary from that repository, since the source fixture is C++ test code (not a
binary asset) -- see `CnjModelReaderTests.cs` for the exact field values this reproduces.

`mismatched_type.cnj` is a minimal, hand-authored document whose `"type"` is `"SpriteFont"` --
exercises the same `"type"` envelope check the real fixture's own
`MismatchedTypeThrowsContentLoadException` test does, independent of any mesh content.
