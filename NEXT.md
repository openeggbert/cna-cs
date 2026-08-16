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

## Extended `SpriteBatch.Draw` overloads; `RenderTarget2D` (2026-08-16, session 4)

> Continuation of Phase 4 per the user's "keep working through the plan"
> instruction. Picked the two items NEXT.md's previous "Where to pick up"
> section flagged as lower-risk (natural extensions of the already-proven
> `Texture2D`/`SpriteBatch` pattern), in that order, after a research pass
> confirmed exactly what doc backing each one actually has — see below,
> because it turned out to be less than plan.md previously implied.

**Toolchain fix, worth keeping for future sessions:** the machine had two
independent local .NET installs — `/tmp/platformer-dotnet/sdk` (SDK, .NET
9.0.316 runtime only) and `/tmp/racinggame-dotnet` (.NET 8.0.29 runtime
only, no SDK). `dotnet build` worked fine with just the SDK one on `PATH`
(it can compile against a referenced net8.0 target without a matching
runtime installed), but `dotnet test` failed — `vstest`'s testhost launch
resolves the runtime relative to wherever the SDK's own `dotnet` muxer
lives, ignoring `DOTNET_ROOT`/`DOTNET_HOST_PATH` overrides entirely (tested
directly: setting both had no effect). Fix:
`ln -s /tmp/racinggame-dotnet/shared/Microsoft.NETCore.App/8.0.29 /tmp/platformer-dotnet/sdk/shared/Microsoft.NETCore.App/8.0.29`
— one symlink, dropped straight into the found SDK's own `shared/` folder
so its own muxer sees both runtimes via `dotnet --list-runtimes`. After
that, `dotnet test CNA.sln` ran both test projects normally. Neither
`/tmp/...` path was created by this session and neither should be assumed
present in a future one — re-locate a working SDK+runtime pair the same way
the 2026-08-16 (session 2) entry below did, and re-apply this symlink trick
if `dotnet test` fails the same way.

**Research finding that changed the plan:** plan.md's Phase 4 list grouped
`SpriteFont`/`RenderTarget2D`/extra `SpriteBatch.Draw` overloads together as
"natural extensions... reasonable to build against the ABI shape." A
full-text grep of both `analysis_binding.md` and
`analysis_binding_sharp_runtime.md` (not a skim) found this is only true for
one of the three: **§22's `CNA_SpriteDrawCommand` example struct** gives a
concrete, usable field shape for the extended `Draw` primitive. `SpriteFont`
and `RenderTarget2D` have **zero** ABI detail anywhere in either doc — not
even a rough sketch — they're status-table/checklist entries only. This
matters for how much to trust what got built this session: the `Draw`
overloads are shape-verified against a real doc citation; `RenderTarget2D`'s
two native functions are this session's own invention, no better-grounded
than a guess at the conventions, and should get extra scrutiny once Track A
ships — flagged accordingly in `plan.md` and in code comments on
`RenderTarget2D.cs`/`Native.cs`.

