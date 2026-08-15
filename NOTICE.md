CNA.NET (`cna-dotnet`) is licensed under the Microsoft Public License (Ms-PL).

CNA.NET is a C#/.NET language binding for [CNA](https://github.com/openeggbert/cna),
a native C++ implementation of an XNA-inspired game framework. CNA.NET is
distributed separately from CNA and communicates with it only through CNA's
stable native interoperability boundary (see `docs/architecture.md`).

## Relationship to Microsoft XNA Framework

`CNA.XnaCompat` intentionally reuses namespaces originally introduced by
Microsoft's XNA Framework (for example `Microsoft.Xna.Framework`,
`Microsoft.Xna.Framework.Graphics`) so that existing XNA 4.0 game source code
can be recompiled against CNA.NET with little or no modification. This is a
source-compatibility facade implemented independently by the CNA project.

- CNA.NET is **not** produced, endorsed, or supported by Microsoft.
- CNA.NET does not include, redistribute, or link against any Microsoft XNA
  Framework binaries.
- "XNA" and "Microsoft" are used here only to describe API compatibility, not
  to claim affiliation.

## Relationship to Sharp Runtime

CNA (the native C++ engine this project binds to) may use
[Sharp Runtime](https://github.com/openeggbert/sharp-runtime) internally as a
C++ implementation dependency. Sharp Runtime is a C++23 library that provides
.NET-like APIs in native code — it is not the .NET CLR and does not execute
CNA.NET applications. CNA.NET applications run on a normal, unmodified .NET
runtime and use the real .NET Base Class Library. Sharp Runtime is never
exposed through CNA.NET's managed API surface. See
`docs/architecture.md` for the full explanation.

## Relationship to FNA

CNA itself is partly derived from and based on portions of
[FNA](https://fna-xna.github.io/), which is licensed under the Microsoft
Public License (Ms-PL), copyright 2009-2021 Ethan Lee and the MonoGame Team.
CNA.NET does not directly include FNA source code, but the compatibility goals
of `CNA.XnaCompat` are informed by FNA's and MonoGame's prior art in
XNA-compatible reimplementation.
