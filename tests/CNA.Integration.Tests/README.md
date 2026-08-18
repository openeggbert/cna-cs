# CNA.Integration.Tests

The only tests here that load the real native library and run a real game.

Everything else in `tests/` is managed-only. That matters more than it sounds: a
P/Invoke binding's characteristic failures — a struct whose managed layout does
not match the C one, a handle passed where a different handle was expected, a
callback with the wrong calling convention — all compile, all pass unit tests,
and all fail only when something actually calls across the boundary.

This project exists because 701 managed tests were green while the graphics API
could not work at all.

## Running

The native library is not part of this repository. Point at a build of
`openeggbert/cna`:

```
CNA_NATIVE_LIBRARY=/path/to/libcna_c_api.so dotnet test tests/CNA.Integration.Tests
```

or give the directory and let the resolver find the file:

```
CNA_NATIVE_DIR=/path/to/build/modules/c-api dotnet test tests/CNA.Integration.Tests
```

Without either, every test **skips** with the loader's own reason attached — not
fails, and not silently passes. A CI job on a machine with no engine build stays
green and says why.

The headless build variant is the right one for automation; it needs no display.

## Constraints these tests discovered

Both are properties of the runtime, not defects, and both are load-bearing for
anyone writing more tests here:

- **One game at a time.** `cna_game_create` answers
  `InvalidState: Only one C-owned CNA game may be active at a time` for a second
  concurrent game. xUnit parallelises test *classes* by default, so the moment
  there were two classes constructing a `Game`, nine of twelve tests failed at
  once. `AssemblyInfo.cs` disables parallelisation for the whole assembly.
- **A `GraphicsDevice` only exists inside a lifecycle callback.**
  `cna_game_get_graphics_device` fails outside one. Every graphics test therefore
  runs its body inside a real frame rather than standalone.

## What is deliberately pinned as broken

`Texture2D_GetData_ThrowsAndNamesWhatIsMissing` asserts a `NotSupportedException`.
It is meant to start failing: when the ABI gains the format query that route
needs, the test breaks and says to implement it. Ten "the C API cannot do this"
claims in this repository turned out to be false and had gone stale silently —
a test is how a documented gap gets a shelf life.

## Adding a test

Use `[NativeFact]`, not `[Fact]`, so it skips cleanly without the library. If it
touches graphics, run it through `InsideAFrame`. If a callback can throw, capture
the exception rather than letting it unwind through C — an exception crossing a
native frame is undefined behaviour, and the symptom is a dead test host with no
output rather than a failure message.
