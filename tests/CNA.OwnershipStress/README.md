# Native ownership stress runner

This executable is intentionally separate from xUnit. A native double free, stale callback, or
use-after-free terminates only this process, and every cycle destroys its game before constructing
the next one. Odd cycles abandon resources and force collection/finalization; even cycles dispose
the same resource graph explicitly and call `Dispose` twice. Every tenth cycle also throws from a
`GraphicsDevice.Disposing` handler and verifies that the exception returns only through managed
`Dispose`, native teardown still completes, and the next game can be created.

Run the complete 100-cycle gate on Linux with an ABI-compatible CNA library and display server:

```bash
CNA_NATIVE_LIBRARY=/path/to/libcna_c_api.so \
  CNA_OWNERSHIP_STRESS_FAMILY=all \
  xvfb-run -a dotnet run -c Release \
  --project tests/CNA.OwnershipStress/CNA.OwnershipStress.csproj -- 100
```

`CNA_OWNERSHIP_STRESS_FAMILY` may isolate `texture`, `batch`, `font`, `effect`, `sound`, `media`,
`storage`, `buffers`, `adopted`, or `content`. `all` exercises them together, including parent-owned
effect reflection objects, a transferred `Texture2D.FromStream` handle, and a content-managed model
graph. Success prints the exact explicit/finalizer split and `game-recreate=ok`.
