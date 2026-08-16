# NEXT.md

> Session-by-session history of this repository, newest entry first. Written
> so a fresh context (new session, no memory of prior conversation) can read
> this file plus `plan.md` and pick up exactly where the last session left
> off, without re-deriving decisions that were already made and verified.
>
> For the *target architecture and phase plan*, read `plan.md`. This file is
> the *log of what actually happened*, including dead ends, corrections, and
> the reasoning behind non-obvious calls. If the two ever disagree, `plan.md`
> is normative for what to build next; this file is normative for why past
> decisions were made the way they were.

## Namespace correction: `CNA.Framework` → `CNA`; `PlayerIndex` moved to root (2026-08-16)

> Prompted by the user directly comparing this repo's namespaces against the
> real `openeggbert/cna` C++ source. Two separate, sequential fixes:

**1. Root idiomatic namespace renamed `CNA.Framework` → `CNA`.** A background
agent was sent to grep the actual C++ codebase for `namespace CNA` usage
before touching anything (see the sub-agent report inlined in the
conversation this was done in — not reproduced here, but the conclusion
was verified, not assumed). Confirmed: the C++ side uses `CNA::Graphics`,
`CNA::Input`, `CNA::Devices`, and bare `CNA::` (module `graphics-ext` →
namespace `CNA::Graphics`, `core` → bare `CNA::`) as its public
CNA-specific-extension namespace, parallel to
`Microsoft::Xna::Framework::*` for the XNA-compatible surface.
`CNA::Internal::*` is a *separate*, distinct namespace for private
implementation. There is **no `CNA::Framework::` namespace** in the C++
codebase at all — this project's earlier choice to nest the idiomatic layer
under `CNA.Framework.*` didn't match anything real on the C++ side.

Fix applied: renamed the C# **namespace** from `CNA.Framework`/
`CNA.Framework.Graphics`/`.Input`/`.Content` to `CNA`/`CNA.Graphics`/
`CNA.Input`/`CNA.Content`. The **project/assembly name stayed
`CNA.Framework`** — deliberately not renamed, because (a) `analysis_binding.md`
§18 prescribes exactly the project layout `src/CNA.Interop/`,
`src/CNA.Framework/`, `src/CNA.XnaCompat/`, and (b) the C++ side has the
exact same project-name-vs-namespace asymmetry (folder `modules/graphics-ext/`
→ namespace `CNA::Graphics`, not `CNA::GraphicsExt`). `CNA.Interop`'s project
name and namespace still match each other, because it plays the role of
`CNA::Internal::*` — a genuinely different, intentionally-private namespace,
not the public extension surface. This distinction is now codified as
`plan.md` invariant #8 and explained in `docs/architecture.md`'s "Layers"
section.

Mechanics: ~80 files touched by an *ordered* substring rename (most-specific
first: `CNA.Framework.Graphics`→`CNA.Graphics`, `.Input`→`CNA.Input`,
`.Content`→`CNA.Content`, then bare `CNA.Framework`→`CNA` with a Perl
negative-lookahead `(?!\/)` guard so the one doc-comment folder-path
reference — `.../src/CNA.Framework/Vector2.cs` — wasn't corrupted into a
non-existent path). Both `AssemblyInfo.cs` files were excluded from the bulk
rename by hand, since their `InternalsVisibleTo("CNA.Framework")` /
`InternalsVisibleTo("CNA.Framework.Tests")` attributes name *assemblies*
(unrenamed), not namespaces — a blind rename would have silently broken
internals access. Verified after: 0 warnings/0 errors across all 6 projects,
all 44 tests still pass.

**2. `PlayerIndex` moved from `CNA.Input`/`Microsoft.Xna.Framework.Input` to
the root `CNA`/`Microsoft.Xna.Framework` namespace.** Caught by the user
spot-checking the freshly-renamed layout against real XNA. Real XNA declares
`PlayerIndex` at the root, not in `.Input`, because it's shared between
`GamePad.GetState(PlayerIndex)` and the GamerServices/Storage APis (not
implemented in this repo yet) — tying it to `.Input` would have been wrong
even before the `CNA.Framework`→`CNA` rename. Audited every other type's
file-path-vs-declared-namespace after this fix (see the full table dump in
that session) — no other misplacements found. **Lesson recorded in
`plan.md`:** don't place a new type's namespace by where it "feels" like it
belongs; check real XNA's actual namespace for it first.

**Also:** added `.output.txt` and `assets_tmp/` to `.gitignore` — two files
that appeared in the working tree from an unrelated process (not authored by
work in this repository; contents were C++ template code and an MIT license,
neither belonging here) and were correctly *not* committed, just ignored
going forward.

## Full XNA math/value-type layer; `Mouse`/`GamePad` (2026-08-16)

