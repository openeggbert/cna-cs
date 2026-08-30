# Real-game compile probe

Compiles an unmodified XNA-API game's source against `CNA.XnaCompat`.

```
CNA_GAME_SOURCE_ROOT=/path/to/a/game/source \
  dotnet build tests/CNA.XnaCompat.GameCompileProbe -c Debug
```

## Why this exists next to the other compile probe

`tests/CNA.XnaCompat.CompileProbe` is a corpus written in this repository. It is good at what it
was written for -- pinning inheritance, interface and generic-assignment relationships that a
metadata diff cannot see -- and structurally incapable of finding a member nobody thought to use.
It contains exactly the API someone here chose to write down.

This compiles source nobody here chose. Against an 18,391-line Windows Phone XNA game ported to
MonoGame, the whole of it resolved except one call: `Mouse.SetCursor(MouseCursor.Arrow)`, which is
MonoGame's addition and not XNA 4.0. That is the kind of finding a self-written corpus cannot
produce, and one data point of it is worth more than a page of assertions about what should work.

## What it does and does not prove

It proves the facade's *shape*: every type, member, overload, constraint and conversion the game's
source names exists and binds the way the game expects. It proves nothing about behaviour -- a
compiled game can still draw the wrong thing, and the behaviour corpus is what covers that.

## Vendoring

Nothing. No game source is copied into this repository, and with no root configured the probe
compiles nothing and succeeds so the solution still builds. The build prints
`GAME_COMPILE_PROBE=not-configured` in that case, because an optional input that is absent must
say so rather than pass quietly.

## Choosing a game

Any source tree whose files compile against `Microsoft.Xna.Framework`. A tree with several projects
in it works: the probe globs `**/*.cs` under the root and excludes `bin`/`obj`, so point it at the
game's own sources rather than at a checkout that also contains a copy of FNA or MonoGame -- those
define the same namespaces and every type would then be ambiguous.
