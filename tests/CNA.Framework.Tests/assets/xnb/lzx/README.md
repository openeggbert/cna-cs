# LZX-compressed `.xnb` test fixtures

`Explosion.xnb`/`FontCalibri14.xnb` are real, LZX-compressed, MonoGame-compiled content assets
(`Texture2D`/`SpriteFont` respectively). Copied unmodified from `openeggbert/cna`'s own test
fixtures (`tests/assets/xnb/monogame/windows/lzx/`), which are themselves MonoGame content-pipeline
test output (MonoGame is MIT-licensed) -- see the sibling `assets/xnb/README.md` for the same
precedent this project's own uncompressed `.xnb` fixture already established.

`Explosion.xnb` decompresses to under 32KB, exercising a single-block LZX decode. `FontCalibri14.xnb`
decompresses to 44032 bytes -- more than one 32KB LZX block (32768 + 11264) -- deliberately picked
to exercise the block-framing loop's multi-block state persistence (the same `LzxDecoder` instance's
sliding window and repeated-offset LRU queue must carry over correctly across `Decompress()` calls).

## `reference-decompressed/`

`Explosion.decompressed.bin`/`FontCalibri14.decompressed.bin` are the **exact decompressed bytes
produced by FNA's own, unmodified `LzxDecoder.cs`**, run under Mono against the compressed payloads
inside the sibling `.xnb` files -- a genuinely independent cross-implementation check, not a
self-consistency check or a re-derivation of the algorithm from its own description. Copied
unmodified from `openeggbert/cna`'s own vendored copy (`tests/assets/xnb/monogame/windows/lzx/
reference-decompressed/`), which that project's own C++ `LzxDecoder` port is itself cross-verified
against byte-for-byte (SHA-256-identical, confirmed 2026-07-16 per that directory's own `README.md`).

`XnbLzxDecompressionTests.cs` asserts this project's own C# `LzxDecoder`/`XnbLzxDecompression`
(a direct port of that same C++ decoder) produces byte-identical output against these same two
reference files.