> User explicitly asked to "expand to support the full XNA 4 API." Given the
> real scope of that (the analysis docs' own estimate: 4,000-8,000+
> agent-hours for "very broad" coverage, most of it impossible to make
> *behaviorally* real without the native ABI that doesn't exist upstream
> yet), this was scoped down via `AskUserQuestion` to: **do the pure
> math/value-type layer first** (zero native dependency, so it can be 100%
> real today, not a stub), plus `Mouse`/`GamePad` (same snapshot pattern
> already proven for `Keyboard`). The user picked that option explicitly.

Added to `CNA.Framework` and mirrored into `CNA.XnaCompat`:
`Vector3`, `Vector4`, `Quaternion`, `Matrix`, `Rectangle`, `Point`, `Ray`,
`Plane`, `BoundingBox`, `BoundingSphere`, `BoundingFrustum`, `MathHelper`,
the full 139-color XNA/X11 named-color table (`Color.Transparent` fixed to
match real XNA's white-with-zero-alpha value — it was black-with-zero-alpha
in the original scaffold), and the ~150-member `Keys` enum (Windows
virtual-key codes).

**Compat-layer pattern refined.** `Vector2`/`Color` (written in the original
scaffold) fully re-implement their formulas a second time in `CNA.XnaCompat`.
Every value type added in *this* session instead **duplicates only the
fields** (needed because C# structs can't inherit, and real XNA code
directly reads/writes fields like `matrix.M11`) and **delegates every
formula** to the `CNA`-namespace counterpart via the implicit conversion
operators — e.g. `public static Vector3 operator +(Vector3 a, Vector3 b) =>
(CNA.Vector3)a + (CNA.Vector3)b;`. This eliminates the risk of the two
copies of a formula silently drifting apart, at the cost of being a
different pattern from the first two types (documented as an intentional,
accepted inconsistency in `plan.md` invariant #3 — not worth retrofitting
Vector2/Color, which already ship and are tested). `BoundingFrustum` needed
no duplication at all beyond a `GetCorners()` array-covariance workaround,
because it's a *class* in real XNA (not a struct), so `CNA.XnaCompat`'s
version is a genuine subclass.

**`Matrix.Invert`** uses Gauss-Jordan elimination with partial pivoting
(`Matrix.cs`), not a hand-expanded cofactor formula — deliberate choice,
because a standard textbook algorithm is easier to verify correct by
inspection than trusting a memorized closed-form expansion. Verified anyway,
not just trusted: `MatrixTests.Invert_ProducesMultiplicativeIdentity` checks
`M * Invert(M) ≈ Identity` across 9 matrices including
`CreateLookAt`/`CreatePerspectiveFieldOfView` compositions.

**`BoundingFrustum` plane extraction** is derived from this project's own
row-vector `Matrix`/`Vector4` transform convention (worked out from first
principles in the code comments — clip-space half-space coefficients for
`v * M`, not copied from another XNA implementation's source, since the
convention needed to match *this* project's own already-implemented
`Matrix`/`Vector3.Transform`, not some external reference that might use a
different convention). Corners are computed *independently*, by unprojecting
the NDC cube through `Matrix.Invert`, specifically so the two derivations
(planes vs. corners) cross-check each other rather than sharing a single
possibly-wrong assumption. `BoundingFrustumTests` checks containment
(inside/outside points, spheres) and near-vs-far corner ordering.

**`Mouse`/`GamePad`** needed two new `CNA.Interop` natives
(`cna_mouse_get_state`, `cna_gamepad_get_state`) — same snapshot-struct
pattern as the existing `cna_keyboard_get_state`. `Buttons` (the `[Flags]`
enum) covers only the core d-pad/face/shoulder/stick-click bits; XNA's
additional flags for representing thumbstick directions and trigger pulls as
pseudo-buttons were deliberately left out (lower confidence in the exact bit
values from memory, low real-world usage, and `GamePadState.ThumbSticks`/
`.Triggers` already expose that data properly).

**Verification method for this whole session:** a working local .NET SDK was
*found* on the machine (`/tmp/racinggame-dotnet` — .NET 8.0.423 runtime,
`/tmp/platformer-dotnet/sdk` — .NET 9.0.316 SDK-only) — not installed by
this session, and not something to rely on being there in a future session.
Every batch of new/changed code in this session was actually built and
tested with it before committing (`dotnet build CNA.sln` / `dotnet test
CNA.sln`), not just written and assumed correct — this caught real mistakes
immediately (e.g. `BoundingFrustum : sealed class` in `CNA.Framework`
blocking the `CNA.XnaCompat` subclass; a member/type name collision between
the `GamePadState.Buttons` property and the `Buttons` enum type, fixed with
full qualification `CNA.Input.Buttons`). **If a future session doesn't have
a working `dotnet` available, say so explicitly rather than claiming
verification that didn't happen** — see `plan.md` "Toolchain note".

Final state this session: 44/44 tests passing, 0 warnings/0 errors across
all 6 projects.