**Extended `SpriteBatch.Draw`:** added `CnaRect` and `CnaSpriteDrawCommand`
(`CNA.Interop/NativeStructs.cs`, the latter matching §22's struct
field-for-field) and one new native primitive,
`cna_sprite_batch_draw_ex(CnaHandle spriteBatch, in CnaSpriteDrawCommand)`.
Every new `Draw` overload in `CNA.Graphics.SpriteBatch` funnels through this
one native call via two private `DrawEx` helpers — one taking
position+scale (the primitive), one taking a destination rectangle (resolves
to position+scale in C#, no native call of its own) — rather than adding a
native function per overload, continuing the "minimal native surface, C#
handles convenience overloads" approach already used for the math value
types. Deliberately **no "has source rectangle" flag** in the struct (the
doc's §22 example doesn't have one either): "no source rectangle given"
resolves to a concrete `Rectangle(0, 0, texture.Width, texture.Height)` at
the C# call site before the struct is built, so the ABI shape needed nothing
beyond what the doc already showed.

Added `CNA.Graphics.SpriteEffects` (`[Flags] { None, FlipHorizontally,
FlipVertically }`) — the docs (§52) name only `FlipHorizontally`, in a
naming-parity example, no bit values anywhere, so real XNA 4.0's actual
values were used from memory, not derived from this project's own source
material. Mirrored into `CNA.XnaCompat` as a numerically-identical but
*distinct* enum type (C# forbids user-defined conversion operators on
enums), same pattern as `Keys`/`Buttons` — parity now tested in
`CompatibilityTests.SpriteEffects_NumericValuesMatchFrameworkSpriteEffects`,
mirroring the existing `Keys` parity test.

**XnaCompat inheritance detail worth remembering:** most of the new `Draw`
overloads needed **zero** code in `CNA.XnaCompat.SpriteBatch` — they're
inherited unchanged from `CNA.Graphics.SpriteBatch` because `Rectangle?`
(nullable) converts through the *lifted* form of `Rectangle`'s existing
implicit conversion operator automatically; C# does this for any
`Nullable<T>` where `T` has a user-defined conversion, no extra code needed.
Only the three overloads with a `SpriteEffects` parameter needed an explicit
override (cast `(CNA.Graphics.SpriteEffects)(int)effects` before calling
`base.Draw(...)`), because that parameter is a same-shaped-but-distinct enum
type, not something with a conversion operator. Worth remembering next time
a new `Draw`-shaped overload is added: check whether every parameter type
already has a conversion path before assuming an override is needed.

**`RenderTarget2D`:** two new native functions, `cna_render_target2d_create`
and `cna_graphics_device_set_render_target` — both **invented for this
repository**, see the research finding above. Deliberately does *not* get
its own release/width/height native functions: the handle it wraps is
texture-shaped (created through a render-target-specific factory, but
otherwise an ordinary texture on the native side), so `CNA.Graphics.
RenderTarget2D` subclasses `Texture2D` and reuses its existing
`cna_texture2d_release`/`get_width`/`get_height` calls unchanged.

Hit a real design fork on the `CNA.XnaCompat` side, worth recording because
it'll recur for any future type where a *derived* native-backed type needs
an XnaCompat mirror: real XNA has `RenderTarget2D : Texture2D`, and C#
single inheritance means `Microsoft.Xna.Framework.Graphics.RenderTarget2D`
can extend `CNA.Graphics.RenderTarget2D` (preserving the *native-creation*
lineage) **or** `Microsoft.Xna.Framework.Graphics.Texture2D` (preserving the
*compat-layer* lineage so `Texture2D t = someRenderTarget;` compiles in game
code, which is the whole point of `CNA.XnaCompat` existing) — not both,
because `CNA.Graphics.RenderTarget2D` and `CNA.XnaCompat`'s `Texture2D` are
siblings, not ancestor/descendant. Chose the compat-layer lineage (extends
XnaCompat's own `Texture2D`) as the more important XNA-compatibility
guarantee to preserve, and moved the native-handle-creation logic into an
`internal static CreateNativeHandle(...)` method on `CNA.Graphics.
RenderTarget2D` that both sides call — reusable across the assembly boundary
without violating invariant #5 (it returns a raw `nint`, not any
`CNA.Interop` type) because `CNA.Framework`'s `AssemblyInfo.cs` already
grants `CNA.XnaCompat` an `InternalsVisibleTo` (confirmed by reading it, not
assumed — this is *not* the same grant chain as
`CNA.Interop`→`CNA.Framework`, and doesn't violate "XnaCompat never
references CNA.Interop directly," since no CNA.Interop type crosses that
call). Same fork forced `GraphicsDevice.SetRenderTarget` to accept
`CNA.Graphics.Texture2D?` instead of the stricter `CNA.Graphics.
RenderTarget2D?` real XNA's signature would suggest — documented as a
deliberate, narrow compatibility looseness in that method's doc comment,
traded for zero extra code needed in `CNA.XnaCompat.GraphicsDevice` (the
compat `RenderTarget2D` upcasts straight into the looser parameter, same
"inherited unchanged, converts through the type hierarchy" pattern as every
other compat method).

**Verified, not just written:** every change built and tested with the
locally-found SDK before committing — `dotnet build CNA.sln` (0
warnings/errors across all 6 projects), `dotnet test CNA.sln` (47/47 passing,
up from 44 — added the `SpriteEffects` parity test, no other new tests: the
native-backed `Draw`/`RenderTarget2D` code paths can't be exercised without
an actual `cna-native` library, same limitation `Texture2D`/`SpriteBatch`/
`Mouse`/`GamePad` already had, so no test coverage was invented for logic
that can't actually run yet), and `dotnet run --project samples/HelloGame`
still fails at exactly the same documented point
(`DllNotFoundException` for `cna-native` inside `Game`'s constructor) as
every prior session — confirms nothing in this session's changes altered
that code path, since none of it touches `Game`/`GraphicsDeviceManager`.

**`SpriteFont` (same session, right after the above):** turned out to need
*zero* new native ABI surface, better than the design sketch this section
originally predicted (see the crossed-out plan below, kept because the
reasoning that got here is worth keeping). The unlock: real XNA 4.0's
`SpriteFont` has a **public constructor** — `SpriteFont(Texture2D texture,
List<Rectangle> glyphBounds, List<Rectangle> cropping, List<char> characters,
int lineSpacing, float spacing, List<Vector3> kerning, char?
defaultCharacter)` — meant for third-party font-building tools, not just
XNA's own content pipeline. Reproducing that constructor field-for-field
(`CNA.Framework/Graphics/SpriteFont.cs`) means the whole glyph table lives
in plain managed arrays from the moment a `SpriteFont` exists, with no FFI
boundary in the object model itself. That makes `MeasureString` pure
managed code — real unit tests today, no native dependency, same as
`Vector2`/`Matrix` — and `SpriteBatch.DrawString` a thin loop over the
`Draw` primitive from earlier this session (one `Draw(texture, position,
sourceRectangle, ...)` call per glyph, no dedicated native draw-string
call needed).

Implementation notes:
- `MeasureString` and the glyph-placement walk `DrawString` uses share one
  private `Walk` method (`SpriteFont.cs`) rather than duplicating the
  ABC-kerning-triple (`Vector3(leftBearing, width, rightBearing)`) +
  cropping-rectangle traversal — this is the standard XNA/MonoGame bitmap
  font algorithm, not invented here, but it's also not been checked against
  a real XNA binary (none available in this environment). Verified instead
  with hand-worked expected values for several short strings (single glyph,
  two glyphs with spacing, a newline) in `SpriteFontTests.cs` — the numbers
  were computed by hand from the same formula being tested, so this catches
  *regressions* in the walk logic, not disagreement with real XNA's actual
  output; say so plainly if this ever needs auditing against a real engine.
- `DrawString`'s rotation/scale/origin apply to the *whole string* as one
  rigid body, not per-glyph independently. Implemented by offsetting each
  glyph's own `Draw` call's `origin` parameter by that glyph's placement
  anchor (`origin - placement.Anchor`) rather than by pre-transforming each
  glyph's position — the same trick a single `Draw` call's `origin`
  parameter already performs, just applied once per glyph. Known
  incompleteness, flagged in the code: doesn't implement XNA's
  `SpriteEffects`-driven character/line reversal for flipped text (flip
  effects currently just flip each glyph sprite in place).
- Testing needed a dummy `Texture2D` with no working native library behind
  it. Solution: `new Texture2D(nativeHandleValue: 0)` — handle value `0` is
  what `NativeResourceHandle.IsInvalid` treats as invalid, and `SafeHandle`
  never calls the release callback for an invalid handle, so disposal (or
  GC finalization, if the test never disposes it) never touches native
  code. This works from `CNA.Framework.Tests` because that project already
  has the `protected internal` raw-handle constructor's `internal` half
  granted via `CNA.Framework`'s `InternalsVisibleTo` — but **not** from
  `CNA.XnaCompat.Tests`, which only gets that grant transitively through
  `CNA.XnaCompat` itself, not extended to its own test project. That's why
  there's no XnaCompat-layer runtime test for `SpriteFont` this session —
  matches the existing precedent that `Texture2D`/`SpriteBatch`/`Mouse`/
  `GamePad` don't have XnaCompat runtime tests either, for the same reason.
- `CNA.XnaCompat`'s `SpriteFont` needed a `new Texture2D Texture { get; }`
  property hiding the base class's `CNA.Graphics.Texture2D`-typed one — the
  first place in this codebase a compat subclass needed to hide (not just
  inherit-unchanged) a property, because `Texture` is the one XNA
  `SpriteFont` member whose declared type actually differs between the two
  namespaces. Worth remembering as a precedent if a future type has the
  same shape (a property whose value is always actually a compat-typed
  instance, but whose base-declared type is the CNA.Framework one).

<details>
<summary>Original (2026-08-16, pre-`SpriteFont`) design sketch — kept for
the reasoning, superseded by what's above</summary>

Unlike everything else done in Phase 4 so far, there is no doc shape to
build against at all (confirmed by grep, see above) — this needs an actual
small ABI design, in the spirit of §8/§9's conventions (opaque handles,
`CnaResult`, fixed-width primitives, generation-checked handles) but
genuinely new. Worth considering before starting:
- Real XNA/MonoGame's `SpriteFont` does *not* need its own native draw
  call — `DrawString`/`MeasureString` are pure managed-code loops over a
  per-character glyph table (source rect into a font atlas texture +
  advance width + per-character-pair kerning), calling the *existing*
  `SpriteBatch.Draw(texture, sourceRect, ...)` primitive once per character.
  That primitive already exists as of this session, which is exactly why
  this was sequenced after it, not before.
- So the only new native surface needed is probably: however font *data*
  crosses the ABI (an atlas `Texture2D` handle, likely reusable as-is, plus
  a glyph table — could ride through `ContentManager.Load<SpriteFont>`
  exactly like `cna_content_load_texture2d` already works, or need its own
  `cna_content_load_spritefont` if the glyph table doesn't fit that call's
  shape) and however the glyph table itself is retrieved (a single call
  returning a fixed-format buffer of per-glyph structs — character code,
  source rect, advance width — is the shape to reach for first; kerning
  pairs are the one part of real XNA's `SpriteFont` that's genuinely
  optional/lower-value to implement first).
- This is real ABI design work, not doc-shape-following — say so plainly if
  picking this up, the same way this file has said so plainly about
  `RenderTarget2D` above, rather than presenting an invented shape as if it
  had more grounding than it does.

*(This is what got predicted before actually reading real XNA's `SpriteFont`
constructor signature closely enough to notice the public-constructor
escape hatch above. Left in place as a reminder: check whether the "obvious"
hard case is actually hard before designing new ABI surface for it.)*
</details>

**Where to pick up next:** `ContentManager.Load<SpriteFont>` (how font data
crosses the FFI boundary, still genuinely open, no doc backing — see above),
then `plan.md` Phase 4's remaining items (`Effect`/`Model`/3D/audio), which
are explicitly flagged riskier than everything done so far — the analysis
docs specify even less for those than they did for `SpriteFont`'s ABI-free
path.

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
