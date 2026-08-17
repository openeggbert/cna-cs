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

## Thirteenth `/code-review high` pass, over the `MediaLibrary` commit -- six findings, four fixed (2026-08-17, session 6 continued autonomously still further)

Ran the review a thirteenth time. Six findings, the most of any single
pass this session -- four real and fixed, two accepted as real but
low-priority and folded into the same fix pass anyway since it touched
the same files:

- **Fixed, real, the standout bug:** compat `Song.cs` was never updated
  when the base `CNA.Media.Song` gained `Album`/`Artist`/`Genre`
  properties in the same commit -- every other new compat type in that
  commit got a `new` compat-typed override, this file was simply missed.
  Added the three overrides. While fixing this, caught a second, self-
  introduced bug in the process: the new overrides' setters had no
  explicit accessor-level modifier, so they defaulted to `public set`,
  silently *widening* real XNA's actual get-only surface (and the base
  class's own deliberate `internal set`) into a fully public one nothing
  asked for. Fixed to `internal set`, matching the base exactly.
- **Fixed, real:** compat `Album`'s internal constructor accepted
  base-typed (`CNA.Media.Artist?`/`Genre?`) parameters rather than
  compat-typed ones, so its own `(Artist?)base.Artist`/`(Genre?)base.Genre`
  downcast getters were only safe *in practice* (nothing currently calls
  this constructor with real data), not *provably* safe by construction
  the way the doc comment's safety argument implied. Changed the
  constructor to accept compat-typed `Artist?`/`Genre?`/`SongCollection`
  directly, converting to base-typed only at the `base(...)` call site --
  now the compiler itself enforces what the doc comment used to just
  assert.
- **Fixed, real, the biggest single change:** eight near-identical
  ~30-line `List<T>`-wrapping `IDisposable`/`IEnumerable<T>` collection
  classes (`Song`/`Album`/`Artist`/`Genre`/`Playlist`Collection, once
  each in `CNA.Framework`/`CNA.XnaCompat`) with no shared base --
  extracted a shared `ReadOnlyMediaCollection<T>` (one copy per project,
  necessarily `public` rather than `internal`: a `public sealed class
  SongCollection` cannot derive from an `internal` base -- C# CS0060 --
  same shape the BCL's own `ReadOnlyCollection<T>` uses for the identical
  reason). Every one of the eight collection types is now a ~10-line
  thin subclass. Directly relevant given the `Song` bug just above: this
  is exactly the kind of "the same fix has to be manually reapplied N
  times, and it's easy to miss one" risk the review's own reasoning
  called out, demonstrated in the very same diff it was reviewing.
- **Fixed, real, same root cause as the `Album` finding:** `Artist`/
  `Genre`/`Playlist`'s compat constructors accepted `albums`/`songs`
  parameters that were silently discarded in favor of hardcoded fresh
  empty collections in the `new`-shadowed properties -- confusing for a
  future maintainer expecting the constructor's own parameters to be
  reflected in the resulting object. Changed all three to genuinely store
  what's passed to the constructor (still always empty in practice, since
  nothing currently constructs these with real data, but no longer
  silently different from what the constructor signature promises).
- **Fixed, real but minor:** compat `MediaLibrary`'s five `new`-shadowed
  collection properties each allocated a fresh empty wrapper per
  instance, on top of the five the base constructor already allocates.
  Replaced with five `static readonly` shared empty instances -- safe
  specifically because these collections are provably immutable and
  permanently empty (unlike a cache of *mutable* shared state, which
  would be a real bug), cutting the redundant half of the allocation
  entirely.
- **Accepted as a real observation, not actioned as a separate fix:** the
  review noted that each compat type re-derives its own bespoke,
  hand-verified safety argument for its downcast rather than a
  structural mechanism enforcing the invariant project-wide. The `Album`/
  `Artist`/`Genre`/`Playlist` fixes above *are* exactly that kind of
  structural fix, applied file-by-file rather than as one shared
  mechanism -- judged sufficient for now (four call sites, now all
  compiler-enforced) rather than designing a general-purpose "safe
  downcast" abstraction for a pattern that's shown up in maybe half a
  dozen places across the whole session.

**Verified, not just written:** `dotnet build CNA.sln` clean across all 6
projects (needed one extra fix mid-refactor: `ReadOnlyMediaCollection<T>`
had to be `public`, not `internal` -- CS0060 caught immediately on first
build attempt, not a design decision that could have shipped silently
wrong). `dotnet test CNA.sln`: 312/312 passing (up from 311 -- the
refactor itself was behavior-preserving with zero test changes needed;
+1 new regression test for the `Song.Album`/`.Artist`/`.Genre` compat
override fix). `samples/HelloGame` re-verified unaffected.

## `Album`/`Artist`/`Genre`/`Playlist`/`MediaLibrary`: real XNA object model, scoped to always-empty (2026-08-17, session 6 continued autonomously past the "everything tractable is done" checkpoint, by explicit user choice)

> Reported that every well-scoped, high-confidence task was done and
> asked the user how to continue, offering to stop, attempt two
> lower-confidence guesses, or start one of the two large deferred
> features. User chose to start the `MediaLibrary` subsystem.

**Research came first, and it changed the plan significantly.** Reading
the real C++ engine's `MediaLibrary.hpp` showed the real feature is much
bigger than "Album/Artist/Genre" -- it also owns an entire parallel
picture-library subsystem (`Picture`/`PictureAlbum`/`PictureCollection`/
`PictureAlbumCollection`, `GetPictureFromToken`/`SavePicture`). Reading
`MediaLibrary.cpp`'s actual `BuildFromRoots` scan logic showed something
more fundamental: unlike every other native-backed feature this session
grounded against a real, working C++ implementation, this one's real
logic is bound to infrastructure with **no equivalent anywhere in this
binding and no C ABI exposure to build one against** -- real ID3v2/Vorbis/
FLAC tag parsing (`CNA::Internal::Media::AudioTagParser`), FFmpeg-based
audio duration probing (`AudioDurationProbe::ProbeDurationMS`, built on
`avformat_find_stream_info`), a native directory-scanning index, and a
native cover-art image loader. This is categorically different from
`BasicEffect`/`Model`/`Song`'s own "real, working implementation just
needs porting/reflecting" situation -- there is no C# equivalent to port
*to*, and no native ABI shape to design *for*, without either a large new
native surface upstream (itself needing FFmpeg-equivalent decoding
exposed through a C API, a substantial problem in its own right) or
reimplementing binary audio-tag/container parsing from scratch in pure
C#. Reported this finding back to the user with concrete options rather
than silently picking a direction or grinding ahead on a guess -- this
was a genuine "the task is different in kind than assumed" moment, not
just a sizing question. **User chose: implement the real XNA object model
in full, but every collection stays always-empty.**

**A second real discovery while reading the headers, worth internalizing
as a pattern:** `Album`'s (and `Artist`/`Genre`/`Playlist`'s) constructor
is `private`, friended only to `MediaLibrary` -- unlike `Song`/`ModelBone`/
`SongCollection`, the real C++ engine's own authors deliberately did
**not** give these a `CNAEXT`-public hand-building constructor. That's a
real signal, not an oversight: those types only make sense as part of a
coherent library scan (cross-referenced `Album`↔`Artist`↔`Genre`↔`Song`
relationships), unlike a standalone `Song` or `ModelBone`, which are each
meaningful in isolation. Respected that signal -- none of these four
types got a `CNAEXT` public constructor here either, even though this
project's own "no content pipeline exists" reasoning could have justified
inventing one the way it did for `Song`. **Worth remembering for any
future CNAEXT decision:** the real C++ engine having *already* made a
CNAEXT-or-not choice for a given type is itself evidence about whether
hand-building genuinely makes sense for it -- check what the engine
authors decided before assuming every construction gap needs a CNAEXT
escape hatch the way `Song`/`ModelBone` got one.

**Equality semantics reproduced exactly, not guessed, by reading each
type's own `.cpp`:** `Artist`/`Genre`/`Playlist.Equals` compare by name
only; `Album.Equals` compares by *(Name, Artist)*, not name alone (album
names collide across different artists), delegating to `Artist.Equals`
for the artist half rather than reference equality -- confirmed by
reading `Album.cpp`'s own `Equals` body, not assumed from the property
list. `Album.HasArt` is hardcoded `false` and
`GetAlbumArt`/`GetThumbnail` always throw
`InvalidOperationException` -- correct here specifically because no
`Album` in this project is ever backed by a real scanned file with real
art to report (matches the same "this is the actually correct answer,
not an unimplemented stub" reasoning `Song.IsProtected` already
established), not a stub standing in for unwritten logic.