## Initial scaffold (2026-08-15)

> First real content in this repository (previously just an empty `.git`).
> Built from `../cnabinding/analysis_binding.md`,
> `../cnabinding/analysis_binding_sharp_runtime.md`, and
> `../cna/analysis_binding_languages.md` — read closely, not skimmed, since
> the whole point of this repo is to follow that architecture precisely
> rather than invent a plausible-looking alternative.

Created: `plan.md`, `README.md`, `LICENSE` (Ms-PL, matching `openeggbert/cna`),
`NOTICE.md` (clarifies no Microsoft affiliation and, importantly, that Sharp
Runtime is not a CLR and does not execute `CNA.NET` applications — see
`analysis_binding_sharp_runtime.md` §130-131, which practically hands you
this paragraph), `.gitignore`, `.editorconfig`, `docs/architecture.md`,
`docs/xna-compatibility.md`.

`CNA.sln` + three SDK-style projects (`CNA.Interop`, `CNA.Framework`,
`CNA.XnaCompat`) targeting `net8.0`, plus `samples/HelloGame` (the exact
reference example from `analysis_binding.md` §38/§140) and two xunit test
projects. `.vscode/` added so VS Code works without extra setup; no
per-IDE project format needed since SDK-style `.csproj` is read directly by
Visual Studio, Rider, VS Code (C# Dev Kit), and the `dotnet` CLI alike.

**Architecture established, since reused unchanged through every later
session:**

- `CNA.Interop` (project) — fully `internal`, the *only* place allowed to
  reference native symbols. `LibraryImport`-based P/Invoke over a minimal
  slice of the CNA C ABI shape (the ABI itself doesn't exist upstream yet —
  this project builds against the *shape* the analysis docs specify, so it
  only needs signature fix-ups once the real ABI ships, not a redesign).
- `CNA.Framework` (project) — idiomatic CNA .NET API. Local (non-P/Invoke)
  math value types; `SafeHandle`-based (`NativeResourceHandle`, a
  general-purpose wrapper parameterized by a release callback) native
  resource lifetime; `Game`'s native callback bridge via
  `[UnmanagedCallersOnly]` static methods resolving a `GCHandle` back to the
  managed instance — this is the trickiest single piece of code in the
  repo and got the most design attention (see `Game.cs`).
- `CNA.XnaCompat` (project) — the real `Microsoft.Xna.Framework` namespace
  facade. Reference types subclass their `CNA.Framework`-project
  counterparts (later: `CNA`-namespace counterparts, see the 2026-08-16
  entry above) with zero duplicated logic; **never** references
  `CNA.Interop` directly — enforced at compile time by
  `InternalsVisibleTo` only being granted from `CNA.Interop` to
  `CNA.Framework`, not transitively to `CNA.XnaCompat`.

**Validated by actually running it**, not just by compiling: `HelloGame`
throws `DllNotFoundException` for `cna-native` from exactly the point the
design predicts (`Game`'s constructor, at `cna_managed_game_create`) —
confirming the whole callback-bridge scaffolding, including the
`UnmanagedCallersOnly`/function-pointer marshalling for the tricky
`CnaManagedGameCallbacks` struct, is wired correctly end to end, ahead of
the native ABI existing to actually run against.

## Where to pick up next

Read `plan.md` Phase 4 ("Native-backed, not started") for the actual task
list: `SpriteFont`, `RenderTarget2D`, `BasicEffect`/`Effect`, 3D (`Model`,
`VertexBuffer`, `IndexBuffer`), audio (`SoundEffect`, `SoundEffectInstance`,
`Song`, `MediaPlayer`), and the extra `SpriteBatch.Draw` overloads. **All of
it is blocked on `openeggbert/cna` shipping its C ABI** (`modules/c-api/` —
does not exist there as of this writing; check before assuming otherwise).
Two honest options for a future session:

1. **Keep building against the ABI *shape*** the analysis docs specify (the
   same approach this repo has used throughout) — reasonable for
   `SpriteFont`/`RenderTarget2D`/extra `SpriteBatch.Draw` overloads, which
   are natural extensions of the already-proven `Texture2D`/`SpriteBatch`
   pattern. Riskier for `Effect`/`Model`/audio, where the ABI shape is far
   less specified in the analysis docs and more likely to need real rework
   once `openeggbert/cna` actually ships something.
2. **Check `openeggbert/cna` first** for whether `modules/c-api/` has
   landed. If it has, this repo's Phase 1 (`CNA.Interop`) needs a real
   signature audit against the shipped ABI before anything else — the
   analysis-doc-shaped signatures in `Native.cs` were never validated
   against a real implementation.

Either way: **use the local `.dotnet` SDK check from the 2026-08-16 session
as the template** — actually build and test every change before claiming it
works, don't just write code and assume. If no working `dotnet` is available
in a future sandbox, say so plainly rather than presenting unverified code
as verified.