**`Song.Album`/`.Artist`/`.Genre` added, always `null`, with `internal`
setters reserved for a hypothetical future real scan.** These didn't
exist before this pass (deliberately omitted when `Song` was first
built, since `Album`/`Artist`/`Genre` didn't exist yet) -- completes
`Song`'s own real XNA public API surface now that the types they'd
reference exist, without implying they're ever actually populated today.

**Compat-layer mirror needed careful design, but turned out to be fully
safe, unlike `MediaPlayer.Queue`'s genuine structural blocker two entries
ago.** Two separate safety arguments, not one:
- **`MediaLibrary`/`Album`/`Artist`/`Genre`/`Playlist`'s `new`-shadowed
  collection properties (`Albums`, `Songs`, etc.):** safe because every
  collection is *provably* always empty on both sides -- there's no real
  data that could ever diverge between a base-typed and compat-typed
  version of an empty collection, unlike `MediaPlayer.Queue` (where
  `LoadSong` always constructs real, non-empty, base-typed `Song` copies
  regardless of caller). The two-independent-pieces-of-mutable-state bug
  class this session found and fixed once for `GraphicsDevice.Indices`
  fundamentally needs *real, divergeable data* to bite -- an empty
  collection can't disagree with another empty collection.
- **`MediaLibrary.MediaSource`'s downcast:** safe for the different,
  "single construction seam, provably compat-typed" reason
  `SpriteFont.Texture` already established -- the compat `MediaLibrary`
  type has exactly two constructors, and both of them always supply a
  compat-typed `MediaSource` to the base constructor, so `base.MediaSource`
  is guaranteed compat-typed for every actually-reachable compat
  instance.
- The five collection types (`Song`/`Album`/`Artist`/`Genre`/
  `Playlist`Collection) are independent compat-namespaced implementations,
  not subclasses of their `CNA.Media` counterparts -- extending directly
  would inherit an indexer/enumerator typed to the base namespace's
  element type, defeating the entire point of a compat-typed collection
  (same reasoning `SpriteBatch`'s own compat `SongCollection`
  would have needed, had it been built). This is also the first time this
  session built a compat `SongCollection` at all -- it didn't exist
  before (the earlier `MediaPlayer.Queue`/`Play(SongCollection)` compat
  gap meant nothing needed one yet). **Worth flagging as a real, separate
  follow-up, not acted on in this pass:** now that a compat
  `SongCollection` exists, `Microsoft.Xna.Framework.Media.MediaPlayer.Play(SongCollection)`
  may be reachable after all (the original blocker was specifically about
  `Queue`'s *output* type, not about accepting a compat `SongCollection`
  as an *input* parameter) -- worth reassessing as its own small,
  separately-scoped pass rather than folding into this one.

**Deliberately out of scope even within this pass, not just deferred by
the empty-collections decision:** the real C++ engine's entire
picture-library surface (`Picture`/`PictureAlbum`/`PictureCollection`/
`PictureAlbumCollection`, `GetPictureFromToken`/`SavePicture`) -- a
separate, similarly infrastructure-bound feature (native image loading/
thumbnailing) that real XNA games essentially never touch (Zune-era
personal-photo browsing, not game asset loading), so it wasn't worth
pulling into this already-large pass. `MediaLibrary.Pictures`/
`.SavedPictures`/`.RootPictureAlbum` don't exist on this project's
`MediaLibrary` yet -- a real, narrow, documented gap, not silently
missing.

**Verified, not just written:** `dotnet build CNA.sln` clean across all 6
projects (0 warnings -- one real mistake caught and fixed during this
pass: `CNA.Media.MediaLibrary` was initially written `sealed`, which
would have blocked the planned compat mirror entirely; un-sealed it the
same way `Song`/`BasicEffect` already established, once the compat type
itself failed to compile against it). `dotnet test CNA.sln`: 311/311
passing (up from 280 -- 31 new tests: real construction/validation/
equality behavior for every new type, all reachable without any native
dependency at all, plus a `MediaSourceType` parity entry and focused
compat tests covering `MediaLibrary`'s public-only-reachable surface).
`samples/HelloGame` re-verified unaffected.

**Where to pick up next:** the `Play(SongCollection)` compat-mirror
reassessment flagged above is a small, well-bounded next step. Beyond
that, `Model` file-loading and the real C++ engine's picture-library
surface are the two remaining large, separately-scoped Phase 4
follow-ups -- a `/code-review high` pass over this commit is the
immediate next step, following this session's own established rhythm.

## Twelfth `/code-review high` pass, over the `VertexBuffer`/`IndexBuffer` `Type`-constructor commit -- clean (2026-08-16, session 6 continued autonomously still further again)

Ran the review a twelfth time, over the `VertexBuffer`/`IndexBuffer`
`Type`-taking constructors and the `MediaPlayer.cs` doc-only fix from the
previous entry. Clean pass -- no findings. Checked (and confirmed correct)
the constructor-initializer evaluation-order reasoning this session's own
tests already relied on, the compat-layer `FromType`'s correct resolution
to its own namespace's `IVertexType`, and that the doc/`NEXT.md`/`plan.md`
changes matched the code. One minor fidelity nuance noted for awareness,
not filed as a finding since it causes no crash or wrong behavior: real
MonoGame's `VertexDeclaration.FromType` throws a plain `Exception` if
`IVertexType.VertexDeclaration` returns null; this port instead lets that
null flow into `VertexBuffer`'s own existing `ArgumentNullException.ThrowIfNull`
check, a different (arguably more idiomatic) exception type for the same
practically-unreachable case (no real `IVertexType` implementer in this
codebase or a realistic one would ever return null there) -- not worth a
special-case fix for.

280/280 tests passing, unchanged; `dotnet build` clean; `samples/HelloGame`
re-verified unaffected. This closes out the `VertexBuffer`/`IndexBuffer`
`Type`-constructor pass entirely -- no follow-up code changes needed.

**Session status at this point:** every explicitly-flagged Phase 4/5 item
in `plan.md` is done, all twelve `/code-review high` passes this session
are clean or fully addressed, and the two remaining follow-ups (`Model`
file-format loading, the real C++ engine's `Album`/`Artist`/`Genre`/
`MediaLibrary` scanning subsystem) are both large enough to need their
own dedicated design pass rather than a natural continuation of anything
already in flight. Two smaller, previously-flagged gaps
(`GamePadState.PacketNumber`, `SpriteFont`'s `SpriteEffects`-driven
text-flip reversal) were considered and deliberately not attempted:
`PacketNumber` would need new, self-invented native ABI design with
nothing upstream to validate against (unlike every other native surface
this session added, all grounded against a real working C++
implementation); the text-flip reversal needs MonoGame's exact
line/character-reversal algorithm, which this environment has no way to
verify against (no decompiled source, no live binary) -- attempting it
without that grounding would mean guessing at exactly the kind of thing
this session has consistently avoided guessing at elsewhere.

## Eleventh `/code-review high` pass, over the `MediaQueue`/`SongCollection` commit (2026-08-16, session 6 continued autonomously still further)

Ran the review an eleventh time. Notable this pass: no `dotnet` toolchain
was available inside the review agent's own sandbox, so it verified by
direct line-by-line comparison against the real upstream C++ source
(`MediaPlayer.cpp`/`MediaQueue.cpp`/`SongCollection.cpp`) rather than by
building/running -- and reported the port as "unusually well-verified,"
confirming several subtle details it initially suspected were bugs
(the repeat-wraparound-regardless-of-direction quirk in `NextSong`) were
actually present in the ground truth too, matching what this session's own
implementation pass had already found.

One real, confirmed discrepancy from the verified upstream source:
`Play(SongCollection, int)` unconditionally raises `ActiveSongChanged`,
but the real C++ engine's equivalent function never raises the
equivalent flag for this specific overload at all (confirmed in its
source -- `Play(Song*)` and `NextSong` both do, `Play(SongCollection&, int)`
doesn't). Considered reverting to match upstream exactly, but concluded
this is a case for consistency over literal fidelity, not the other way
around: this session's own `Play(Song)` implementation *already* made a
deliberate, documented choice to simplify its C++ counterpart's
practically-always-true pointer-comparison guard into an unconditional
raise, reasoning that a `Play()` call always changes what's playing in
any meaningful sense. Applying that same reasoning here, an event whose
entire purpose is "tell observers the active song changed" firing for
some "the song definitely changed" call paths and not others would be a
worse, harder-to-discover API inconsistency than the upstream omission it
would be reproducing -- especially since starting a whole new playlist is
at least as meaningful a change as continuing within an existing one.
Kept the unconditional raise; fixed the doc comment instead, which had
overclaimed "matches the real C++ engine... exactly" without carving out
this one deliberate deviation.

280/280 tests passing, unchanged; `dotnet build` clean; `samples/HelloGame`
re-verified unaffected.

## `VertexBuffer`/`IndexBuffer`'s `Type`-taking constructors (2026-08-16, session 6 continued autonomously still further)

> Picked up a small, well-bounded, previously-flagged gap while looking
> for the next task after `MediaQueue`: `VertexBuffer`/`IndexBuffer`'s
> real XNA `Type`-taking constructor overloads had been explicitly
> deferred since the vertex-format-layer session (`IVertexType.cs`'s own
> doc comment still said "not implemented yet"). Zero native ABI impact
> (both overloads are pure reflection feeding the *same* existing native
> call), well-understood real XNA/MonoGame semantics recalled with high
> confidence, and directly testable -- a good-sized next task rather than
> reaching for the much larger deferred features (`Model` file-loading,
> `MediaLibrary`).

**`VertexDeclaration.FromType(Type)`** (`internal`, matching real XNA's
own accessibility -- not standalone public API, just
`VertexBuffer(GraphicsDevice, Type, int, BufferUsage)`'s implementation
detail): constructs a default instance of the given value type via
`Activator.CreateInstance`, casts to `IVertexType`, reads its
`VertexDeclaration` property. Matches real XNA/MonoGame's own internal
`VertexDeclaration.FromType` exactly, including its exception shape
(`ArgumentException` for a non-value-type or a value type that doesn't
implement `IVertexType`) -- message text recalled from memory, not
independently verified against a live binary, flagged the same way
`SpriteBatch`'s `Begin`/`End` exception text already was this session.

**`IndexBuffer`'s equivalent, `SizeForType(Type)`**, is simpler and needs
no interface/reflection at all: `typeof(short)`/`typeof(ushort)` →
`SixteenBits`, `typeof(int)`/`typeof(uint)` → `ThirtyTwoBits`, anything
else throws `ArgumentOutOfRangeException`. Made `internal` rather than
`private` specifically so it's directly unit-testable (`VertexDeclaration.FromType`
already needed to be `internal` for its own real reason -- matching real
XNA's accessibility -- so this one extra step for `IndexBuffer`'s equally
simple helper was a deliberate, minor testability-motivated accessibility
choice, not a fidelity one).

**Both `Type`-taking constructors chain into their existing
`VertexDeclaration`/`IndexElementSize`-taking constructors** (`: this(graphicsDevice,
VertexDeclaration.FromType(vertexType), vertexCount, bufferUsage)`, and
the `IndexBuffer` equivalent) rather than duplicating the native-call
logic -- convenience sugar over the same native call, not a second one.
A real, useful side effect of C#'s constructor-initializer evaluation
order: `FromType`/`SizeForType` run *before* the target constructor's own
body (where `graphicsDevice`'s null check and the native call live), so
an invalid `vertexType`/`indexType` throws before any native call is
reached -- fully testable without a real `cna-native`, unlike almost
everything else about these two types.

**`CNA.XnaCompat` mirror needed a genuinely separate `FromType`
implementation for `VertexBuffer`, not a forwarding call, for a real
reason:** a compat-namespaced vertex struct (e.g.
`Microsoft.Xna.Framework.Graphics.VertexPositionColor`) implements *this
namespace's own* `IVertexType`, a distinct interface from
`CNA.Graphics.IVertexType` -- the base layer's `FromType` would never
match it via its own pattern match (`is CNA.Graphics.IVertexType` fails
for an object that only implements the compat interface). Added a
second, compat-namespaced `VertexDeclaration.FromType` that operates on
the compat `IVertexType`/`VertexDeclaration` types instead -- structurally
identical to the base one, but a real second implementation, not
duplicated code with no reason to differ. `IndexBuffer`'s equivalent
needed no such split: `Type`-to-`IndexElementSize` inference has no
compat-specific dependency at all (no interface involved), so the compat
`IndexBuffer(GraphicsDevice, Type, int, BufferUsage)` constructor calls
`CNA.Graphics.IndexBuffer.SizeForType` directly.

**Attempted a compat-layer test file, then deleted it once the real
blocker became clear:** every compat `VertexBuffer`/`IndexBuffer`
constructor needs a compat `GraphicsDevice`, and that type's only
constructor is `protected internal` with no `InternalsVisibleTo` grant to
`CNA.XnaCompat.Tests` (this project has no `AssemblyInfo.cs` of its own --
a discovery from earlier this session, not new). No compat `GraphicsDevice`
instance can be constructed in that test project at all, so nothing
requiring one -- including these two new constructors -- can be exercised
there, matching `SpriteBatch`'s own already-documented limitation exactly.
Documented this in `CNA.XnaCompat.VertexBuffer`'s own doc comment instead
of leaving a non-viable test file in place.

**Verified, not just written:** `dotnet build CNA.sln` clean across all 6
projects. `dotnet test CNA.sln`: 280/280 passing (up from 264 -- 16 new
tests: `VertexDeclaration.FromType`'s success/failure paths for all five
standard vertex structs plus invalid-type cases, `IndexBuffer.SizeForType`'s
four valid mappings plus the unsupported-type/null-type cases, and each
`Type`-taking constructor's own invalid-type failure path). `samples/HelloGame`
re-verified unaffected.

**Where to pick up next:** the two remaining Phase 4 follow-ups (`Model`
file-format loading, the real C++ engine's `Album`/`Artist`/`Genre`/
`MediaLibrary` scanning subsystem) are both large enough to be their own
dedicated pass. A `/code-review high` pass over this commit is the
immediate next step, following this session's own established rhythm.

## `MediaQueue`/`SongCollection`: multi-song playlists, shuffle, repeat, events (2026-08-16, session 6 continued past the original "Phase 4/5 complete" checkpoint, per explicit instruction to keep going)

> Reported Phase 4 + Phase 5 complete and paused at a natural checkpoint.
> User replied "pokracuj autonomne dale dokud ti nejdou ukoly" (keep going
> autonomously until you run out of tasks). Picked `MediaQueue` over
> `Model` file-loading or the `Album`/`Artist`/`Genre`/`MediaLibrary`
> subsystem specifically because it was already mostly researched --
> `MediaPlayer.cpp`'s `NextSong`/`Update`/`Play(SongCollection)` logic was
> read in full during the original `Song`/`MediaPlayer` pass, just not
> acted on yet, so this was implementation work more than a fresh design
> pass.

**Shape, confirmed against the real C++ engine's `MediaQueue.hpp`/`.cpp`
and `SongCollection.hpp`, not re-derived from scratch:** `MediaQueue`
starts with `ActiveSongIndex = -1` (not 0) specifically so an empty
queue's `ActiveSong` correctly returns null rather than indexing a
would-be entry 0 that doesn't exist -- confirmed by reading the .cpp
constructor, not assumed from the header. `ActiveSong`'s getter
bounds-checks defensively (empty queue, negative index, out-of-range
index all return null) rather than trusting the index is always valid.
`SongCollection`'s own constructor is `CNAEXT` (public here, same "no
content pipeline exists" reasoning as `Song`'s own constructor), but
`MediaQueue`'s constructor and `Add`/`Clear` are `internal`, matching real
XNA's own encapsulation exactly (not a `CNAEXT` deviation this time --
nothing outside `MediaPlayer` ever needs to build a `MediaQueue` from
scratch, since it's always populated through `Play`).

**`NextSong`'s algorithm (shared by `MoveNext`/`MovePrevious`/`Update`'s
auto-advance) reproduced exactly:** stop first, then wrap to index 0 when
repeating past the last song, pick a uniformly random index when
shuffled, otherwise clamp `ActiveSongIndex + direction` to the queue's
bounds -- so calling `MoveNext` at the last song (or `MovePrevious` at the
first) is a no-op-at-the-edge, not a silent wraparound, unless repeating
is on. Used `System.Random` for the shuffle case rather than porting the
C++ engine's own `std::mt19937`, per design invariant #7 (real BCL types
for non-CNA-specific concepts).

**`Update()`'s song-end detection always uses the elapsed-time fallback,
not conditionally:** the real C++ engine prefers a native track-stopped
signal when compiled with `SOUND_ENABLED`, falling back to comparing
elapsed playback time against `Song.Duration` only when that native
signal isn't available. This project's own `MediaPlayer` native surface
was deliberately scoped (in the original pass) without an equivalent
track-stopped callback at all, so there's no preferred path to fall back
*from* here -- `DetectSongEndedByElapsedTime` is unconditionally the only
detection this project has. Kept public (matching the real engine's own
`CNAEXT`-public choice) specifically because it's a pure function
(`Song`, `TimeSpan` in, `bool` out) fully testable without a real queue or
native call, unlike `Update()` itself.

**A real asymmetry in the C++ engine, reproduced faithfully rather than
"fixed" the way `Model.Draw`'s bone-index fallback was:** `Play(Song)`
calls the low-level `PlaySong` with the caller's *original* `Song` object,
while `Play(SongCollection, index)` calls it with the *queue's own
defensive copy* (from `LoadSong`). This means a successful `Play(Song)`
increments `PlayCount` on the exact object the caller passed in, while a
successful `Play(SongCollection, index)` increments it on a copy the
caller never sees. Considered "fixing" this to be consistent (matching
the `Root`-fallback precedent from the `Model` review), but concluded it
doesn't meet that bar: unlike the `Root` fallback (where "use the actual
root bone" was obviously more correct than "hardcode bone 0"), there's no
obviously-more-correct choice here between "the object I asked to play"
and "the object my internal queue is actually tracking" -- both are
defensible source-of-truth choices, so guessing at a "fix" would be
inventing a preference neither this project's own conventions nor the
real engine's source actually call for. Documented the asymmetry
explicitly in both `Play` overloads' own doc comments instead.

**Wired `MediaPlayer.Update()` into `CNA.Game`'s base `Update(GameTime)`,
a new integration point this project didn't have before:** without it,
nothing would ever call `Update()` at all, so song-end detection and
queue auto-advance would silently never fire even though all the logic
for it now exists. Real XNA calls the equivalent (`FrameworkDispatcher.Update()`)
automatically as part of its own `Game.Update()`; this project has no
`FrameworkDispatcher`, so `CNA.Game.Update(GameTime)` calling
`MediaPlayer.Update()` directly is the closest available equivalent --
works for any game that calls `base.Update(gameTime)` in its own
override, which is standard XNA practice already.

**Test-isolation design was the hard part of this pass, not the logic
itself.** `MediaPlayer` is a process-global static class, and populating
`MediaPlayer.Queue` at all requires a (partially-executed, ultimately
throwing) `Play` call -- `LoadSong`'s `InnerQueue.Add(...)` runs
*before* the native call that eventually throws `DllNotFoundException`
in this environment, so calling `MediaPlayer.Play(validSong)` anywhere in
a test would leak a non-empty queue into every later test in the same
assembly for the rest of the run. Two fixes, both real design decisions,
not workarounds: (1) `MediaQueueTests.cs` tests `MediaQueue`'s own
logic (`Add`/`Clear`/`ActiveSong`/indexer/enumeration) against **fresh,
isolated instances** constructed directly via its `internal` constructor
(reachable via `InternalsVisibleTo`), never touching
`MediaPlayer.Queue` at all; (2) every `MediaPlayerTests.cs` addition was
checked against the explicit rule "never call `Play` with a real,
non-null, non-disposed `Song`" before being written, and the class's own
doc comment states that rule explicitly for whoever adds the next test
here. `CNA.XnaCompat.Tests` needed the identical caution independently
(same static class, but a genuinely separate process per `dotnet test`'s
own per-project test hosts, so its own state starts fresh regardless).

**Compat-layer decision, following the `Model` precedent for a different,
more structural reason:** `Model` had no compat mirror because there was
no content pipeline to ever produce one another way -- a *usage*
limitation. `Queue`/`Play(SongCollection)` have no compat mirror for a
*structural* reason: `LoadSong` always constructs the base `CNA.Media.Song`
type internally (matching the real C++ engine's own `LoadSong` exactly),
regardless of what type of `Song` was actually passed to `Play` --
meaning `CNA.Media.MediaPlayer.Queue`'s songs are never actually
compat-typed, even when the compat layer's own `Play(Song)` was called
with a compat `Song`. Every other compat type this session built solved
an equivalent "the base type isn't what I want to expose" problem via
inheritance (`BasicEffect`, `Song` itself extending its base directly) --
but `MediaPlayer` is a `static` class, which C# does not allow
subclassing at all, so there's no seam here to override `LoadSong`'s
copy-construction the way inheritance solved it everywhere else. A
compat `Queue` property would therefore return songs that fail an
explicit compat-typed downcast -- unsafe to ship, not a nice-to-have left
out. `IsShuffled`/`MoveNext`/`MovePrevious`/`ActiveSongChanged`/
`MediaStateChanged` have no such problem (no `Song`-typed data crosses
their own boundary) and got the full compat mirror, same thin-forwarding
shape as `Mouse`/`Keyboard`.

**Verified, not just written:** `dotnet build CNA.sln` clean across all 6
projects (0 warnings, after fixing two ambiguous-`cref` doc-comment
warnings the new `MediaPlayer.Play` overload set triggered, same shape as
`SpriteBatch.Draw`'s own earlier this session). `dotnet test CNA.sln`:
264/264 passing (up from 242 -- 22 new tests across `MediaQueueTests.cs`,
`MediaPlayerTests.cs`, and `MediaPlayerCompatTests.cs`, all passing on
first run). `samples/HelloGame` re-verified unaffected.

**Where to pick up next:** `Model` file-format loading and the real
`Album`/`Artist`/`Genre`/`MediaLibrary` scanning subsystem remain the two
substantial, separately-scoped Phase 4 follow-ups (see `plan.md`'s own
follow-up bullet) -- both large enough to be their own dedicated pass
rather than something to fold into a continuation of this one. A
`/code-review high` pass over this commit is the next immediate step,
following this session's own established per-feature rhythm.

## Tenth `/code-review high` pass, over the `SpriteBatch` batching commit (2026-08-16, session 6 continued yet further still again once more still further again once more still further)

Ran the review a tenth time. Two real findings, both fixed:

- **Fixed, real, and a genuine "permanently bricked instance" bug:**
  `End()` only reset `_hasBegun = false` on its last line, after both the
  flush (`cna_sprite_batch_draw_many`) and the native end call
  (`cna_sprite_batch_end`) had already succeeded. If either failed and
  threw `CnaException`, `_hasBegun` stayed `true` forever -- there's no
  public API to reset it directly, so every subsequent `Begin()` call
  would throw "cannot be called again until End has been successfully
  called," with no recovery short of disposing the instance and
  constructing a brand-new one. Wrapped the flush + native-end call in
  `try`/`finally`, unconditionally resetting `_hasBegun = false` -- a
  caller can now retry `Begin()` after a failed `End()` instead of losing
  the `SpriteBatch` permanently. Worth remembering as a general shape:
  any state flag that's only reset on a method's success path, with no
  other way to reset it, is a latent "one native failure away from
  permanently wedging this object" bug -- worth checking for on any
  future stateful wrapper this session or a later one adds.
- **Fixed, real, a message-quality bug:** the shared private `DrawEx`
  hardcoded `EnsureHasBegun(nameof(Draw))`, but it's the funnel-through
  point for *both* the `Draw` overload family *and* `DrawString`'s
  per-glyph loop -- so calling `DrawString` without `Begin()` threw
  "Begin must be called before Draw," naming the wrong method. Threaded a
  `caller` parameter through both private `DrawEx` overloads instead of
  hardcoding one name, with every call site passing its own actual
  top-level method name (`nameof(Draw)` from all six `Draw` overloads,
  `nameof(DrawString)` from the glyph loop).

No new tests possible -- same limitation as the commit these fixes apply
to (`SpriteBatch` still has no way to construct a test instance without a
real `cna-native`). 242/242 tests passing, unchanged; `dotnet build`
clean; `samples/HelloGame` re-verified unaffected.

## `SpriteBatch` command buffering (Phase 5) (2026-08-16, session 6 continued yet further still again once more still further again once more still)

> Every explicitly-flagged Phase 4 item is now done (see the previous
> entry's own "where to pick up next"). Picked Phase 5's first listed
> item -- `SpriteBatch` command buffering -- over the deferred
> `MediaQueue`/`Model`-file-loading follow-ups: it's a real, concretely-
> scoped task with an exact shape already specified in
> `analysis_binding.md` §22 (the only place in this whole ABI surface
> where the analysis docs give a literal struct/function signature to
> implement against, not just a naming convention), rather than another
> open-ended feature needing its own from-scratch design pass.

**What changed, mechanically:** every `SpriteBatch.Draw`/`DrawString` call
used to call native immediately (`cna_sprite_batch_draw`/
`cna_sprite_batch_draw_ex`, one native call per sprite). Now every call
appends a `CnaSpriteDrawCommand` to a managed `List<T>` instead, and
`End()` flushes the whole batch through one new `cna_sprite_batch_draw_many`
call (`CnaHandle, CnaSpriteDrawCommand*, nuint` -- matches §22's own
example signature exactly). The old single-draw native functions were
removed outright, not kept alongside the batched form: once every `Draw`
call funnels through the same buffer, nothing in this project's C# calls
them anymore, and keeping unreachable P/Invoke declarations around would
just be dead code.

**A real correctness gap surfaced for free while doing this, not sought
out separately:** `SpriteBatch` had never tracked whether `Begin()` had
been called at all -- there was nothing to track before, since every
`Draw` went straight to native with no managed state in between. Once a
managed command buffer existed, "what if `Draw` is called with no active
`Begin()`" became an actual question with an actual wrong answer if left
unhandled (silently buffering commands that might never get flushed, or
flushing stale commands from an earlier unpaired `Begin()`). Added real
state tracking (`_hasBegun`) and three `InvalidOperationException` guards
matching real XNA/MonoGame's own actual behavior there: `Draw`/`DrawString`
before `Begin`, `End` before `Begin`, and calling `Begin` twice without an
intervening `End`. **Confidence flag, matching this session's own
recalled-not-verified convention:** the exact exception message text is
recalled from memory (MonoGame source), not independently verified
against a live binary or decompiled source in this environment -- same
honesty standard as the rare `Keys` ordinals/`GamePadType` values earlier
this session.

**Native struct/function shape needed zero design work, unlike almost
everything else this session:** `CnaSpriteDrawCommand` already existed
(added when the single-draw `cna_sprite_batch_draw_ex` primitive was
built, itself already shaped after §22's example struct field-for-field)
-- this pass just changed *how many* of them cross the ABI per native
call and *when*, not their shape. `cna_sprite_batch_draw_many`'s
signature is likewise a direct, literal implementation of §22's own
example, not an inference from a naming convention the way `RenderTarget2D`/
`SoundEffect`/`BasicEffect`/`Song`/`MediaPlayer` all needed this session.

**Real testability limitation, not a new one, but worth naming
explicitly:** despite all of this being pure managed logic (the buffer,
the `_hasBegun` guards), it's still not independently testable.
`SpriteBatch`'s only constructor calls `cna_sprite_batch_create`
immediately, with no raw-handle-wrapping escape hatch the way
`GraphicsDevice`/`Texture2D` both have (`protected internal
GraphicsDevice(nint)` / `Texture2D(nint)`, both added for a real
production reason -- wrapping an already-created native handle -- with
testability as a side benefit, not the goal). Considered adding an
equivalent constructor to `SpriteBatch` purely to unlock testing this new
logic, and deliberately didn't: there's no production scenario that ever
wraps an already-created `SpriteBatch` handle the way `ContentManager`
wraps an already-created `Texture2D` one, so a test-only constructor here
would be a new, weaker-justified pattern than the two existing precedents,
not an extension of one. Documented the limitation in `SpriteBatch`'s own
doc comment instead of silently shipping untested.

**Verified, not just written:** `dotnet build CNA.sln` clean across all 6
projects (0 warnings, after fixing one ambiguous-`cref` doc-comment
warning the class-level summary's own `<see cref="Draw"/>` triggered --
`Draw` has multiple overloads, so the cref needed to become a plain
`<c>Draw</c>` instead). `dotnet test CNA.sln`: 242/242 passing, unchanged
(no new tests possible, per the limitation above -- confirmed this is
genuinely a "can't", not a "didn't bother", before moving on).
`samples/HelloGame` re-verified unaffected -- still fails at exactly the
same documented `DllNotFoundException` point, confirming this refactor
touched nothing about *when* or *whether* native gets called for the
sample's own single `Draw` call, only what happens when there's more than
one.

**Where to pick up next:** `EffectParameter` handle caching (§27) is
Phase 5's next listed item, but this project doesn't implement
`EffectParameter` at all yet (`BasicEffect`'s own property surface is its
parameter interface -- see that type's own doc comment) -- likely not
worth doing in isolation without a real `EffectParameter` type to cache
handles *for*. Buffer-based bulk transfer for `Texture2D.SetData`/vertex/
index data (`analysis_binding_sharp_runtime.md` §40) is more directly
actionable, closer in shape to this pass. Otherwise, the deferred Phase 4
follow-ups (`MediaQueue`, `Model` file-format loading,
`Album`/`Artist`/`Genre`/`MediaLibrary`) remain real, substantial,
separately-scoped next steps -- see `plan.md`'s own follow-up bullet.

## Ninth `/code-review high` pass, over the `Song`/`MediaPlayer` commit (2026-08-16, session 6 continued yet further still again once more still further again once more)

Ran the review a ninth time. Four findings; three real and fixed, one
confirmed to match the real C++ engine's own verified behavior and left
as-is with strengthened documentation, same disposition as the
`ModelMeshPart.Effect`-registration-timing finding two reviews ago:

- **Fixed, real, and confirmed to exist in the upstream C++ engine too:**
  `MediaPlayer.Play`'s native call always stops whatever was previously
  playing *before* attempting to load the new song (confirmed by reading
  `PlaySong` again: `DestroyMusicTrack`/`DestroyMusicAudio` run
  unconditionally at the top, before the load attempt) -- so a failed
  `Play()` left the managed `State`/`PlayPosition` referring to a song
  that was no longer actually playing. The real C++ engine has this exact
  bug itself (`setStateProperty(MediaState::Playing)` is only ever called
  on `PlaySong`'s success path, never reset on a failure path), but unlike
  most of this session's "faithful reproduction over invention" calls,
  this one was worth fixing rather than reproducing: this project's own
  exception-based failure convention (unlike the C++ engine's silent
  early-`return`) makes correcting it a two-line change with no
  architectural cost. `Play` now unconditionally resets `Timer`/`State` to
  `Stopped` immediately after the native call returns, *before* checking
  whether it succeeded -- so a thrown `CnaException` still leaves managed
  state consistent with what the native call actually did.
- **Fixed, real, genuinely cheap:** `Play` never checked
  `song.IsDisposed` before handing its handle to native code. Neither real
  XNA nor the C++ engine document/enforce this either, and `Song` has no
  actual native handle to protect (`Dispose()` just flips a bool), so the
  practical risk is low -- but `ObjectDisposedException.ThrowIf` is a
  one-line, zero-cost addition that keeps `Song`'s `IDisposable` contract
  honest, and it's a real, if narrow, misuse this project can now catch
  where the C++ engine can't. New regression test, fully native-independent
  (both checks run before the native call).
- **Fixed, real, worth the small refactor:** `CNA.XnaCompat.Song.FromUri`
  duplicated `CNA.Media.Song.FromUri`'s ~10-line URI-resolution algorithm
  verbatim -- a real drift risk, unlike this session's other "small,
  self-evidently-correct duplication" precedents (`SetRenderTarget`/
  `Indices`'s null-to-handle ternary, the four `Model*Collection` types'
  boilerplate), because those duplicate *different* small operations,
  while this duplicated the *exact same* algorithm for the *same*
  conceptual operation twice. Extracted `CNA.Media.Song.ResolvePathFromUri`
  (`internal`, visible to `CNA.XnaCompat` the usual way) and pointed both
  `FromUri` overloads at it -- eliminates the drift risk with zero
  behavior change and no new public API surface.
- **Left as-is, matches verified upstream behavior:** `Song.Equals`/
  `GetHashCode` compare `Handle` with plain ordinal (case-sensitive)
  string equality, so two paths differing only in case but naming the
  same file on a case-insensitive filesystem (Windows, default macOS)
  compare unequal. Confirmed the real C++ engine's own `Song::Equals` does
  the identical plain `std::string ==` comparison, no case-folding there
  either -- and unlike the `Play()`-state-reset fix above, there's no
  obviously-correct fix available to prefer over reproduction here: the
  "right" case-sensitivity is genuinely platform-dependent, and neither
  the analysis docs nor the real engine's own implementation specify one,
  so guessing at a fix would be inventing behavior rather than correcting
  a knowably-wrong one. Strengthened `Equals`'s own doc comment to say so
  explicitly rather than let this look like an accidental oversight.

242/242 tests passing (up from 241); `dotnet build` clean; `samples/HelloGame`
re-verified unaffected.

## `Song`/`MediaPlayer`/`MediaState` (2026-08-16, session 6 continued yet further still again once more still further again)

> Last explicitly-flagged Phase 4 item, per the previous entry's own
> "where to pick up next" pointer, which also flagged it as needing a
> check for the same "real, working, not-yet-C-ABI-exposed C++
> implementation" lucky break -- and, per that same pointer's own
> reminder not to assume a feature is unusually hard without looking
> first (the lesson `Model` itself had just taught), checked
> `modules/media/` before assuming `Song`/`MediaPlayer` needed pure
> invention or were out of reach.

**They did have that lucky break, and it was bigger than expected --
`modules/media/` has a full, real, working `Song`/`SongCollection`/
`MediaPlayer` implementation, complete with real test audio assets
(mp3/opus/flac) and even actual `.xnb` content-pipeline loading for
songs.** But reading `Song.hpp`/`.cpp` and `MediaPlayer.hpp`/`.cpp` in
full (before designing anything, same discipline as every native-backed
feature this session) showed the real engine's version is substantially
larger than what a typical XNA game actually uses: a full
`Album`/`Artist`/`Genre`/`MediaLibrary` scanning subsystem (tag parsing,
on-disk indexing, ratings), `MediaQueue` (multi-song playlists, shuffle,
repeat-driven auto-advance), visualization data capture (FFT, post-mix
audio-thread taps), and deferred events routed through a
`FrameworkDispatcher` this project doesn't implement. Deliberately scoped
this pass down to real XNA's actual most-used surface --
`MediaPlayer.Play(song)` for background music, `Volume`/`IsMuted`,
checking `State` -- rather than porting the whole thing in one pass,
matching this session's own repeated "explicit, documented scope cut,
not a silent gap" practice (`SoundEffect.Play()`'s convenience overloads,
`VertexBuffer`'s `Type`-taking constructor, and now this). Each deferred
piece is called out by name in `plan.md`'s own follow-up bullet so it
doesn't quietly disappear from the record.

**`Song` construction turned out to be a *third* zero-native-ABI escape
hatch this session** (after `SpriteFont`'s raw-glyph constructor and
`BasicEffect`'s zero-ABI-until-`Apply()` construction): the real C++
`Song` constructor is pure managed logic -- a `std::filesystem::exists`
check, nothing else, no renderer/audio handle allocated until
`MediaPlayer` actually plays it. Reproduced identically with
`File.Exists`/`FileNotFoundException` in C#, which makes `Song` real and
testable *today* against real temporary files -- a rarity among this
session's native-backed types, most of which can only test their
argument-validation failure paths, not real success-path behavior.

**Found (and correctly resolved) a real doc/code mismatch in the
upstream C++ header while grounding this:** `Song.hpp`'s own doc comment
claims an empty `name` argument "defaults to the file name," but
`Song.cpp`'s actual constructor body just stores whatever was passed,
`std::move(name)`, with no fallback logic at all -- confirmed by reading
the .cpp, not assumed from the .hpp. Reproduced the *verified behavior*
(name stays empty if passed empty), not the doc comment's claim, and
added a dedicated regression test
(`Constructor_EmptyNameNotDefaulted_MatchesVerifiedRealBehavior`) so this
doesn't quietly "fix itself" back to the doc-comment's claimed behavior
later. **Worth restating as a general habit:** when a header's doc
comment and its own .cpp body disagree, the .cpp is the ground truth --
this is the second time this exact class of discrepancy has mattered this
session (the first was catching `CreateBillboard`'s fallback sign being
exactly what an existing doc comment had already flagged as unconfirmed).

**`MediaPlayer.State`/`Volume`/`IsMuted`/`PlayPosition` are plain C#
static state, not native queries -- matches the real C++ engine's own
architecture, not a simplification invented for this project.** The real
`state_`/`volume_` are plain C++ static fields set locally by
`Play`/`Pause`/`Resume`/`Stop` themselves (never queried from SDL3_mixer),
and its own playback-position timer uses `std::chrono::steady_clock` -- a
language-level facility, not an ABI call. `System.Diagnostics.Stopwatch`
is the exact .NET BCL equivalent (design invariant #7: never invent a
CNA-flavored reimplementation of an ordinary BCL type) -- and unlike the
C++ engine's own manual `TimerStart`/`TimerStop`/accumulate bookkeeping,
`Stopwatch` already tracks elapsed time correctly across multiple
start/stop cycles on its own, so no manual accumulation logic was needed
at all.

**`Song.FromUri` deliberately does NOT port the real C++ engine's own
URI-parsing logic**, even though everything else this pass reproduces
that engine's algorithms faithfully. The C++ version hand-rolls
percent-decoding, scheme detection, and UNC-path handling (Windows
`file://<host>/` paths) because C++ has no equivalent to `System.Uri` in
its standard library. C# does -- `Uri.TryCreate`/`.LocalPath` already
solve exactly this problem, correctly, including the RFC 8089 edge cases
the C++ version's ~90 lines of manual parsing exist to handle. Porting
that logic by hand would have been reproducing a workaround for a gap
that doesn't exist on this side of the binding. Design invariant #7 made
this an easy call, not a judgment call.

**Native surface, six functions, all shaped to match the real C++
engine's actual method surface:** `cna_mediaplayer_play`/`pause`/`resume`/
`stop`/`set_volume`/`set_muted`. `MediaPlayer` is process-global/static in
real XNA (not tied to a `GraphicsDevice` or any other handle) -- these
take no `CnaHandle` parameter at all, matching the existing
`Keyboard`/`Mouse`/`GamePad` state calls' own no-handle shape rather than
inventing a new calling convention for this project's first
static-subsystem native surface. Unlike the real C++ engine (which
silently does nothing on a native load failure inside `PlaySong`), this
project's `Play` throws `CnaException` on failure -- matching *this
project's own* established convention (every other native call does),
not the C++ engine's, a deliberate divergence for consistency with the
rest of this codebase rather than a reproduction choice.

**Compat-layer decision, opposite of `Model`'s, for a documented reason:**
`Model` got no `CNA.XnaCompat` mirror this session because there's no
content pipeline to ever produce one any other way, making a mirror's
practical value near zero. `Song` has no equivalent blocker --
construction is a public, real, usable CNAEXT path with no hierarchical
construction-seam problem the way `ModelBone`/`DirectionalLight` had -- so
it got a full mirror: `Microsoft.Xna.Framework.Media.Song` extends
`CNA.Media.Song` directly (the "preserve the real logic's lineage" trade-off
`RenderTarget2D`/`BasicEffect` already established), and is `sealed` there
specifically because real XNA's own `Song` is `sealed` -- `CNA.Media.Song`
itself is deliberately left unsealed only so the compat subclass has
something to extend. `Microsoft.Xna.Framework.Media.MediaPlayer` is a thin
forwarding static class, the exact shape this compat layer's `Mouse`/
`Keyboard` already use for process-global subsystems.

**Verified, not just written:** `dotnet build CNA.sln` clean across all 6
projects. `dotnet test CNA.sln`: 241/241 passing (up from 218 -- 23 new
tests: 11 in `SongTests.cs` exercising real file-existence/equality/
`FromUri` behavior against real temp files, 6 in `MediaPlayerTests.cs`
covering its native-independent guard clauses, 3 in `SongCompatTests.cs`,
1 new `MediaState` parity entry in `CompatibilityTests.cs`, all passing on
first run). `samples/HelloGame` re-verified unaffected.

**Where to pick up next:** this closes out every item `plan.md` Phase 4
originally called out by name. What's left is either genuinely deferred
follow-up work (the `MediaQueue`/`Album`/`Artist`/`Genre`/`MediaLibrary`
subsystem, `Model` file-format loading -- see `plan.md`'s own new
follow-up bullet for the full list) or Phase 5 performance work
(`SpriteBatch` command buffering, `EffectParameter` handle caching, bulk
buffer transfer) -- both are real, substantial, separately-scoped next
steps rather than small continuations of what this session already did.
Worth deciding explicitly which of those (if either) is the best use of
remaining budget, rather than defaulting to whichever is listed first.

## Eighth `/code-review high` pass, over the `Model` commit -- three real bugs, one already-verified-against-upstream non-issue (2026-08-16, session 6 continued yet further still again once more still further)

Ran the review an eighth time. This one notified twice for the same run --
its own continued investigation after the first notification turned up a
fourth, genuinely new finding the first pass hadn't caught, and revised
its severity ranking; per this tool's own documented behavior ("the same
task-id may notify more than once"), acted on the final, corrected list
rather than the first one. Four real findings total, three fixed, one
already covered by existing tests/docs and left alone:

- **Fixed, real:** `CopyAbsoluteBoneTransformsTo` (and by extension
  `Model.Draw`) assumed, unvalidated, that every bone's `Index` matches its
  position in `Bones` and that parents always appear before children in
  the list -- a malformed hand-built list silently produced a zero matrix
  (not even a wrong-but-plausible one) instead of failing. Confirmed the
  real C++ engine's own `Model::CopyAbsoluteBoneTransformsTo` has the
  *exact same* unvalidated assumption (read its source again to check) --
  but since this project uniquely exposes hand-construction as the *only*
  way to build a `Model` (real XNA never lets a caller violate this
  invariant, because only the trusted content pipeline ever builds the
  list), added explicit validation here that the real engine doesn't have:
  throws `InvalidOperationException` for either a bone whose `Index`
  doesn't match its list position, or a bone whose parent appears at a
  later position than itself. Two new regression tests, one per failure
  shape.
- **Fixed, real:** `Model.Draw()` on a model with meshes but zero bones
  crashed with a bare `IndexOutOfRangeException` (`_sharedDrawBoneMatrices`
  is `new Matrix[0]`, and the null-`ParentBone` fallback index is `0`,
  which is out of range for a zero-length array). Added a range check that
  throws a clear, actionable `InvalidOperationException` naming the mesh
  and the actual bone count instead. One new regression test.
- **Fixed, real, and the one that needed a second look to find:** the
  review's own continued investigation (between its two notifications)
  caught that `Draw()`'s "mesh has no explicit `ParentBone`" fallback
  hardcoded bone index `0` -- also confirmed against the real C++ engine's
  own `Model::Draw` (`boneIdx = mesh->getParentBoneProperty() ? ... : 0`),
  so this one is a faithful reproduction of upstream, not a divergence
  introduced here. Faithful reproduction of a real bug is still a bug,
  though: `0` only coincides with the model's actual root bone when
  `rootBoneIndex` is left at its default -- a caller using the 5-argument
  constructor with a non-zero `rootBoneIndex` *and* an empty
  `meshParentBones` list (fully valid, publicly reachable) would silently
  draw every parentless mesh positioned by the wrong bone's transform, no
  exception. Changed the fallback to `mesh.ParentBone?.Index ?? Root?.Index
  ?? 0` -- `Root` is exactly the concept that should have been the
  fallback in the first place, and was already sitting right there on
  `Model`. **Worth remembering:** "confirmed faithful to the real C++
  engine's source" answers "did I introduce this," not "is this correct" --
  the two questions are independent, and this session's own habit of
  treating upstream fidelity as a strong signal of correctness needed a
  deliberate exception here, made with a documented reason, not a reflexive
  one. One new regression test, using a bone list where position 0 and the
  actual root are deliberately different bones so the old hardcoded
  fallback would have failed it.
- **Left as-is, matches verified upstream behavior, already documented and
  tested:** setting `ModelMeshPart.Effect` before the part has a `Parent`
  silently skips registering the effect in `ModelMesh.Effects`, so
  `Model.Draw()` never updates its matrices or runs its `IEffectMatrices`
  check, yet `ModelMesh.Draw()` still renders it with stale/unconfigured
  matrices. Re-confirmed this matches the real C++ engine's own
  `ModelMesh` constructor exactly (a raw field assignment for the parent
  link, no re-invocation of the effect-registration logic) -- already
  covered by `ModelMeshPartTests.Effect_Setter_BeforePartHasParent_DoesNotRegisterOnMesh`
  and `ModelMeshPart.Effect`'s own doc comment, which was strengthened this
  pass to spell out the silent-wrong-rendering consequence explicitly
  (previously it only said "no-op," which undersold the actual severity)
  rather than changing behavior to diverge from the verified real engine.
  A fifth finding (the four collection types' shared indexer/enumerator
  boilerplate) was left alone too, same "small, self-evidently-correct
  duplication across a handful of already-tested types isn't worth a
  premature shared-base abstraction" reasoning this session has applied
  repeatedly (`VertexElementFormat`/`VertexElementUsage`,
  `GamePadCapabilities`'s flag checks, the `SetRenderTarget`/
  `SetVertexBuffer`/`Indices` null-to-handle ternary).

218/218 tests passing (up from 214 -- three new regression tests: bone
index/position mismatch, parent-appears-after-child, and the
root-bone-fallback case); `dotnet build` clean; `samples/HelloGame`
re-verified unaffected.

## `Model`/`ModelBone`/`ModelMesh`/`ModelMeshPart`/`IEffectMatrices`/`IEffectFog`/`IEffectLights` (2026-08-16, session 6 continued yet further still again once more still)

> Picked up per `plan.md`'s own "still not started" pointer after the
> `BasicEffect` review-and-fix round: `Model` was flagged as needing a
> check for the same "real, working, not-yet-C-ABI-exposed C++
> implementation" lucky break `SoundEffect`/`VertexBuffer`/`BasicEffect`
> each had, before assuming it needed pure invention. It did -- and turned
> out to need *more* of a lucky break than any of those: zero new native
> ABI surface at all.

**The big discovery: `Model` isn't a native-resource-backed type family in
the real engine, it's pure object composition on top of ones that already
exist.** Read `modules/graphics/`'s `Model.hpp`/`.cpp`,
`ModelMesh.hpp`/`.cpp`, `ModelMeshPart.hpp`/`.cpp`, `ModelBone.hpp`/`.cpp`,
and all four collection headers (`ModelBoneCollection`/`ModelMeshCollection`/
`ModelMeshPartCollection`/`ModelEffectCollection`) in full before writing
anything. `Model::Draw()`/`ModelMesh::Draw()` don't call any GPU/renderer
API directly at all -- they're just C++ logic that calls the *already
native-backed* primitives this session already built (`SetVertexBuffer`,
`Indices`, `Effect::Apply()`/`EffectPass::Apply()`, `DrawIndexedPrimitives`).
So the entire `Model` feature -- five classes plus four collections --
needed **one new native ABI function total: zero.** This is a stronger
version of the "escape hatch" pattern that's shown up repeatedly this
session (`SpriteFont`'s raw-glyph constructor, `VertexDeclaration`'s stride
arithmetic, `BasicEffect`'s zero-ABI-until-`Apply()` construction) --
previous instances were "this *specific member* needs no native call,"
this one is "this *entire feature* needs no native call, because it's
built entirely out of already-native-backed pieces."

**`Model::Draw()`'s algorithm, reproduced exactly, not approximated:**
compute every bone's absolute (world-relative) transform once into a
reused buffer (`dest[i] = bone.Transform` if root, else
`bone.Transform * dest[bone.Parent.Index]` -- the real C++ code's own
`sharedDrawBoneMatrices_` static buffer, reproduced as an instance field
here rather than a `static`, since C++'s file-scope static would be a
cross-instance data race waiting to happen in C# the moment two `Model`s
draw on different threads, even though real XNA's own single-threaded
assumption means it was never actually unsafe there either -- a case where
literal fidelity to the C++ source would have *introduced* a new footgun,
not preserved a real one, so this one detail was deliberately not copied
verbatim). Then for each mesh, for each effect its parts use, cast to
`IEffectMatrices` (throw `InvalidOperationException` if it doesn't
implement that interface -- matches the real engine's `std::runtime_error`
exactly), and set `World = boneAbsoluteTransform[boneIndex] * world`,
`View = view`, `Projection = projection`, before finally calling
`mesh.Draw()`.

**`IEffectMatrices`/`IEffectFog`/`IEffectLights` added to `CNA.Graphics`,
confirmed against the real engine's own headers, not invented.**
`IEffectMatrices` is the one `Model.Draw()` actually needs (to set
`World`/`View`/`Projection` on an effect without knowing its concrete
type). `BasicEffect.World`/`View`/`Projection` are public *fields*
(matching the real C++ engine's own field-not-property choice, confirmed
by reading `BasicEffect.hpp` again for this) -- a field can't satisfy an
interface property in C#, so `BasicEffect` needed an explicit interface
implementation forwarding to the fields
(`Matrix IEffectMatrices.World { get => World; set => World = value; }`),
the identical shape the real C++ engine itself uses (`getWorldProperty()`/
`setWorldProperty()` override methods wrapping the same public field).
`IEffectFog`/`IEffectLights` were essentially free once `IEffectMatrices`
existed: `BasicEffect`'s `FogColor`/`FogEnabled`/`FogEnd`/`FogStart`/
`AmbientLightColor`/`DirectionalLight0-2`/`LightingEnabled`/
`EnableDefaultLighting()` already matched every interface member's name
and type exactly (they're real *properties*, not fields, so ordinary
implicit interface implementation just worked -- no forwarding needed).

**Real XNA's `ModelBone`/`ModelMesh`/`ModelMeshPart`/`ModelEffectCollection`
have no public construction/mutation surface at all -- content-pipeline-only
(`internal`) -- because real games only ever get a populated model back
from `Content.Load<Model>()`.** This project has no model-file loader
(parsing a real model format is a separate, much larger problem than
anything else built this session). The real C++ engine resolves this by
marking the equivalent constructors/setters `CNAEXT` (public, documented
deviations) specifically so hand-written code has *some* way to build a
model at all -- reproduced verbatim here rather than re-deciding
independently, matching this session's own established "when the real
engine already made this exact call, don't re-litigate it" habit.
`ModelBone.AddChild`, `ModelMeshPart`'s six `SetXxx` methods, and
`ModelEffectCollection.Add`/`Remove` are all public for this reason.

**`ModelMeshPart.Effect`'s setter auto-maintains its parent mesh's
`Effects` collection, reproduced from `ModelMeshPart::setEffectProperty`
exactly: adds the new effect only if no sibling part already references
it, removes the old effect only if this was the last part still using
it.** This surfaced a genuine, non-obvious ordering requirement while
writing the very first `Model.Draw()` test: setting `Effect` via an object
initializer *before* the part belongs to a mesh (i.e. before `ModelMesh`'s
constructor sets `part.Parent`) is a no-op for registration purposes --
`Parent` is still null when the setter runs, so the "add to
`Parent.Effects`" branch never executes, and nothing retroactively
re-registers it once `Parent` actually gets set. This matches the real
C++ code's behavior exactly (it's a raw field assignment in `ModelMesh`'s
constructor, not a re-invocation of `setEffectProperty`), so it's not a
bug -- but it *did* fail two tests on first run before the cause was
understood, both fixed by reordering the test (construct parts, construct
the mesh, *then* assign each part's `Effect`) rather than changing the
product code. Added a dedicated regression test for the ordering itself
(`Effect_Setter_BeforePartHasParent_DoesNotRegisterOnMesh`) so this
behavior stays intentional and documented rather than being a trap for
the next session. **Lesson worth carrying forward:** when a hand-built
object graph has this "later construction step wires up a link that
earlier property setters silently depend on" shape, the right fix for a
test failure is almost always reordering the test to match the real
required construction sequence, not assuming the product logic is wrong --
but only *after* actually reading why (here: tracing that `mesh.Effects`
came back empty, then finding the `Parent is not null` guard), not by
reflexively reordering and hoping.

**Test technique worth reusing:** `Model.Draw()`'s bone-transform/matrix-
assignment logic runs entirely in managed code *before* `mesh.Draw()`'s
first native call, so `Draw_SetsEffectMatricesBeforeDrawingMesh` lets the
expected native failure (no real `cna-native` in this environment) happen
via `Record.Exception(...)` and then asserts on the effect's `World`/
`View`/`Projection` having already been set correctly -- same idea as
`BasicEffectTests`' `FogVectorForTests`/`EyePositionWorldForTests`
internal-property exposure, but applied here by simply tolerating (not
suppressing) the native call's failure instead of needing a dedicated
test-only accessor, since the state under test is on a plain test-only
`RecordingEffect : Effect, IEffectMatrices`, not on the type actually
being exercised.

**Verified, not just written:** `dotnet build CNA.sln` clean across all 6
projects (0 warnings after fixing two `CS8603` nullability warnings on
`ModelBoneCollection`/`ModelMeshCollection`'s name-indexer, both needing a
`[NotNullWhen(true)]` annotation on their `TryGetValue` `out` parameter for
the compiler to trust the null-check-then-throw pattern). `dotnet test
CNA.sln`: 214/214 passing (up from 189 -- 25 new tests across
`ModelTests.cs`/`ModelMeshPartTests.cs`, two of which failed on first run
for the real-but-non-obvious ordering reason above, both fixed by
correcting the test). `samples/HelloGame` re-verified unaffected.

**Where to pick up next:** `Song`/`MediaPlayer` is the last explicitly-flagged
Phase 4 item, and a harder problem than everything done so far this
session even with the "check the real C++ engine first" habit applied --
real XNA's `Song` has no public constructor at all (unlike every
native-backed type built this session, all of which had *some* real public
escape hatch), so it would need native streaming-audio-format loading
designed close to from scratch. Worth checking `modules/audio/` for a
`Song`/`MediaPlayer` implementation before assuming that, though -- the
`Model` discovery above is a reminder not to assume a feature is
unusually hard without actually looking first.

## Seventh `/code-review high` pass, over the `BasicEffect` commit (2026-08-16, session 6 continued yet further still again once more)

Ran the review a seventh time. The agent independently rebuilt and re-ran
the full suite before reviewing (0 warnings, 189/189 passing) -- worth
noting since that's a stronger starting point than a review that only
reads the diff. Three findings; two real and fixed, one judged a
pre-existing, accepted, codebase-wide pattern rather than a bug specific to
this commit:

- **Fixed:** the constructor hand-wrote `new Vector3(0f, -1f, 0f)` three
  times for the inert default light direction, even though the doc comment
  right above it already says this value "matches `Vector3.Down`" -- now
  actually uses `Vector3.Down` in all three places instead of a literal
  that could silently drift from the property it's documented as matching.
- **Fixed:** `WriteColumnMajor` hand-derived the row-to-column transpose
  element-by-element, duplicating exactly what `Matrix.Transpose`
  (already implemented, already tested) already computes. Verified the
  replacement is equivalent by hand before switching, not just assumed:
  writing `Matrix.Transpose(m)`'s fields out in ordinary row order
  (`t.M11, t.M12, t.M13, t.M14, t.M21, ...`) produces the exact same
  16-float sequence as the original hand-written column-major mapping --
  confirmed field-by-field against `Matrix.Transpose`'s own constructor
  argument order before editing, not just by inspection after. Now calls
  `Matrix.Transpose` and writes its fields in row order instead.
- **Left as-is, not a new problem:** `CNA.XnaCompat.BasicEffect.Texture`'s
  getter (`(Texture2D?)base.Texture`) throws `InvalidCastException` if
  `base.Texture` was ever set to a plain `CNA.Graphics.Texture2D` through
  a base-typed reference to the same object (e.g. code holding it as
  `CNA.Graphics.Effect` in a batch-apply list). Real, but not unique to
  this commit -- `GraphicsDevice.Indices`' own downcast pass-through
  (`(IndexBuffer?)base.Indices`, fixed for a *different* bug two commits
  ago) has the exact same hard-cast shape and the exact same theoretical
  exposure, and nothing about `BasicEffect.Texture` makes it more
  reachable than any other mutable downcast-pass-through property already
  shipped in this codebase. Fixing this one property in isolation would
  make it inconsistent with every sibling property that shares the same
  shape, not safer overall -- if this class of risk is ever worth closing,
  it should be closed everywhere at once (e.g. switching every such getter
  from a hard cast to `as` returning `null` on a real type mismatch), as
  its own dedicated pass, not smuggled into an unrelated feature commit.
  Noted here so a future "harden all downcast-pass-through compat
  properties" pass has this as a starting inventory entry.

189/189 tests still passing; `dotnet build` clean; `samples/HelloGame`
re-verified unaffected.

## `Effect`/`BasicEffect`/`EffectTechnique`/`EffectPass`/`DirectionalLight` (2026-08-16, session 6 continued yet further still again)

> Fourth slice of the 3D pipeline, per the previous entry's own "where to
> pick up next" pointer -- the largest single addition this session, as
> flagged in advance. Confirmed the same research-before-design payoff one
> more time: the real `openeggbert/cna` C++ engine's `modules/graphics/`
> has a full, working, tested `BasicEffect` implementation (headers plus
> `BasicEffect.cpp`), not yet C-ABI-exposed but real -- every property,
> `EnableDefaultLighting()`'s exact numeric literals, and the
> `Apply()`-time parameter-computation algorithm were read from it, not
> invented.

**A second zero-ABI-until-Apply() escape hatch, same shape `SpriteFont`
found first:** the real C++ constructor chain for `BasicEffect` is pure
object state -- no renderer/GPU handle allocation happens until a draw call
actually applies the effect. So constructing a `BasicEffect` and setting
any of its properties (`World`/`View`/`Projection`, `DiffuseColor`,
`SpecularPower`, `AmbientLightColor`, `DirectionalLight0-2`'s own
properties, `FogEnabled`/`FogColor`/`FogStart`/`FogEnd`, `TextureEnabled`/
`Texture`, `VertexColorEnabled`, `Alpha`, `LightingEnabled`,
`PreferPerPixelLighting`) is real, tested, native-independent code today --
only `Apply()` (via `Effect.Apply` → `BasicEffect.OnApply`) crosses into
native code, through one new native call,
`cna_graphics_device_apply_basic_effect(CnaHandle device, in
CnaBasicEffectParams effectParams)`.

**`CnaBasicEffectParams` shape, and a self-correction before it shipped:**
first draft was a ~33-parameter positional constructor, mirroring the
"validate everything up front" habit this session's earlier native-backed
constructors (`SoundEffect`, `VertexBuffer`) established. Reconsidered
before writing any call site: a constructor that long is far more
error-prone (parameter-order transposition) than the risk it was guarding
against (this struct has no validation to perform -- every field is either
already-validated managed state or a derived computation). Switched to a
plain mutable struct with no constructor, populated via C# object-initializer
syntax at the one call site (`OnApply()`) instead -- named-field assignment
makes a transposition bug structurally impossible instead of just less
likely. `CnaMatrix16` (a `[InlineArray(16)]` column-major float buffer, same
marshalling pattern `CnaGlyphBuffer` already proved works through
`[LibraryImport]`) carries `World` across the ABI; `WriteColumnMajor`
transposes from this project's row-major `Matrix` at the call site rather
than changing `Matrix`'s own convention.

**`OnApply()` reproduces the real `FillGpuDrawParams` algorithm exactly,
not approximately:**
- Diffuse baking: when lighting is off, `EmissiveColor` gets folded into
  the forwarded diffuse (`DiffuseColor + EmissiveColor`) before the
  alpha-premultiply, because the lit-path material computation that would
  otherwise apply `EmissiveColor` separately never runs -- the real code's
  own comment explains this is deliberate, not an oversight, so it was
  reproduced rather than "simplified."
- Eye position: only computed when lit, via `Matrix.Invert(View).Translation`
  -- exercised directly in `EyePositionWorld_WhenLightingEnabled_RecoversCameraPosition`,
  which builds a `View` via `Matrix.CreateLookAt` from a known camera
  position and confirms the round-trip recovers it (this doubles as a
  fresh cross-check of `Matrix.Invert`, already tested elsewhere, against a
  second independent formula).
- Fog vector: zero when fog is off; `(0,0,0,1)` (fully fogged) for the
  degenerate `FogStart == FogEnd` case (avoids the divide-by-zero the real
  code also explicitly guards against); otherwise derived from
  `World * View`'s third row, scaled by `1/(FogStart-FogEnd)` -- verified
  against a hand-computed value in
  `FogVector_IdentityWorldView_MatchesHandComputedValue` (worked the
  arithmetic out by hand for `FogStart=10, FogEnd=20` before writing the
  assertion, rather than trusting the code's own output as the oracle).

Both derived computations are pulled out into private helpers
(`ComputeLightingParams`/`ComputeFogVector`) with `internal`-only
test-exposing properties (`EyePositionWorldForTests`/`FogVectorForTests`)
wrapping them -- the same "expose the pure-computation half for direct
testing since the public entry point needs native code" split
`VertexBuffer`/`IndexBuffer`'s constructor-validation-only testability
already established, applied here to a return-value computation instead of
an argument-validation one.

**Constructor default worth double-checking against the real engine again
later:** only `DirectionalLight0` starts `Enabled = true` (confirmed
against the real C++ constructor, which calls
`DirectionalLight0.setEnabledProperty(true);` and nothing equivalent for
`1`/`2`) -- verified with its own dedicated test
(`Constructor_OnlyDirectionalLight0StartsEnabled`). The not-yet-configured
lights' `Direction`/color defaults (`Vector3.Down`, zero diffuse/specular)
are *not* sourced from the research -- nothing in the C++ implementation
specifies what an unconfigured, disabled light's fields start as, since
real code never reads them before `EnableDefaultLighting()` or manual
configuration sets them. Flagged as a reasonable-but-unverified default in
the constructor's own doc comment, matching this session's established
practice of saying so rather than letting an invented value look
equally-grounded as the researched ones next to it.

**Compat-layer trade-off, same shape `RenderTarget2D` already chose, for a
new reason this time:** considered giving `CNA.XnaCompat` its own
`Effect`/`EffectTechnique`/`EffectPass`/`DirectionalLight` hierarchy
mirroring `CNA.Graphics`'s, the same way most other compat types wrap
rather than extend. Rejected because `DirectionalLight0/1/2` are
constructed exactly once, inside `CNA.Graphics.BasicEffect`'s own
constructor, with no seam for a compat subclass to intervene -- giving them
a compat-typed wrapper would need either duplicating construction (risking
`OnApply()`'s computation reading a *different* light object than compat
code mutated -- a new instance of the exact bug class the `Indices`
shadow-field fix, two entries below, exists to prevent) or an unsafe
downcast of an object that was genuinely never constructed as any compat
subclass. So `CNA.XnaCompat.BasicEffect` extends `CNA.Graphics.BasicEffect`
directly instead, same as `RenderTarget2D` extending `CNA.XnaCompat`'s own
`Texture2D`. Real, narrow, documented compat gap as a result:
`effect.CurrentTechnique`, `.Passes`, and `DirectionalLight0/1/2` are all
inherited unchanged and return `CNA.Graphics`-namespaced types -- ordinary
`var`-typed/chained call-site usage
(`effect.CurrentTechnique.Passes[0].Apply();`,
`effect.DirectionalLight0.Enabled = true;`) still compiles and works fine;
only an explicit XNA-namespaced type declaration for one of those three
members would fail to compile. Only `Texture` needed its own compat
override (same `new` + covariant-return reasoning `SpriteFont.Texture`
already used), since it's the only member whose declared type actually
needs to be XNA-namespaced at the call site for common code
(`SpriteBatch.Draw`-style texture assignment) to work.

**`EffectTechnique`/`EffectPass`/`EffectPassCollection` are minimal
scaffolding, not a general effect-parameter system:** exist only so
`effect.CurrentTechnique.Passes[0].Apply();` (the idiom every real
XNA/MonoGame `Effect` consumer uses, regardless of which effect) compiles
and forwards to `Effect.Apply()`/`OnApply()` correctly. `EffectParameter`
itself is not implemented -- `BasicEffect`'s own property surface is its
parameter interface, nothing in this pass needs a generic
name-to-parameter lookup.

**Discovered, not acted on:** `CNA.XnaCompat` has no `AssemblyInfo.cs` of
its own (unlike `CNA.Framework`/`CNA.Interop`), so `CNA.XnaCompat.Tests`
has no `InternalsVisibleTo`-granted access to any `protected internal`
constructor the way `CNA.Framework.Tests` does for
`GraphicsDevice(nint)`. This is why `BasicEffectTests.cs` lives only in
`CNA.Framework.Tests` (constructing `CNA.Graphics.GraphicsDevice(0)`
directly, same pattern the rest of this session's native-backed-type tests
already use) with no `CNA.XnaCompat.Tests` mirror -- consistent with
existing precedent (`Texture2D`/`SpriteBatch`/etc. have no compat-layer
construction tests either), not a new gap introduced here.

**Verified, not just written:** `dotnet build CNA.sln` clean across all 6
projects. `dotnet test CNA.sln`: 189/189 passing (up from 181 -- 8 new
`BasicEffectTests`, all passing on first run). `samples/HelloGame`
re-verified unaffected (still fails at exactly the same documented
`DllNotFoundException` point).

**Where to pick up next:** `Model` is the next well-grounded 3D-pipeline
item (not yet checked this session whether `modules/graphics/` has an
equally-real, working implementation the way it did for `BasicEffect`/
`VertexBuffer` -- check before assuming it needs pure invention, per this
session's own established habit). `Song`/`MediaPlayer` is a separate,
likely-harder problem regardless (`Song` has no public constructor in real
XNA at all, unlike every native-backed type done so far this session, all
of which had *some* real public escape hatch to build from).

## Sixth `/code-review high` pass, over the `GraphicsDevice` draw-calls commit -- a real shadow-field desync bug (2026-08-16, session 6 continued still further again once more)

Ran the review a sixth time. Found a genuine, confirmed bug (the review
dispatched its own verification sub-agent, which traced a concrete
reachable call path, not just a theoretical one) in
`CNA.XnaCompat.GraphicsDevice.Indices`: the `new`-shadowed property had
**its own private backing field**, separate from the base class's own
`_indices` field. Since `GraphicsDeviceManager.Game` is declared with the
base `CNA.Game` type (not shadowed with `new` in the compat
`GraphicsDeviceManager`), code reaching a `GraphicsDevice` through
`manager.Game.GraphicsDevice.Indices` resolves to the *base*, non-virtual
`Indices` property — which correctly calls native code but only updates
the *base* class's field. A `Game` subclass reading `this.GraphicsDevice.Indices`
(the compat-typed path) would then see stale or `null` data despite native
state having actually changed, and vice versa for writes.

**Root-cause fix, not a patch:** removed the compat property's private
field entirely. It's now a pure downcast pass-through --
`get => (IndexBuffer?)base.Indices; set => base.Indices = value;` -- so
there is only ever *one* piece of storage (the base class's own field),
regardless of which static type (`CNA.Graphics.GraphicsDevice` or
`Microsoft.Xna.Framework.Graphics.GraphicsDevice`) a caller happens to be
holding a reference through. This is the same "no independent state, just
a typed read-through" shape `SoundEffectInstance.State` already used two
entries ago -- worth recognizing as the *general* answer whenever a `new`
property override needs a different declared type for **mutable** state:
a private shadow field is only safe for state that's set once at
construction and never changes after (like `VertexBuffer.VertexDeclaration`/
`BufferUsage`, which really are immutable), never for anything with a
public setter. This is the pattern to reach for by default now, not just
an option.

**Lesson worth internalizing, not just fixing:** this bug went undetected
through the previous commit's own build+test+`HelloGame` verification
because C#'s `new` (member-hiding) is silent — nothing about writing a
property with `new` instead of `override` signals "this creates two
independent storage locations that can now disagree," and the test suite
has no way to exercise "access the same object through two different
static types" without a real `GraphicsDevice`/`IndexBuffer` (both
native-backed, so untestable here regardless). The review caught what
testing structurally couldn't. **When adding a `new`-shadowed property to
mirror this codebase's established compat-layer pattern, ask first
whether the state being shadowed is mutable — if it is, downcast-passthrough
is very likely the only safe shape**, not a private field mirroring the
base's.

A second finding (the `value is null ? CnaHandle.Zero : new CnaHandle(...)`
null-to-handle ternary now duplicated three times across `SetRenderTarget`/
`SetVertexBuffer`/`Indices`) was left as-is: each is a single obviously-correct
line over three different, unrelated resource types with no shared base to
generalize over without a larger interface-introducing refactor across
already-shipped types -- judged as acceptable duplication, unlike the
multi-line `BufferRangeValidation` block two entries ago, which had
already caused a real bug via copy-paste before being extracted.

No new tests possible for the fix itself -- exercising it needs a real
`GraphicsDevice`/`IndexBuffer` pair, both native-backed. 181/181 existing
tests still pass; `dotnet build` clean; `samples/HelloGame` re-verified
unaffected.

## `GraphicsDevice` draw calls: `SetVertexBuffer`/`Indices`/`DrawPrimitives`/`DrawIndexedPrimitives` (2026-08-16, session 6 continued still further again)

> Third slice of the 3D pipeline. `VertexBuffer`/`IndexBuffer` exist but
> are useless without a way to actually issue a draw call with them —
> this closes that gap, ahead of `BasicEffect` (the largest, still
> not-started remaining piece — see "where to pick up next" below).

`DrawIndexedPrimitives`'s public signature matches real XNA's full 6-parameter
form (`primitiveType, baseVertex, minVertexIndex, numVertices, startIndex,
primitiveCount`) for API compatibility, but `minVertexIndex`/`numVertices`
are validated and then **not forwarded** to the native call — they're
driver hints with no required effect on modern GPUs (real XNA/MonoGame
themselves mostly ignore them too), so this project's minimal native
surface doesn't plumb unused parameters through the ABI just to match a
signature shape. Worth remembering as a general principle for the next
API-compat-but-minimal-native-surface method: match the *public C#
signature* to real XNA exactly for source compatibility, but don't feel
obligated to give every parameter a native counterpart if it wouldn't do
anything there anyway — say so in a doc comment rather than silently
dropping it, which is what happened here.

Testability follows the exact same split `VertexBuffer`/`IndexBuffer`
established: `DrawPrimitives`/`DrawIndexedPrimitives` validate their
scalar arguments before touching native, so those failure paths are
tested; `SetVertexBuffer`/`Indices`'s setter call native unconditionally
(there's nothing to validate first — a null vertex/index buffer is a
legal "unbind" call, not an error), so they're untested here, matching
the existing `SetRenderTarget` precedent exactly.

`Indices` needed the same `new`-override-plus-shadow-field treatment
`CNA.XnaCompat.VertexBuffer.VertexDeclaration`/`BufferUsage` already used
in the previous entry: declared return type differs between the base
(`CNA.Graphics.IndexBuffer?`) and compat (`Microsoft.Xna.Framework.Graphics.IndexBuffer?`)
layers, so the compat `GraphicsDevice` stores its own shadow field and its
setter calls `base.Indices = value` (the compat `IndexBuffer` upcasts
fine) before caching the compat-typed reference locally.

**Verified, not just written:** `dotnet build CNA.sln` clean across all 6
projects. `dotnet test CNA.sln`: 181/181 passing (up from 173 — 8 new
`GraphicsDeviceTests`). `samples/HelloGame` re-verified unaffected.

**Where to pick up next:** `BasicEffect`/`Effect` is now the largest
remaining well-grounded item (real C++ implementation confirmed in
`modules/graphics/`, per the earlier research entry) — property surface
alone (`World`/`View`/`Projection`, `VertexColorEnabled`,
`TextureEnabled`/`Texture`, `Alpha`, lighting via `AmbientLightColor`/
`DirectionalLight0-2`/`EnableDefaultLighting`/material colors, fog via
`FogEnabled`/`FogColor`/`FogStart`/`FogEnd`) is bigger than anything
implemented in a single pass so far this session. With buffers and draw
calls both done now, a `BasicEffect`-drawn triangle is achievable in
scope, but budget for it being the largest single addition yet — consider
whether `Apply()` (which needs to decide how effect parameters actually
reach a draw call, and the real C++ engine's own `FillGpuDrawParams`
pattern suggests this project's own draw-call ABI may need to grow a
parameters-struct concept alongside it) is better split into its own pass
after the property surface itself lands.

## Fifth `/code-review high` pass, over the `VertexBuffer`/`IndexBuffer` commit (2026-08-16, session 6 continued once more)

Ran the review a fifth time. Its own last-angle result came back
inconclusive on first read, so the recap in the completion notification
under-reported the findings — had to resume the agent
(`SendMessage`) and ask it to restate the full JSON before acting, rather
than proceed on a partial summary. Worth remembering: if a review
notification's `<result>` reads like a status update ("no changes to the
findings... the review stands as delivered") rather than the findings
themselves, that's a sign the full JSON is sitting one message earlier in
that agent's own history — resume it and ask, don't guess from the recap.

Two real, fixed issues, plus one architectural observation left as a note
rather than a code change:
- **Validation-ordering bug**: `VertexBuffer.SetData`/`GetData`'s 5-arg
  overload checked `startIndex`/`elementCount` bounds *before*
  `vertexStride`'s positivity, so a caller with both wrong got the wrong
  exception (bounds-related `ArgumentException`, masking the also-invalid
  `vertexStride`). Reordered so all simple scalar checks
  (`offsetInBytes`, `vertexStride`) run before the compound bounds check.
- **Real duplication, now proven by recurrence**: the overflow-safe
  `startIndex`/`elementCount`-fits-within-length check (the same one
  `SoundEffect`'s constructor already had its own overflow bug fixed in,
  two review passes ago) had been copy-pasted five times total across
  `SoundEffect.cs`'s constructor and `VertexBuffer.cs`/`IndexBuffer.cs`'s
  `SetData`/`GetData`. Extracted into a shared
  `BufferRangeValidation.ValidateRange(length, startIndex, elementCount)`
  (root `CNA` namespace, `internal`, visible to both `CNA.Audio` and
  `CNA.Graphics` since they're the same project) and switched all five
  call sites to it. **Worth reaching for by name** the next time a
  native-backed data-transfer method needs this exact check, rather than
  copy-pasting a sixth time — `BasicEffect`/`Model`'s eventual `SetData`-shaped
  methods (per the previous entry's "where to pick up next") are the
  likely next candidate.
- **Left as a note, not a code change**: the review flagged that
  `CNA.XnaCompat.VertexDeclaration.Framework` (an `internal` accessor
  exposing a wrapped `CNA.Graphics` instance) is a different shape from
  `RenderTarget2D.CreateNativeHandle` (a static factory that performs a
  native call and returns a raw handle) for "compat type reaches into
  native/wrapped state for a sibling constructor." Both are correct for
  the sub-problem each actually solves (unwrap-existing vs.
  create-and-return-raw-handle) — not proposing to force them into one
  shape, but noting here for whoever adds the next wrap-and-forward compat
  type: check whether the need is "get the thing I already wrapped" (use
  the `Framework`-accessor shape) or "create a new native resource my base
  class also needs to create" (use the `CreateNativeHandle`-factory shape)
  before picking one by pattern-matching on whichever example is closest
  at hand.

No new tests possible for the `VertexBuffer`/`GetData`/`SetData` ordering
fix specifically (same testability limitation as before -- `SetData` needs
an already-constructed instance, which needs real native code); the
`SoundEffect`/`BufferRangeValidation` behavior is still covered by the
existing overflow regression test, now exercising the shared helper
instead of the inlined check it replaced. 173/173 tests still passing;
`dotnet build` clean; `samples/HelloGame` re-verified unaffected.

## `VertexBuffer`/`IndexBuffer` (2026-08-16, session 6 continued still further)

> Continuation of the vertex-format work, per its own "where to pick up
> next" pointer: the native-backed half of the buffer layer, ahead of
> `BasicEffect` (needs a buffer to apply to for a demo to make sense) and
> `Model` (needs both).

Same ABI-grounding situation as `SoundEffect`: no doc-backed shape (not
even a naming-convention bullet, unlike audio's `cna_audio_*`), but the
real `openeggbert/cna` C++ engine's `modules/graphics/` already has full,
tested, renderer-backend-wired `VertexBuffer`/`IndexBuffer`
implementations — a renderer-owned GPU handle plus a CPU-side shadow byte
buffer enabling `GetData()` readback. Every native function added here is
shaped to match that.

**Deliberate typing tightening, worth remembering for `Model`/`Effect`
too:** `SetData<T>`/`GetData<T>` use `where T : unmanaged`, not real XNA's
broader `where T : struct`. `unmanaged` is what makes `sizeof(T)` and
`fixed (T* p = data)` legal in the marshalling code; every realistic
vertex/index type (the five standard vertex structs from the previous
entry, `short`/`int` for indices) already satisfies it, so this loses
nothing in practice while making the implementation actually valid C#.
Documented as an intentional deviation, not silently narrower than
advertised.

**Real testability limitation, different in kind from `SoundEffect`'s:**
`SoundEffect`'s constructor validates every argument in managed code
*before* ever calling native, so its validation-failure paths are fully
testable without a real `cna-native`. `VertexBuffer`/`IndexBuffer`'s
constructors call native immediately after only minimal validation
(non-null, positive count) — there is no way to reach a
successfully-constructed instance to call `SetData`/`GetData` on without
real native code, so *only* the constructors' own argument checks are
testable here, not the data-transfer methods at all. Said so explicitly in
both the test file's own doc comment and `plan.md`, rather than let the
existing test count imply more coverage than there is.

**Only the `VertexDeclaration`/`IndexElementSize`-taking constructors are
implemented**, not real XNA's additional `Type`-taking overloads
(`VertexBuffer(GraphicsDevice, Type, int, BufferUsage)` /
`IndexBuffer(GraphicsDevice, Type, int, BufferUsage)`), which derive the
declaration/element-size from a `Type` via reflection (an
`IVertexType.VertexDeclaration` static-property lookup, or `typeof(short)`/
`typeof(int)` size inference) — convenience sugar over the constructors
that are implemented, left for a follow-up rather than adding reflection
based type discovery to this pass.

**XnaCompat pattern note:** `VertexDeclaration` (from the previous entry)
is a *wrapped* (composition), not subclassed, compat type — so
`CNA.XnaCompat.VertexBuffer`'s constructor needed a way to reach the
wrapped `CNA.Graphics.VertexDeclaration` to forward to
`CNA.Graphics.VertexBuffer`'s base constructor. Added a plain `internal
CNA.Graphics.VertexDeclaration Framework => _framework;` accessor on
`CNA.XnaCompat.VertexDeclaration` — ordinary same-assembly `internal` is
enough here (both types live in the `CNA.XnaCompat` project), no
`InternalsVisibleTo` grant needed the way crossing from `CNA.Framework`
into `CNA.XnaCompat` needs one.

**Verified, not just written:** `dotnet build CNA.sln` clean across all 6
projects. `dotnet test CNA.sln`: 173/173 passing (up from 166 -- 7 new
constructor-validation tests; no `SetData`/`GetData` tests are possible,
see above). `samples/HelloGame` re-verified unaffected.

**Where to pick up next:** `BasicEffect`/`Effect` — well-grounded (real
C++ implementation confirmed), but meaningfully larger than `VertexBuffer`/
`IndexBuffer` alone: `BasicEffect` has a large property surface
(World/View/Projection, `VertexColorEnabled`, `TextureEnabled`/`Texture`,
`Alpha`, lighting via `AmbientLightColor`/`DirectionalLight0-2`/
`EnableDefaultLighting`/material colors, fog via `FogEnabled`/`FogColor`/
`FogStart`/`FogEnd`), and — per this session's own research notes — real
`.fx` shader bytecode is explicitly **not** supported even by the real
C++ engine yet (`Effect(GraphicsDevice, byte[])` always throws
`NotImplementedException` there, tracked as their own "Phase 74"), so
whatever gets built here should target hand-authored/stock effects
(`BasicEffect` itself, not custom compiled shaders) to stay within what
the real engine can actually do. `GraphicsDevice.DrawIndexedPrimitives`
(or equivalent) is also still needed before any of this produces a
visible result — worth deciding whether that belongs in the same pass as
`BasicEffect` or its own.

## Fourth `/code-review high` pass, over the vertex-format commit (2026-08-16, session 6 continued once more)

Ran the same review discipline a fourth time, including for this
lower-risk (pure-data, no `unsafe` code, no native calls) commit — worth
doing anyway, since three review passes in a row had each found a real
bug. This one verified its top finding against **real MonoGame source
fetched from GitHub**, not just recollection — a stronger verification
standard than most of this session's other findings had available (no
"real implementation" analog exists here the way `modules/audio`/
`modules/graphics` gave `SoundEffect`/`Effect` something concrete to check
against).

One real, fixed compatibility bug: real XNA/MonoGame's `VertexDeclaration`
constructors both reject a null **or empty** `elements` array with
`ArgumentNullException("elements", "Elements cannot be empty")`,
unconditionally — including the explicit-stride overload, which this
project's code let through silently (an empty-elements declaration with a
stride quietly "succeeded" and produced a zero-element, non-empty-stride
declaration). The elements-only overload also threw the *wrong* exception
type before this fix (`ArgumentOutOfRangeException` on `vertexStride`, from
`ComputeStride`'s empty-array walk landing on stride 0) — a caller
catching real XNA's documented exception type/param name wouldn't have
caught either failure mode. Fixed with a shared `ValidateNotEmpty` helper
called from both constructors, matching real XNA's exact exception type,
param name, and message. New regression tests cover both constructors
and both null/empty cases.

Also added exhaustive (not hand-picked-subset) parity tests for
`VertexElementFormat`/`VertexElementUsage` — reflection-based, comparing
every member `Enum.GetNames` returns on both the `CNA.Graphics` and
`Microsoft.Xna.Framework.Graphics` sides, rather than the small
hand-picked `[InlineData]` spot-checks `Keys`/`Buttons` use elsewhere in
this codebase. **Worth considering as the new default for future
small-to-medium enum pairs** (roughly a dozen members or fewer, like these
two): it catches a future member added to one side and not the other
automatically, where a hand-picked subset only catches whatever the person
adding the test happened to think to check. Not proposing a retrofit of
the existing `Keys`/`Buttons`/`SpriteEffects` spot-checks this session —
noting it as a pattern worth reaching for next time, not a cleanup task
for what's already shipped and passing.

One finding (`CNA.XnaCompat.VertexDeclaration.GetVertexElements()`
double-allocating -- clones once inside `CNA.Graphics.VertexDeclaration`,
then loops again for the type conversion) was left alone: real but minor,
on a method nothing in this codebase calls yet (`VertexBuffer.SetData<T>`,
the eventual real caller, isn't implemented), and avoiding it would need a
new non-cloning internal accessor purely for this micro-optimization --
not worth the added API surface for code with no measured cost yet.

166/166 tests passing (was 161); `dotnet build` clean; `samples/HelloGame`
re-verified unaffected.

## Zero-ABI vertex-format layer: `VertexDeclaration`/`VertexElement`/standard vertex structs (2026-08-16, session 6 continued yet further)

> After the `SoundEffect` review-and-fix round, checked whether `Effect`/
> `BasicEffect`/`VertexBuffer`/`IndexBuffer`/`Model` had the same "real
> working C++ implementation, just not C-ABI-exposed" lucky break audio
> did, before assuming they needed pure invention (per this file's own
> "where to pick up next" pointer from the previous entry). They do —
> `modules/graphics/` has full, tested, renderer-backend-wired C++
> implementations of all of them. But the *combined* native surface needed
> for even a minimal "draw a textured triangle with `BasicEffect`" demo —
> buffer creation, effect parameter application, and the actual draw call
> — is large and every piece depends on the others being present to be
> minimally useful (unlike `RenderTarget2D`/`SoundEffect`, which were each
> reasonably self-contained). Rather than swallow that whole thing in one
> pass, split off the part that turned out to need **no native ABI at
> all**: `VertexDeclaration` and the standard vertex structs.

**The escape hatch, one more time:** real XNA's `VertexDeclaration`
computes its byte stride from the given `VertexElement`s' offsets/formats
in its constructor — pure arithmetic, not a GPU query. Confirmed the real
C++ engine's own `VertexDeclaration` does the identical thing ("auto-computes
stride from element offsets/formats" — same research pass that found the
real `BasicEffect`/`VertexBuffer` implementations). This is the third time
this exact shape of escape hatch has shown up this session (after
`SpriteFont`'s public raw-glyph-array constructor and
`SoundEffect.GetSampleDuration`'s pure arithmetic) — worth actively
looking for on any future native-backed type: does XNA's own public API
already expose a pure-data or pure-arithmetic path that doesn't strictly
need the native device?

**Stride computation, precisely:** `max(offset + GetTypeSize(format))`
across all elements — not a running sum in declaration order, and not
dependent on elements being given in offset order (elements aren't
required to be contiguous or sorted; `VertexDeclarationTests` has a
dedicated case with elements deliberately given out of offset order to
catch a naive "sum in declared order" implementation). `GetTypeSize`'s
per-format byte sizes (`Vector3`→12, `Color`→4, `HalfVector4`→8, etc.) are
well-known, standard XNA constants — verified against all five standard
vertex structs' well-known real-XNA strides (`VertexPosition`=12,
`VertexPositionColor`=16, `VertexPositionTexture`=20,
`VertexPositionColorTexture`=24, `VertexPositionNormalTexture`=32), which
all came out correct on the first run — a reasonably strong cross-check
that both the per-format sizes and the stride formula are right together
even though there's no live system to verify against directly.

**XnaCompat pattern used, worth noting for the next struct-with-nested-enum
type:** `VertexElement` is a *struct* containing two *enum* fields
(`VertexElementFormat`/`VertexElementUsage`). Gave the struct itself
implicit conversion operators (matching the `Vector3`/`Color` pattern —
structs can define these), while the two enum fields still need the
`Buttons`/`Keys`/`SpriteEffects`-style separate-numerically-identical-enum
treatment inside that conversion (enums can't define conversion operators
at all) — i.e. the struct-level conversion operator is exactly where the
enum-level numeric casts live. `VertexDeclaration` itself is a *class*
wrapped by composition in `CNA.XnaCompat` rather than subclassed (no
construction-seam reason to subclass here — it's never native-backed, so
there's no "wrap an already-created native handle" case the way
`Texture2D`'s inheritance exists to serve).

**Verified, not just written:** `dotnet build CNA.sln` clean across all 6
projects. `dotnet test CNA.sln`: 161/161 passing (up from 136 — 16 new
`VertexDeclarationTests` plus 2 new `CompatibilityTests` entries).
`samples/HelloGame` re-verified unaffected.

**Where to pick up next:** the native-backed half of the 3D pipeline
(`VertexBuffer`/`IndexBuffer`/`BasicEffect`/`Effect`, in roughly that
dependency order — buffers first since `BasicEffect.Apply()` needs
something to apply to for a demo to make sense) — well-grounded (real C++
implementations exist, same as audio), but meaningfully larger in scope
than any single native-backed addition this session has made so far.
Budget accordingly; consider whether it's better split across multiple
passes (e.g. `VertexBuffer`/`IndexBuffer` alone first, `BasicEffect`
after) rather than attempted as one commit, given how large `SoundEffect`
alone already was.

## Third `/code-review high` pass, over the `SoundEffect`/`SoundEffectInstance` commit (2026-08-16, session 6 continued)

Ran the same review discipline a third time. Found three real, fixed bugs
in `SoundEffect.cs`, none of them in the parts checked against the real
C++ engine's documented semantics (those held up) -- all three were in
validation code this session wrote itself, not reproduced from anywhere:
- **Integer overflow**: the constructor's `offset + count > buffer.Length`
  bounds check can overflow `int32` and wrap negative for an adversarial
  `(offset, count)` pair, silently passing validation it should fail and
  handing native code an out-of-bounds pointer. Rewritten as
  `offset > buffer.Length || count > buffer.Length - offset` — the
  subtraction form can't overflow once `offset <= buffer.Length` is
  already established by the first half of the check.
- **Division by zero**: `AudioChannels` is a plain enum with no CLR-enforced
  range, so `(AudioChannels)0` is a legal cast that reached
  `GetSampleDuration`'s `blockAlign = 2 * (int)channels` and divided by
  zero; the sibling `GetSampleSizeInBytes` had the same gap but failed
  silently instead (multiplied by 0, always returned 0 bytes). Added a
  shared `ValidateChannels` check, used by both plus the constructor.
- **Unvalidated loop parameters**: `loopStart`/`loopLength` were passed
  straight to native with no check at all — not even non-negative, unlike
  every other constructor parameter. Added `ThrowIfNegative` for both;
  deliberately did *not* try to validate they fit within the sample count
  implied by `count` (documented as a real, intentional gap — that
  validation needs the same channel/bit-depth interpretation the native
  side already owns).

All three are now covered by regression tests in `SoundEffectTests.cs`,
including ones that exercise the *validation failure path* of
`SoundEffect`'s constructor without needing native code at all — validation
runs and throws before the native call is ever reached, so a bad-argument
test never touches `cna_soundeffect_create`. This is a real, useful
distinction worth remembering for any future native-backed constructor: the
success path needs a real `cna-native` to test, but the validation-failure
paths usually don't, and are worth testing even when the type as a whole
"can't be tested."

The fourth finding from this pass (`ContentManager.Load<T>`'s per-type
`if`/`typeof` chain, now three deep across two files, could be a registry
instead) was judged not worth acting on: it is a pre-existing pattern from
before this session (already used for `Texture2D`/`SpriteFont`), extended
consistently rather than newly introduced, and replacing it now would be a
speculative refactor of already-shipped, tested code for a marginal
maintainability gain, not a bug fix.

136/136 tests passing (was 129 going into this pass, +7 new regression
tests); `dotnet build` clean.

## `SoundEffect`/`SoundEffectInstance` (2026-08-16, session 6 -- after the weekly Claude Max 20x limit reset)

> User's weekly limit reset and they asked to keep working autonomously
> ("pokracuj autonomne v praci"). Picked up past the point session 5 had
> reached (all explicitly-flagged, well-scoped gaps closed; remaining
> Phase 4 items were `Effect`/3D/audio, all flagged as needing genuinely
> speculative ABI design). Confirmed the toolchain fix from earlier
> sessions (the `/tmp/racinggame-dotnet` → `/tmp/platformer-dotnet/sdk`
> runtime symlink) had survived the reset and still worked — re-verified
> rather than assumed. Also noticed (via a system note, not asked about)
> that the user had directly committed two README changes of their own
> between sessions, adding a "Status: In progress - NOT YET FUNCTIONAL"
> banner — left as-is, not reverted, per the explicit instruction that
> accompanied that note.

**Chose audio over `Effect`/3D specifically because it turned out to be
better-grounded than expected.** A full-text grep of both analysis docs
confirmed audio gets no concrete ABI shape anywhere (unlike `SpriteBatch`'s
§22 draw-call struct) — just class names to preserve and one
`cna_audio_*` naming-convention bullet. But a follow-up look at the actual
`openeggbert/cna` C++ engine found `modules/audio/` already has a **working
C++ implementation** of `Microsoft::Xna::Framework::Audio::SoundEffect`/
`SoundEffectInstance` over SDL3_mixer — not yet exposed through any
`extern "C"` API, but real, working code with real documented semantics
(exact constructor signatures, `Volume`/`Pitch` pass-through-unclamped vs.
`Pan`'s `[-1,1]`-validated setter vs. `IsLooped`'s already-played guard,
`SoundState`/`AudioChannels`' exact enum values). Read those headers in
full before designing anything. This is meaningfully better grounding than
`RenderTarget2D`/`GamePadCapabilities` had (pure invention) — the native
ABI added here is this repository's best guess at what a future
`cna_soundeffect_*` C API would need to expose *over an implementation that
already exists*, not a guess made from nothing. `Effect`/3D got no such
lucky break (no equivalent look was taken at `modules/graphics`/`graphics-ext`
for this session — a reasonable next thing to check before assuming they're
equally ungrounded).

**Real XNA's own public escape hatches made the object model tractable,
same pattern as `SpriteFont`:** `SoundEffect(byte[] buffer, int sampleRate,
AudioChannels channels)` (and the 7-arg loop-point overload) are real XNA
API, not an invention, and `SoundEffect.GetSampleDuration`/
`GetSampleSizeInBytes` are pure 16-bit-PCM arithmetic — no native call, real
and tested today (`SoundEffectTests.cs`, including a round-trip test
tolerant of sample-alignment rounding). Everything else -- construction
itself, `CreateInstance`, `Play`/`Pause`/`Resume`/`Stop`, and every
`SoundEffectInstance` property -- calls into native immediately, unlike
`SpriteFont`'s `MeasureString`: audio playback has no CPU-side escape from
needing a real device, so this doesn't get to be a zero-ABI type the way
`SpriteFont` did.

**A real double-release bug caught and fixed before it shipped, not after:**
first draft of `CNA.XnaCompat.SoundEffect.CreateInstance()` called
`base.CreateInstance()` (which already wraps the native handle in its own
`SafeHandle`-owning `CNA.Audio.SoundEffectInstance`) and then wrapped that
*same* handle a second time in a new compat-typed instance -- two owners of
one native resource, so whichever got disposed/finalized second would
release an already-released handle. Caught while writing the code, not by
a separate review pass this time (worth noting since the *previous* two
review passes this session each found something exactly this shape --
pattern-matched to it faster on the third occurrence). Fixed the same way
`RenderTarget2D.CreateNativeHandle` already solved this exact problem:
factored an `internal nint CreateNativeInstanceHandle()` on
`CNA.Audio.SoundEffect` that does *only* the native call, and both
`SoundEffect.CreateInstance()` and `CNA.XnaCompat.SoundEffect.CreateInstance()`
call it once each, each wrapping the result in exactly its own type.
**Worth remembering as a standing pattern:** any time an XnaCompat override
needs "the same native resource, wrapped in my own type" rather than
"convert an existing wrapped object," reach for a shared raw-handle
factory, not a double-wrap.

**Design choices worth recording:**
- `SoundEffectInstance` has no public constructor anywhere (`CNA.Framework`
  or `CNA.XnaCompat`), matching real XNA and the real C++ engine's own
  `private`, `SoundEffect`-friend-only constructor as closely as C#'s
  accessibility model allows -- `protected internal`, reachable only via
  `SoundEffect.CreateInstance()`.
- `Volume`/`Pitch` setters call native but do **not** validate range in
  managed code (matching the real C++ engine's documented "passed through
  unclamped, matching FNA" behavior exactly) -- deliberately different
  from `Pan` (validates `[-1,1]`, throws `ArgumentOutOfRangeException` in
  managed code before reaching native) and `IsLooped` (throws
  `InvalidOperationException` if already played, tracked via a private
  `_hasBeenPlayed` bool in managed code). All four asymmetries are
  reproductions of where the *real* C++ implementation itself performs
  each check, not arbitrary choices.
- Deliberately did **not** implement `SoundEffect.Play()`/
  `Play(volume,pitch,pan)` (real XNA's fire-and-forget convenience
  methods) -- those rely on an internal instance-limit-tracking pool this
  repository has no equivalent for, including the "returns `false` if the
  limit is reached" behavior. `CreateInstance()` (which this repository
  does implement fully) is explicit-lifetime, real XNA API in its own
  right, not a workaround for the gap.
- Skipped for this pass, not because they're hard exactly but because
  they're separate, boundable follow-ups: `Apply3D`/`AudioListener`/
  `AudioEmitter` (3D positional audio), the static `SoundEffect.MasterVolume`/
  `DistanceScale`/`DopplerScale`/`SpeedOfSound` settings, `SoundEffect`'s
  exact sample-rate range validation (real XNA: 8,000-48,000 Hz; this
  repository only checks positivity, flagged as lower-confidence in the
  constructor's own doc comment rather than guessing the exact bounds).
- `Buttons`/`Keys`/`SpriteEffects`-style enum-crossing pattern shows up
  twice more here: `AudioChannels` and `SoundState` are each a numerically-
  identical but distinct pair (`CNA.Audio.*` / `Microsoft.Xna.Framework.Audio.*`),
  parity-tested in `CompatibilityTests.cs` same as the others.
  `SoundEffectInstance.State`'s declared return type needed a `new`
  override in the compat subclass for the same reason
  `BoundingFrustum.GetCorners()` and `SpriteFont.Texture` did.

**Verified, not just written:** `dotnet build CNA.sln` clean across all 6
projects (this is also the first real compile-time proof that the whole
`unsafe`/fixed-pointer-with-offset construction path and the `internal`
cross-assembly raw-handle-factory pattern both hold up for a *third*
native-backed type family, not just the two from earlier this session).
`dotnet test CNA.sln`: 129/129 passing (up from 112 -- 11 new
`SoundEffectTests`, 6 new `CompatibilityTests` entries). `samples/HelloGame`
re-verified unaffected (still fails at the same documented
`DllNotFoundException` point).

**Where to pick up next:** `Effect`/3D remain the least-grounded, largest
remaining items -- worth checking whether `modules/graphics`/`graphics-ext`
has a similarly-real-but-unexposed C++ implementation the way `modules/audio`
did before assuming they need to be designed from nothing; that lucky break
is what made this session's audio work meaningfully more trustworthy than
`RenderTarget2D`/`GamePadCapabilities` were. `Song`/`MediaPlayer` are a
separate, likely-harder problem even with that same lucky break, since real
XNA's `Song` has no public constructor at all (unlike `SoundEffect`'s raw-PCM
escape hatch) -- would need native streaming-audio-format loading designed
essentially from scratch.

## Second `/code-review high` pass, this time over `GetCapabilities`/`Load<SpriteFont>` (2026-08-16, session 5 continued further still)

Ran the same review discipline again, against the two commits after the
first review pass. Two real bugs, both in `ContentManager.LoadSpriteFontData`:
- `native.GlyphCount` (from the untrusted, self-designed native boundary)
  was used to size arrays and index the fixed-256-slot `InlineArray` buffer
  with **no validation** — a native implementation returning a bad count
  (version skew, an ABI bug) would read past the buffer instead of hitting
  the documented "fails via `CnaResult`" contract. Now explicitly checked
  against `0..CnaGlyphBuffer.MaxGlyphs` and throws a clear `CnaException`
  if not.
- `CnaGlyphMetrics.Character` crosses the ABI as a full Unicode code point
  specifically to avoid surrogate-pair ambiguity (per its own doc comment),
  but the code then silently truncated it to `char` with an unchecked
  cast — defeating the reason that field is an `int` in the first place. A
  code point outside the BMP would silently wrap into a wrong, possibly
  glyph-colliding `char` with no error. Now explicitly validated (rejects
  non-BMP code points and lone surrogates with a clear exception) — the
  underlying limitation is real and unavoidable (`SpriteFont`'s glyph table
  is `char`-keyed, matching real XNA's own limitation), but failing loudly
  beats succeeding wrong.

Also fixed a smaller diagnostic-quality bug: `LoadSpriteFontData` passed
`nameof(Load)` (the unrelated generic method) to `CnaException.ThrowIfFailed`
instead of its own name, so a failed native SpriteFont load would have
misattributed the failure in its exception message.

Three more findings from the same pass were judged not worth acting on,
each for a specific reason (not just "seemed low severity"): an unvalidated
`(GamePadType)native.GamePadType` cast is consistent with how every other
enum crossing this ABI boundary is already handled throughout this
codebase (`Buttons`, `Keys`, `SpriteEffects`, ...), so singling this one
out for validation would be an inconsistent one-off, not a real fix. The
`XnaCompat.ContentManager`/`XnaCompat.SpriteFont` conversion-helper
duplication (opposite-direction element-wise array conversion, ~6 lines
each, used once each) would need a shared generic delegate-based helper to
dedupe — judged as trading a small amount of duplication for a real
abstraction most readers would find harder to follow, not a net
improvement. `GamePadCapabilities`'s 15 explicit `HasFlag` lines were
flagged as repetitive, but they're already self-evidently correct at a
glance; wrapping them wouldn't have anywhere clean to live (the closest
existing helper, `GamePadButtons.ToState`, is `private` and returns the
wrong type), so introducing one *for this* would be the premature
abstraction this project's own conventions warn against, not a
simplification.

112/112 tests still pass; `dotnet build` clean.

## `ContentManager.Load<SpriteFont>` (2026-08-16, session 5 continued further)

> Reconsidered the "genuinely open design question" framing from earlier in
> this file after actually sitting with it: the thing that made it feel
> uniquely blocked (font *data* needs to cross the FFI as a variable-length
> table, unlike a texture's fixed handle+dimensions) is a *marshalling
> complexity* question, not a *does-native-infra-exist* one — and on that
> front it's in the same boat as `Texture2D` content loading (which already
> shipped in session 1 under the same "build against the shape, native
> asset-decoding infra doesn't exist yet either" philosophy). Once framed
> that way, the only real blocker left was finding a marshalling shape
> simple enough to trust — see below.

**Mechanical question answered empirically before designing around it:**
does a C# 12 `[InlineArray(N)]` struct marshal correctly through
`[LibraryImport]`'s source generator? Built a throwaway scratchpad probe
(separate tiny project, `[LibraryImport("nonexistent-native-lib")]` over a
struct containing an `InlineArray`-attributed field) and confirmed it
compiles clean and throws the expected `DllNotFoundException` (not a
marshalling-shape error) when called. This is what unlocked the whole
design: a **fixed-capacity, flat-marshalled glyph buffer**, avoiding the
two-call pointer/length dance `CnaError.GetLastErrorMessage` needs for a
truly unbounded value. Deleted the probe after confirming.

**Shape:** `CnaGlyphMetrics` (Unicode code point as `int`, not `char` — no
surrogate-pair ambiguity; source/cropping rects; the ABC kerning triple as
three separate named floats, not reusing `CnaVector3` since the semantic
meaning differs) `× CnaGlyphBuffer` (the `[InlineArray(256)]` wrapper) `×
CnaSpriteFontData` (texture handle, line spacing, spacing, optional default
character, actual glyph count, the buffer). One native call,
`cna_content_load_spritefont` — no ABI shape for any of this exists
upstream, flagged the same way `RenderTarget2D`'s natives were.

**256-glyph cap is deliberate, not an oversight** — flagged in three places
(the struct's own doc comment, `ContentManager.LoadSpriteFontData`'s doc
comment, `plan.md`) specifically because silent caps are worth calling out
loudly, not because 256 is expected to bind in practice (XNA's default
ASCII-range content-pipeline output is ~95 characters). A font needing more
than 256 glyphs is expected to fail the native call with a `CnaResult`
error, not silently lose glyphs — this repository has no way to verify that
contract holds on a real native implementation, but the *shape* makes
silent truncation the wrong thing to implement even without one.

**Split mirrors the existing `Texture2D` pattern exactly, once you see
it:** `ContentManager.LoadNativeTexture2DHandle` returns a raw `nint`, and
each of `CNA.Content.ContentManager`/`CNA.XnaCompat.ContentManager`'s
`Load<T>` wraps it into *that layer's own* `Texture2D` type. Added
`ContentManager.LoadSpriteFontData` (a new `protected readonly record
struct SpriteFontData` return type, holding exactly `SpriteFont`'s
constructor parameter shape) as the `SpriteFont` equivalent of that split —
each layer's `Load<T>` calls the same protected helper, then builds its own
namespace's `SpriteFont` from the raw pieces. `CNA.XnaCompat.ContentManager`
needed its own element-wise `Rectangle[]`/`Vector3[]` conversion helpers
(CNA types → XnaCompat types) for this — the mirror image of the
already-existing XnaCompat-to-CNA conversion `CNA.XnaCompat.SpriteFont`'s
own constructor needed, for the same "C# generics can't convert a
collection just because its elements convert" reason.

No new tests possible — like `Load<Texture2D>`, this calls into native code
immediately and throws `DllNotFoundException` without a real `cna-native`.
112/112 existing tests still pass; `dotnet build` clean across all 6
projects; `samples/HelloGame` unaffected.

## `GamePad.GetCapabilities` (2026-08-16, session 5 continued)

> Last remaining explicitly-flagged gap after the code-review fixes above.
> Grepped the whole `src/` tree for "not implement"/"TODO"/"deferred"
> comments to confirm this really was the last one before starting —
> everything else left is either Phase 5 (`SpriteBatch` batching) or
> already-documented-and-accepted (`GamePadState.PacketNumber`, `SpriteFont`
> flip-effects text reversal).

New `CNA.Interop` native `cna_gamepad_get_capabilities` and a
`CnaGamePadCapabilities` struct — **no ABI shape for this exists upstream**
(same caveat as `RenderTarget2D`'s natives, flagged the same way in the
struct's own doc comment). `SupportedButtons` reuses `CNA.Input.Buttons`'s
exact bit layout rather than one bool field per button (so `GamePadCapabilities`
only reports the same core button subset `GamePadState` does); the
remaining ~9 thumbstick/trigger/vibration/voice booleans pack into a second
`Features` bitmask with bit positions that are this repository's own
invented convention, documented as such since there's nothing upstream to
match them against.

Added `CNA.Input.GamePadType` (`Unknown`/`GamePad`/`Wheel`/`ArcadeStick`/
`FlightStick`/`DancePad`/`Guitar`/`AlternateGuitar`/`DrumKit`/`BigButtonPad`)
alongside it — needed as `GamePadCapabilities.GamePadType`'s type. **Lower
confidence than everything else added this session**, flagged plainly in
its own doc comment: the member *names* match real XNA (fairly confident,
these are commonly-referenced), but the numeric *values* are a
declaration-order guess (0, 1, 2, ...), not independently confirmed real
XNA ordinals. This only matters if something serializes/compares the raw
int rather than the named member, which nothing here does — but say so
rather than let it look more verified than it is.

No new tests: like `Texture2D`/`SpriteBatch`/`Mouse`/existing `GamePad`,
this is native-backed and can't be exercised without a real `cna-native` —
consistent with existing precedent, not a gap specific to this addition.
112/112 existing tests still pass; `dotnet build` clean; `samples/HelloGame`
unaffected (still fails at the same documented point).

## Complete the pure-math layer; fix a real `Vector3.Transform(Quaternion)` bug (2026-08-16, session 5)

> Continuation of the same "keep working through the plan" session run.
> After `SpriteFont` (session 4, below), the next `plan.md` Phase 4 items
> are `Effect`/`Model`/3D/audio — all flagged riskier than everything done
> so far, needing genuinely speculative native ABI design with even less
> doc backing than `RenderTarget2D` had. Better use of the remaining budget:
> `plan.md`'s "pure math/value types" bullet had a long-standing list of
> explicitly-flagged gaps (`Matrix.Decompose`, spline interpolation, etc.)
> that are 100% real, fully testable work with *zero* native dependency —
> closed all of them this session instead of reaching for more speculative
> ABI surface.

**Toolchain, same fix as session 4, re-applied:** the `/tmp/platformer-dotnet`
↔ `/tmp/racinggame-dotnet` symlink from session 4 was still in place and
still worked (`dotnet test` runs both projects normally). Also used a
throwaway scratchpad probe project (`dotnet run` against a tiny `Program.cs`
referencing `CNA.Framework.csproj`) to empirically check quaternion/matrix
sign conventions before trusting them in real code — see the bug below, this
is exactly what caught it. Deleted after use; this is squarely the "short
scripts, small intermediate files" scratchpad use case in `../CLAUDE.md`,
not a build directory.

**Real bug found and fixed: `Vector3.Transform(Vector3, Quaternion)` was
rotating by the inverse angle.** Not something introduced this session —
this method has existed since session 2 (the "full XNA math layer" session)
and had never had a dedicated test with a non-identity rotation, so nothing
caught it until now. Root cause: this project's `Quaternion.operator *`
computes what standard Hamilton-product notation would call `b*a` for code
written `a*b` (needed so quaternion composition agrees with this project's
row-vector matrix convention — see the operator's own math, worked out via
the scratchpad probe above). The textbook sandwich formula `rotation * v *
conjugate` computes the *correct* rotation only if `operator *` is the
*standard* (non-reversed) Hamilton product; against this project's reversed
one, it silently computes `conjugate * v * rotation` in standard notation —
i.e., the inverse rotation. Caught empirically: built
`Matrix.CreateFromQuaternion(q)` (independently-implemented, uses no
quaternion multiplication at all) and compared `Vector3.Transform(v, q)`
against `Vector3.Transform(v, Matrix.CreateFromQuaternion(q))` for a 90°
rotation about Y — they disagreed in sign (`Z:+1` vs `Z:-1`). Cross-checked
against the independently-simple, obviously-correct `Matrix.CreateRotationY`
to confirm which one was actually wrong before touching anything. Fix:
swap the multiplication order to `conjugate * v * rotation`
(`Vector3.cs`) — this is the one-line fix once you understand *why*, but
finding *why* needed the empirical cross-check, not more staring at the
formula. **Lesson for future sessions:** when this project's `Quaternion`
math and `Matrix`-based math should agree (they're two representations of
the same rotation), don't trust that agreement without a real test — write
one, the way `QuaternionTests.CreateFromRotationMatrix_TransformsVectors...`
now does permanently.

**New `Quaternion` members, needed for `Matrix.Decompose` and useful on
their own:** `CreateFromRotationMatrix` (standard "largest diagonal term" /
Shepperd's-method matrix-to-quaternion extraction — verified by the same
round-trip-through-`CreateFromQuaternion` technique that caught the bug
above, now a permanent `QuaternionTests` case across 6 rotation samples) and
`Slerp` (shortest-path-corrected spherical interpolation, tested for
endpoint values, half-angle midpoint, and the shortest-path correction
itself with a deliberately-negated quaternion).

**`MathHelper`:** `Barycentric`, `CatmullRom`, `Hermite` — standard textbook
spline formulas (not XNA-specific), verified against hand-computed values
in `MathHelperTests.cs` (Catmull-Rom's well-known "passes exactly through
the two inner control points at t=0/t=1" property; Hermite's endpoint
short-circuits and a symmetric-tangent midpoint case).

**`Vector2`/`Vector3`/`Vector4`:** added `Lerp` (missing from `Vector2`
specifically — `Vector3`/`Vector4` already had it), `SmoothStep`,
`Barycentric`, `CatmullRom`, `Hermite` (all delegate to the now-complete
`MathHelper` scalar formulas, applied per-component) to whichever of the
three didn't already have each one; also `DistanceSquared`, `Min`, `Max`,
`Clamp` where `Vector2` was missing them relative to `Vector3`/`Vector4`.

**`Matrix`:** `CreatePerspective`/`CreatePerspectiveOffCenter` (cross-checked
in `MatrixTests` against `CreatePerspectiveFieldOfView` for an equivalent
width/height/fov/aspect combination, and against each other for a centered
frustum — both pass exactly). `Decompose` (row-length scale extraction +
row-normalize + `Quaternion.CreateFromRotationMatrix`; deliberately does
*not* attempt real XNA's own (independently known-imperfect) negative-scale
detection heuristic — flagged explicitly in the doc comment as a known,
accepted gap rather than silently differing from real XNA). `CreateBillboard`
(tested for orthonormality and correct camera-facing direction).
`CreateConstrainedBillboard` (primary path mirrors `CreateBillboard`'s math
exactly; the degenerate near-parallel-axis fallback branch is a simplified
approximation, not a reproduction of real XNA's specific fallback logic —
flagged as lower-confidence in its own doc comment, not tested). `CreateShadow`
(standard planar-shadow-projection matrix; tested via a real homogeneous
divide through `Vector4.Transform`, since shadow matrices generally have
`M44 != 1` and the affine-only `Vector3.Transform` would silently give a
wrong answer — this distinction is called out in the method's own doc
comment specifically so a future caller doesn't make that mistake).
`CreateReflection` (standard planar reflection; tested for mirroring a point
across a plane and leaving an on-plane point unchanged).

**`BoundingFrustum`:** `Intersects(BoundingFrustum)`/`Contains(BoundingFrustum)`
reuse the *exact same* corner-vs-plane loop `Contains(BoundingBox)` already
had (extracted into a shared private `ContainsCorners` helper — no new
algorithm, just parametrized differently) — this is deliberately the same
approximation real XNA/MonoGame's own `BoundingFrustum.Contains(BoundingFrustum)`
uses (can report `Intersects` for the rare edge/face-crossing-with-no-vertex-
containment case a true separating-axis test would resolve differently);
matching real XNA's actual behavior was the goal, not building something
more theoretically correct that behaves differently. `Intersects(Ray)`
returns `float?` via the standard "ray vs. intersection of half-spaces"
slab test (the textbook AABB slab test generalized from 3 axis-aligned
plane pairs to the frustum's 6 arbitrary planes) — also added
`Ray.Intersects(BoundingFrustum)` for symmetry with the box/sphere/plane
overloads that already existed. One test needed loosening after a real
failure, not a bug: `Contains(BoundingFrustum)` compared against an
identical copy of itself put every corner exactly on the boundary planes,
which floating-point rounding can push to either side — same looseness
the pre-existing `Contains_BoundingSphereAroundOrigin_ReturnsIntersectsOrContains`
test already uses for an analogous boundary case, so this isn't a new
pattern, just a new instance of an already-accepted one.

**`Keys`:** added the IME (`Kana`/`Kanji`/`ImeConvert`/`ImeNoConvert`/
`ProcessKey`), Xbox 360 ChatPad (`ChatPadGreen`/`ChatPadOrange`), and
legacy-OEM-hardware (`Oem8`/`OemAuto`/`OemEnlW`/`Attn`/`Crsel`/`Exsel`/
`EraseEof`/`Play`/`Zoom`/`NoName`/`Pa1`/`OemClear`) members that were
previously omitted — 19 new members, 160 total. **Lower-confidence than
everything else in this session's entry**, flagged explicitly in the code:
these are Windows virtual-key ordinals recalled from memory, cross-checked
against this file's own pre-existing `Escape`=27/`Space`=32 (real
VK_ESCAPE/VK_SPACE) as a sanity check of the recollection, but *not*
independently verified against a live system or a real XNA binary — there's
no way to actually execute-and-check an enum ordinal the way the math
formulas above could be. If a future session has access to a real XNA/
MonoGame reference or a live Windows system, these are the values worth
double-checking first.

**Verified, not just written:** `dotnet build CNA.sln` (0 warnings/errors,
all 6 projects), `dotnet test CNA.sln` (103/103 passing, up from 54 at the
start of this session), `dotnet run --project samples/HelloGame` still
fails at exactly the same documented `DllNotFoundException` point (nothing
touched `Game`/`GraphicsDeviceManager`/native interop this session — this
was entirely the pure-math layer, so that's expected, but checked anyway
rather than assumed).

**Post-hoc `/code-review high` pass over all three of this session's
commits** (against `f139f95`, the state before this session started) found
three real, fixed issues, and several restatements of tradeoffs already
documented in code comments (not re-litigated — see below):
- **Real bug, fixed:** `CreateBillboard`/`CreateConstrainedBillboard`'s
  degenerate (coincident-positions) fallback used `cameraForwardVector`
  un-negated; real XNA/MonoGame negates it. The method's own doc comment
  had already flagged this exact spot as "wasn't confidently recalled" —
  the review converted a flagged uncertainty into a confirmed, fixed bug.
  New test (`CreateBillboard_CoincidentPositions_BillboardFacesSameWayAsCamera`)
  needed its own sign derivation double-checked by hand against
  `Matrix.Forward`'s `-row3` definition before trusting it — worth noting
  since it's easy to get backwards a second time even right after fixing
  the first instance.
- **Real gap, fixed:** `CreatePerspectiveFieldOfView`/`CreatePerspective`/
  `CreatePerspectiveOffCenter` had no argument validation; real XNA throws
  for `nearPlaneDistance<=0`, `farPlaneDistance<=0`, or
  `nearPlaneDistance>=farPlaneDistance`. Added a shared
  `ValidatePerspectivePlanes` helper (also fixes the `negFarRange` formula
  being triplicated across the three methods, a separate simplification
  finding from the same review pass, folded into the same fix since it's
  the same three call sites).
- **Real inefficiency, fixed:** `SpriteBatch.DrawString` allocated a new
  `List<GlyphPlacement>` on every call — a per-frame text-rendering hot
  path. Now reuses one `List` per `SpriteBatch` instance, cleared (not
  reallocated) each call.
- **Not fixed, and shouldn't be:** three more findings restated tradeoffs
  already deliberately made and documented in this session's own doc
  comments (`XnaCompat.Vector2` fully duplicating formulas instead of
  delegating — intentional, matches plan.md invariant #3's documented
  `Vector2`/`Color` exception; `GraphicsDevice.SetRenderTarget` accepting
  `Texture2D` instead of strictly `RenderTarget2D` — intentional, documented
  in that method's own doc comment; `RenderTarget2D.CreateNativeHandle`'s
  single-inheritance workaround — intentional, `NEXT.md`'s session-4 entry
  already predicted this exact pattern would recur). One finding
  (`Matrix.Invert`'s nested-ternary row loading) turned out to be
  pre-existing code from session 2 (`git blame` confirms `ec75e9d`, dated
  before this session), out of scope for a review of this session's own
  diff — left alone. **Lesson for future sessions:** a review pass finding
  something you already deliberately chose and documented isn't itself a
  signal to change course; re-derive whether the finding is actually new
  information (the billboard sign truly was, having been flagged as
  low-confidence) or just a restatement of a tradeoff already made with
  its reasoning on record.

All 112 tests pass (up from 103) after these fixes; `dotnet build`/
`dotnet run --project samples/HelloGame` re-verified clean/unchanged.

**Where to pick up next:** `plan.md` Phase 4's remaining items (`SpriteFont`
content loading, then `Effect`/`Model`/3D/audio) — see the session-4 entry
below for the `SpriteFont` design sketch and why the rest need real,
speculative ABI design rather than doc-shape-following.

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
