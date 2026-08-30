# CNA.NET engineering roadmap

Last measured: 2026-08-31, against CNA `next` `71576a7b933c702e1d1384a9720b0237644c2130`
(C ABI 0.20.0) and `sharp-runtimenext` `eebebd862121953538e3b84d43384d70a8a1728d`. Two renderers
this time: OPENGLES3 and HEADLESS. The C API headers are byte-identical to those at `72262a33e`,
where the previous measurement stopped, so the six revisions between them changed nothing this
binding consumes -- checked with `git diff` over `modules/c-api/include`, not assumed from the
unchanged version macro. Session history and
superseded decisions live in [`NEXT.md`](NEXT.md). This file is the current, normative plan.

## Current verified state

**Not release-ready.** The selected seven-assembly XNA 4.0 Windows runtime profile remains
public-metadata complete: the strict facade has the same 257 types and produces zero verifier
diagnostics with an empty allowlist. The binding now targets CNA C ABI 0.20.0 rather than 0.6/0.7/
0.8, and consumes six capabilities the 0.19.0 generation added. Remaining compatibility work is
behavioral, profile breadth, CNA-beyond-XNA surface, native-platform validation, content fixtures,
packaging, and release engineering.

| Area | Measured result |
| --- | --- |
| Debug and Release solution build | 0 warnings, 0 errors |
| Managed tests | 621 `CNA.Framework` + 225 `CNA.XnaCompat`, all passing |
| Native integration | 159/159 passing in Debug and Release on Linux x64 against **both** the ABI 0.20.0 CNA OPENGLES3 library and a HEADLESS build of the same revision. The second renderer is what turns nine absent capabilities from untested branches into exercised ones |
| Native ABI admission | Consumer ABI 0.20.0; the reviewed `cna-cs-native-abi/1` matrix accepts exactly that generation, requires all 910 imports, and runs signature/shape canaries. 11 isolated fixtures: 2 accepted, 9 rejected |
| Upstream ABI diff | `tools/coverage/baselinediff.py` measures 0.8.0 → 0.19.0 as strictly additive over the consumed surface (1,189 exports added, nothing removed or changed), and 0.19.0 → 0.20.0 as 12 renderer-identity constant differences and nothing else |
| Compile probe | Same source builds for CNA and FNA; the MonoGame pure probe builds after recording absent `RendererDetail` dynamically. The future XNA net48/x86 build remains integrated in the Windows snapshot command. Kni still differs at `VertexDeclaration : GraphicsResource` |
| Behavior corpora | One manifest defines 470 observations: 83 Math, 23 Input, 153 Graphics, 13 Resource, 46 Content, 83 Audio, 7 XACT, 20 Media, 17 Video, 20 Storage, and 5 DeviceLifecycle. CNA executes all 470: 199 pure, 166 device, and 105 native-runtime. Windows XNA runtime capture remains pending |
| Windows XNA snapshots | Release-grade validation/build/normalize/manifest/compare workflow implemented; platform-independent manifest/count/compare paths pass locally. Actual Windows XNA execution is not-run/pending |
| Ownership stress | Normal Debug and Release each pass 100/100 cycles, now including the authored DXT3 `SpriteFont` the cycle used to exclude: 1,600 queued owner-thread releases, 3,000 successful release attempts, 0 retries/failures/pending releases, 0 refused game destroys, 0 native crashes. This is not allocator-level leak proof |
| Sanitizers | `not-run`: no exact ABI-compatible ASan/UBSan CNA build was available; no sanitizer-cleanliness inference is made |
| ABI layout evidence | Generated C-authority probe passes on Linux ELF x64: **808 native and 808 managed layout/type measurements with 0 mismatches**, 910 of 910 prototypes compiled, 5 callbacks checked, 327 enum-like constants asserted, and **12 negative controls all rejected** -- including `field-signedness` and `field-wrong-width`, so a struct field's type is measured and not only its offset. Windows PE and macOS Mach-O jobs are wired but actual execution remains pending |
| XNA Windows runtime metadata | 257 reference types, 257 target types, 0 differences, empty allowlist. Run locally against a legally obtained reference set with `XNA_REFERENCE_PATH`; the gate caught three signature regressions during this session and is worth running after every facade change |
| CNA public-type leakage | 0 findings in public/protected strict-profile signatures. For `CNA.Framework`'s own surface the invariant turns out to be **compiler-enforced** -- every `CNA.Interop` type is `internal`, and the one exported type is a static class, so neither can appear in a signature at all. What is guarded instead is that precondition |
| Real-game compile probe | An unmodified 18,391-line Windows Phone XNA game ported to MonoGame compiles against the facade with one unresolved call: `Mouse.SetCursor`, which is MonoGame's addition rather than XNA 4.0. Now offered as `CnaMouse.SetCursor` in the CNA extensions |
| Compiled-content survey | XNA 4.0 sample collection (2,621 assets), `--load`: **2,517 attempted, 2,408 loaded, 0 needing a reader this binding lacks**, 35 failing, 33 refused by the native loader, 41 needing a game's own assembly. Every one of the 35 is classified and none is a binding defect: 29 Xbox 360 compiled effects, 4 XMA2 sounds, 2 models whose referenced effect is absent from that build output. `cna-samples` (574 assets): 555 attempted, **541 loaded, 0 failing**. The delta this session, measured on a fixed 2,047-asset snapshot before the corpus grew: loaded 1,797 -> 1,890, failures 111 -> 38 |
| Template | The checked-in repository project, the generated development project and the isolated package consumer all build. The package-generated project contains no source root, sibling `ProjectReference`, or developer absolute path; native 60/600-frame acceptance passes against 0.20.0 on **both** OPENGLES3 and HEADLESS |
| Other engines | Source builds pass for FNA, MonoGame, and Kni; 60-frame MonoGame and Kni runs pass; FNA runs 600 frames over Vulkan once `FNA.Core` is built and `FNA_FRAMEWORK_PATH` points at it (see E3) |
| Packages | None published. Shipping defaults remain `IsPackable=false`; the isolated acceptance path creates local `CNA.Interop`, `CNA.Framework`, and `CNA.XnaCompat` preview packages, including an experimental `linux-x64` native asset, and passes inspection/install/build/60/600-frame/error-diagnostic checks |
| Tested platform | Linux x64 only in this run |

The metadata result is produced by `tools/api-compat`, not by the legacy name counter. The current
hard invariants are:

```text
TOTAL_DIAGNOSTICS=0                   ALLOWLIST_ENTRIES=0
CNA_TYPE_LEAK=0                       BASE_TYPE_MISMATCH=0
INTERFACE_MISMATCH=0                  MISSING_TYPE=0
MISSING_MEMBER=0                      UNEXPECTED_TYPE=0
UNEXPECTED_MEMBER=0                   PARAMETER_NAME_MISMATCH=0
```

No difference is allowlisted. Both the normal strict verifier and the standalone leak gate exit 0.

## Which CNA this binds

`cnanext` (the `next` branch of `openeggbert/cna`) is the development line this binding targets, and
`sharp-runtimenext` is the `next` branch of the C++ `System.*` implementation it builds on. The
older `cna`/`sharp-runtime` `develop` checkouts sit at C ABI 0.7.0 and are not admitted; see
[Retired entries](docs/native-abi-compatibility.md#retired-entries) for why that is a consequence of
consuming new routes rather than a judgement about those generations.

### Renderers this binding must not assume

`sokol`, `diligent`, `llgl`, `igl`, `wicked`, `magnum`, `skia`, `blend2d`, `nanovg`, `openvg` and
`tinygl` are being removed upstream. That merged into `cnanext` during this work and is what moved
the ABI to 0.20.0: `tools/coverage/baselinediff.py` reports exactly eleven removed
`CNA_GRAPHICS_RENDERER_*` constants and `CNA_GRAPHICS_RENDERER_MAXIMUM` moving from 50 to 49, with
no export, prototype, struct or scalar change anywhere.

Nothing in this repository or in `cna-cs-template` may name them, gate on them, or claim support for
them. Three consequences, two applied and one structural:

- render-target `ContentLost` documentation says "a renderer family that can genuinely lose a
  device" rather than listing `DIRECTX9`/`DIRECT2D`/`SKIA`, because that list has shrunk;
- the template asks the renderer for its name and capabilities instead of enumerating renderers, so
  it needs no change when the set changes;
- this binding reads the renderer's *name* and never a `CNA_GRAPHICS_RENDERER_*` identity, which is
  the reason a removal of eleven renderers is a clean diff over the consumed surface rather than a
  breaking change. Keep it that way: binding the identity enum would make every future renderer
  change a compatibility event.

## Compatibility definition

Priority is XNA 4.0 source compatibility, exact public metadata, then observable behavior. Binary
compatibility with Microsoft's strong-named assemblies is not a release goal. CNA, FNA, and
MonoGame extensions must be outside the strict `Microsoft.Xna.Framework` contract or explicitly
reviewed and allowlisted.

The current measured profile is `tools/api-compat/profiles/xna40-windows-runtime.json`, aggregating:

- `Microsoft.Xna.Framework.dll`
- `Microsoft.Xna.Framework.Game.dll`
- `Microsoft.Xna.Framework.Graphics.dll`
- `Microsoft.Xna.Framework.Storage.dll`
- `Microsoft.Xna.Framework.Video.dll`
- `Microsoft.Xna.Framework.Input.Touch.dll`
- `Microsoft.Xna.Framework.Xact.dll`

The native CNA headers define available backend capabilities. They never define the managed XNA
contract.

## Definition of done for the declared Windows runtime profile

- Metadata comparison: 0 missing, mismatched, or unexpected items with the allowlist empty.
- Public/protected CNA-type leak check: 0 accidental findings.
- Every XNA inheritance/interface/generic assignment in the compile corpus builds unchanged.
- Managed differential tests cover high-risk value, collection, lifecycle, content, graphics,
  input, audio, media, and disposal behavior.
- Custom `ContentTypeReader<T>` and `Content.Load<MyType>()` work through normal XNA APIs.
- C declarations, symbols, layouts, ownership, and callback lifetimes are checked against the
  selected CNA ABI on each supported OS.
- The template and a freshly generated `dotnet new cna-game` project build and complete both short
  and stability native runs.
- NuGet/RID installation is reproducible on every claimed OS/architecture.

## Open work, in priority order

These are the tasks this plan is asking for next. Everything here is either measured as missing or
measured as failing; nothing is aspirational.

### A. Consume the rest of what CNA already provides

Wired since the move to 0.19.0/0.20.0: multi-listener `Apply3D`, raw optioned vertex uploads,
render-target `ContentLost`, the caller-owned `GraphicsDevice` constructor, compressed atlas
loading, the engine-layer availability pair with its five graphics capabilities, and the mouse
cursor surface. The routes below exist upstream and are still unbound.

| Task | Route | Completion criterion |
| --- | --- | --- |
| A1. **Done.** `SpriteBatch.DrawString` through the native text route | `cna_sprite_batch_draw_string` | Adopted. Identical glyph placement (0 of 1024 pixels differ) and about 40% less time per text-heavy frame. Falls back to glyph quads if a renderer refuses the route. See A1c for the measurement and for the reason the recorded rationale was backwards. |
| A2. **Already done, under a different route.** Batched sprite submission | `cna_sprite_batch_submit_scaled_many` | The completion criterion -- one native call per batch, not one per sprite -- has been met since the buffered-flush change. The route named here originally was the wrong one for this binding: `submit_many` is destination-rectangle-based, and CNA's position+scale route is the one this facade needs, because `Draw(texture, Rectangle, ...)` is converted managed-side to `position = rect.XY`, `scale = rect.Size / source.Size`. What was genuinely missing is any check that the conversion lands where the rectangle asked; see the pixel tests below. |
| A3. **Done.** `PresentationParameters` bounds and clone | `cna_presentation_parameters_get_bounds`, `cna_presentation_parameters_clone` | Both go through native. Native agrees with the managed reconstruction that was there (the back buffer at the origin), which is the expected outcome and not the point -- the point is that native is the authority, so a future disagreement shows up here instead of being silently overridden. The clone is asserted independent, since a route returning the same value would pass every equality check and fail the moment a game edited the copy. |
| A4. **Done.** Preferred presentation mode | `cna_graphics_device_manager_get/set_preferred_presentation_mode_ext` | `CnaGraphicsDeviceManagerExtensions.Get/SetCnaPreferredPresentationMode`, outside the strict namespace. All five identities round-trip, not one: the enum crosses the ABI as a numeric cast, and an off-by-one there passes a single-value test. Worth having because XNA stretches the back buffer and offers no say, so a fixed-aspect XNA game letterboxes by hand -- code a port can now delete. |
| A5. **Done.** Explicit content-lost notification | `cna_graphics_device_notify_content_lost_resources_ext` | `GraphicsDevice.NotifyContentLostResourcesForTesting`, named so it cannot be mistaken for a game route. It closes a real hole: the existing test could only show the subscription was *taken*, because these renderers never lose a device, so the handler side had never run once -- and between the managed subscription and a game's callback sit an event bridge, a sender projection and a native registration. The event now fires, delivers to the surviving handler only, and carries the render target as sender. |

### A6. Content, now measured rather than guessed

`tools/content-survey` answers "how much of a real game's compiled content can this read" against
any `Content` folder. Against the XNA 4.0 sample collection there are no missing built-in readers
left. What that does *not* say is that the bytes after each reader table are read correctly, which
is the honest limit of a resolution survey. Remaining work:

- A6a. **Done, across every corpus on this machine, and it paid for itself twice.** 2,879 assets
  surveyed with **0 missing built-in readers and 0 malformed**:

  | corpus | assets | distinct readers |
  | --- | --- | --- |
  | XNA sample collection (`/rv/tmp/samples`) | 1,914 | 88 |
  | `cna-samples` | 532 | 80 |
  | `mobile-eggbert-legacy` (Speedy Blupi) | 420 | 2 |
  | `cna`'s own MonoGame test assets | 13 | 18 |

  The first corpus survey found A6b's premise false. This wider sweep found a second defect, and a
  worse one: **the managed loader accepted three platform bytes where the native loader accepts
  sixteen.** One binding, two answers for one file -- an asset MonoGame built for DesktopGL
  (`'d'`) loaded natively and was refused managed-side. Six assets in CNA's own test corpus, and
  five more in `cna-samples`, were unreadable through the managed path for that reason alone. The
  list now matches `XnbHeader.hpp:XnbAcceptedPlatforms()` exactly, which takes it verbatim from
  FNA's `targetPlatformIdentifiers`, and a theory asserts every byte individually so adding one to
  either side without the other fails and names the byte.

  One corpus stays unreadable and that is the correct answer: 140 assets under `_webs` are Speedy
  Blupi's web builds, stamped `'b'`, which is KNI's BlazorGL identifier and which **neither FNA nor
  CNA accepts**. Matching CNA is a fix; going past CNA would be a decision about a format nothing
  else in this binding supports, so the refusal is asserted rather than left untested.
- A6b. **Done.** `EffectMaterialReader` is built and registered. The note here used to say "no asset
  in the surveyed corpus reaches it" -- eleven assets in `cna-samples` name it, so the reader was
  missing under assets that really exist. It is transcribed from the decompiled reader, including
  the rule that a parameter the effect does not have is skipped rather than reported.

  One deliberate deviation, in `ModelContentReaders.cs`: XNA sets each value directly and catches
  `InvalidCastException` to retry through a widened `Vector4`. That control flow cannot be
  reproduced faithfully, because a shape mismatch here raises a `CnaException` from native rather
  than XNA's own `InvalidCastException`, and catching that broadly would swallow real failures. The
  widening path is taken up front instead -- for these types it is not a fallback at all, since it
  reduces to the identity when the shapes already agree, so the two routes agree wherever XNA would
  not have thrown.

  What is *not* proven is that a real material's parameters land correctly; that needs a
  pipeline-built effect this repository cannot produce. The test proves the reader is selected and
  runs, and it carries a control assertion, which earned its place immediately: the phrase first
  asserted on appeared nowhere in the path, so the test had been passing for any exception at all.
- A6c. **Done.** `tools/content-survey --load` builds a real game, graphics device and
  `ContentManager` and calls `Load`, so the report says what was *materialised* rather than what
  resolved. Against `cna-samples`: **529 attempted, 497 loaded, 24 of 26 compressed assets loaded**,
  5 refused by the native loader, 27 failing.

  It found two real defects immediately, both invisible to the resolution survey:

  - `XnbModelReader` required a non-null string for bone and mesh names. XNA content permits an
    unnamed bone -- a reference element with type index zero is null -- and twenty models in the XNA
    sample collection have them. `ModelBone` and `ModelMesh` refused them on both layers. Storing an
    empty string instead would have been worse than failing: it invents a name the file does not
    contain, and a game comparing bone names would then match the wrong bone. **19 more assets load.**

  Across the XNA sample collection (`/rv/tmp/samples`), the wider corpus: **1,936 attempted, 1,772
  loaded (91.5%), 48 of 52 compressed assets loaded**, 110 failing, 34 needing a game's own assembly,
  20 refused by the native loader.

  The remaining failures are recorded rather than fixed here:

  - **20 need readers `CNA.Framework`'s XNB reader does not have** (11 `EffectMaterialReader`,
    5 `DictionaryReader`, 2 `EnvironmentMapEffectReader`, 1 `DualTextureEffectReader`, 1 other).
    There are two managed XNB readers here -- `CNA.Framework/Content/Xnb` knows nine, and
    `CNA.XnaCompat`'s knows the full built-in set -- and model loading goes through the weaker one,
    so an `EffectMaterial` inside a model fails even though A6b built a reader for it.

    **Routing models through the compat chain was tried and measured worse**: loaded fell 497 -> 436
    and failures rose 27 -> 78, so the compat `ModelContentReader` is not ready to replace the
    framework path. Recorded so nobody repeats it.

    Adding `EffectMaterialReader` to the framework path is *not* the fix on its own either. An
    `EffectMaterial` names its effect through an external reference, and that path resolves no
    external references -- `BasicEffect`'s texture reference is left unresolved for the same reason.
    A model would then load with null part effects and fail when drawn, which is worse than failing
    to load. **The prerequisite is external-reference resolution in `CNA.Framework`'s XNB path**, and
    that is the real next content task.
  - 1 asset passes an absolute path to `TitleContainer.OpenStream`, which is a survey-harness bug
    rather than a binding one.
  - **5 are an upstream limit**, not a defect here: CNA's C content loader answers
    `NotSupported` with "The initial C content loader supports only Color TextureND assets" for
    normal maps and other non-`Color` surface formats.

### A1b. The reason `SpriteFont` stayed managed was not true

`SpriteFont`'s doc comment said the native SpriteFont resource "exposes no per-glyph readback -- no
bounds, no cropping, no kerning", and concluded that a native-owned font "could be measured and never
drawn". All three are returned by `cna_sprite_font_copy_glyphs`, whose own header says it exists
precisely because measuring is not drawing -- and `ContentManager.LoadSpriteFontData` has been
calling it in this repository the whole time. It loads a native font, copies the glyph table out, and
destroys the font. The stated blocker was contradicted by the binding's own code, which is the third
premise in this plan found stale by checking rather than reasoning (after A6b and A2).

The true statement is narrower and is now in the comment: nothing *retains* a native font, so there
is no handle to give `cna_sprite_batch_draw_string`. The load path destroys it after copying, and the
public constructor -- which XNA has, for third-party font tools -- never makes one at all.

Adopting A1 therefore means:

- giving `SpriteFont` a native handle and a lifetime, retained from the load path instead of
  destroyed, and built with `cna_sprite_font_create` for the public-constructor case
- keeping the managed glyph table regardless, because `MeasureString` is pure managed code today and
  is real, tested, and free of a native round trip
- then measuring, which is the part that was always the point: render the same string both ways into
  a `RenderTarget2D` and compare pixels. `SpritePixelTests` now makes that a mechanical exercise --
  it did not exist when A1 was written, which is why A1 said "measure" without saying how.

Adopt only if glyph placement is not observably different, and record the measurement either way.
The win if it holds is one native call per string instead of one per glyph, which is worth having in
a text-heavy game and is worth nothing if the glyphs move.

### A1c. The measurement: native `draw_string` places glyphs identically

`NativeDrawStringMeasurementTests` renders the same string through both routes into a
`RenderTarget2D` and compares the result pixel for pixel. **0 of 1024 pixels differ.**

The font is synthetic on purpose -- one texel per glyph, distinct colours, scale 4 -- so each glyph
is a solid 4x4 block and any disagreement in placement, ordering or tint appears as a block in the
wrong place rather than as a few arguable edge texels. The reference render is asserted to contain
the red `A` block at 8,8 and the green `B` block immediately after it *before* the comparison runs,
because two blank targets agree perfectly. That guard is not hypothetical: the first version of this
test checked for emptiness by looking for a zero alpha, and a target cleared to opaque black has
alpha 255, so it would have compared two blank images and passed. Third time in this session that a
check of mine could not fail; the guard is now written as a positive assertion about what must be
drawn rather than a negative one about what must not.

**The recorded rationale was backwards.** This plan said the win was "one native call per string
instead of one per glyph". `DrawEx` settles it: every `Draw`, and every glyph of every `DrawString`,
appends to `_commandBuffer`, and the whole batch leaves through a single
`cna_sprite_batch_submit_scaled_many` at `End`. The old cost was one transition *per batch* whatever
the glyph count, so the native route makes strictly **more** ABI crossings, not fewer.

It is still worth adopting, for the opposite reason. Measured on a text-heavy frame -- 64 strings of
24 glyphs, 60 frames -- the buffered per-glyph path costs about 2.1 ms/frame with one native call,
and the native route about 1.25 ms/frame with sixty-four: **ratio 0.60**, stable across Debug and
Release and across repeat runs. Computing and buffering a quad per glyph costs more than the extra
crossings. That is a fourth premise in this plan found wrong by checking rather than reasoning -- and
this one was written in this plan a few hours earlier.

Adopted as follows:

- `SpriteFont` creates its native counterpart lazily and keeps it in a `NativeResourceHandle`. Lazy
  because the public constructor is XNA's and is pure managed code today, as is `MeasureString`;
  creating a font eagerly would make constructing one require a live game. Rebuilt from the glyph
  table rather than retained from the load path, because `ContentManager` destroys the font it loads
  as soon as it has copied the table out, deliberately and unconditionally, and rebuilding is
  lossless -- `copy_glyphs` is documented as the exact inverse of `create`.
- `FlushCommandBuffer` replays sprites and strings **interleaved**, one `submit_scaled_many` per
  contiguous sprite run with the strings between them. Submitting all sprites then all strings is one
  call fewer and draws a HUD underneath the scene it labels; `SpriteTextOrderingTests` asserts both
  directions, because a batch that always drew text last would pass a one-directional test.
- A renderer that refuses the route falls back to glyph quads, expanded in that string's place so
  ordering still holds. Every renderer could draw text before this change and a refusal must cost
  speed rather than the string. `ForceGlyphQuadTextForTesting` exists because on a renderer that
  accepts the route the fallback would otherwise never run -- the same hole
  `NotifyContentLostResourcesForTesting` closes for `ContentLost` -- and the fallback is asserted to
  draw pixel-for-pixel what the native route draws.

Adoption also made the original A1 comparison vacuous, since `DrawString` had become the native
route and both arms were then measuring the same code. Both the placement and cost tests now force
the glyph-quad route on the reference arm.

### A2b. Sprite drawing is now checked by reading the pixels back

Every drawing test in the suite asserted that a draw did not throw. That catches a broken ABI
transition and nothing else: a sprite drawn in the wrong place, at the wrong size, or not at all
returns success just as happily -- and "the game renders wrongly" is the most common way a port fails
with every gate green.

`tests/CNA.Integration.Tests/SpritePixelTests.cs` draws into a `RenderTarget2D` and reads it back.
Two things are now measured rather than assumed:

- a destination rectangle covers exactly that rectangle, pixel for pixel
- the origin is in source-texture pixels and **scales with the sprite** -- an origin of one pixel on
  a sprite scaled eight times shifts it eight pixels. Measured: destination `(16,16,8,8)` with origin
  `(1,1)` lights exactly `8,8..15,15`. A binding that passed the origin through unscaled would be
  wrong by a factor of the scale, and invisible at scale 1, which is where a casual test looks.

Both tests report and return early if the renderer reads back an empty target, rather than asserting
against nothing. Neither took that path here.

The obvious extension is A1: glyph placement is exactly the kind of thing this can now measure.

### A7. Dynamic skip does not work in the integration suite -- fixed, and it was bigger than recorded

The premise was right, and measured rather than assumed this time: a probe test throwing
`SkipException.ForSkip` is reported `[FAIL]` by `xunit.runner.visualstudio` 2.8.2, with the raw
marker `$XunitDynamicSkip$` as its message.

Two things this entry had wrong. It said three tests were affected; there were **twenty-two** --
twenty calls to `CnaNativeProbe.RequireCapability` plus two inline throws. And
`NativeFactRequiringAttribute`, which reads as though it gates on the capability it is given,
**ignores the argument entirely** (`_ = capability;`): it only carries the "no native library" skip,
and the real gate was always the runtime throw. So twenty-two tests would each have reported a defect
the first time they met a renderer they were written to tolerate, and "this renderer is 2D-only by
design" would have read as "the binding is broken".

`RequireCapability` is now `HasCapability`, which returns `false` and prints `NOT EXERCISED: ...`
with the renderer name and the missing capability; call sites return early. `CapabilityGate_Agrees-
WithTheDeviceForEveryCapability` asserts the gate agrees with the device for **every** capability
rather than a sample, because a forwarding call breaks on one enum value and not on all of them, and
it prints which capabilities the live renderer lacks. On `OPENGLES3` that count is zero, which is why
none of this had ever fired.

**The honest cost was a silent pass**, and the remaining half of this item was to replace it with an
assertion about what must happen when the capability is *absent*. **Done as far as this host allows,
and the enabling step was a second renderer rather than more test code.**

A HEADLESS build of the same cnanext revision reports nine of nineteen capabilities absent, has no
engine layer, refuses render-target readback and refuses cube-face storage. Run against it before
any change: **139 passed, 6 failed** -- five pixel-evidence tests and one cube test failing with
`NotSupported` as though the binding were broken, which is precisely the confusion this item exists
to remove. After: **146/146 on HEADLESS and 146/146 on OPENGLES3**, with four branches now genuinely
executing and asserting rather than returning:

| absent | asserted refusal |
| --- | --- |
| `Texture3D` | constructing a `Texture3D` answers `NotSupported` |
| cube-face storage | `TextureCube.SetData` answers `NotSupported` |
| render-target readback | `RenderTarget2D.GetData` answers `NotSupported` |
| the engine layer | constructing a `RenderTargetPool` answers `NotSupported`, and the version is 0 |

The last is the branch D3 was designed around and could not reach: every engine-layer symbol
resolves in every build, so a build *without* the layer is the only thing that can prove the
availability query is load-bearing.

**Two of those four have no capability identity to ask for**, which is now a blocker row.
`CNA_GRAPHICS_CAPABILITY_*` names nineteen things and neither readback nor cube-face storage is one
-- HEADLESS reports `ThreeD` and refuses both. They are measured once per renderer by a probe whose
whole purpose is that one fact, and the answer selects which assertion the test makes. No test
catches.

**The thirteen `ThreeD` and four `CustomEffects` sites remain unasserted, deliberately.** No renderer
available on this host lacks either, and the refusal a 2D-only renderer produces is *not* knowable
from the headers: `IGraphicsRenderer::HandleUnsupported3DCall` throws a bare `std::runtime_error`,
which the C API's exception barrier maps to `CNA_RESULT_INTERNAL`, while a renderer whose own
`Ensure3DSupported` throws `System::NotSupportedException` maps to `NOT_SUPPORTED`. Writing the more
plausible of those two guesses is what I would have done before reading the barrier, and it would
have been wrong. The reason is at each call site, and the upstream classification is a blocker row.

Remaining `HasCapability(...) { return; }` sites: **18** (14 `ThreeD`, 4 `CustomEffects`), all
blocked on a renderer this host cannot supply, and **every one now states that at the call site**.
A silent pass with a reason beside it is a recorded gap; a silent pass without one is indistinguishable
from an oversight, and there were seventeen of those.

The precondition sweep this entry also asked for is done and found nothing left. Every assertion
about device state in the suite now establishes that state first: the render-target test unbinds
before asserting empty, the texture-slot test assigns null before asserting null, and the two
counter assertions are on freshly constructed games. `GetVertexBuffers` -- the one that started this
-- was already fixed and carries a comment explaining why.

### B. Deepen the ABI evidence

The C-authority probe measured 13 of the 80 interop structs and compiled 4 of 881 prototypes. That
was defensible against a one-minor step; against a twelve-minor one it is a floor, and it is now the
weakest link in an admission that otherwise rests on the upstream baseline diff.

- B1. **Done.** The layout probe is no longer hand-written: `CNA.AbiVerify` generates the C from the
  structs `CNA.Interop` declares, and builds the managed side from the same enumeration. The two
  cannot drift, because they come from one source, and B1's own criterion is now automatic -- a
  managed struct with no native counterpart is a **compile error in the generated probe**, not a
  struct nobody measured.

  **14 structs measured before, 84 now; 808 values compared on each side, 0 mismatches.**

  Deriving the names is what makes it cover everything: `CnaFoo` is `CNA_Foo` and `StructSize` is
  `struct_size`. A list would need extending by hand for each new struct, which is exactly how the
  old probe came to measure fourteen. Two rules had to be sharpened by the compiler rejecting them:
  the separator also belongs before the last capital of a run (`HasYButton` is `has_y_button`, not
  `has_ybutton`), and padding that C writes as one array is a run of separate bytes here, measured at
  its first byte.

  What derivation cannot bridge is listed explicitly, and every entry is a place where the two sides
  genuinely chose different words -- CNA says `pressed_buttons` where the managed struct says
  `Buttons`, `scroll_wheel` where it says `ScrollWheelValue`, `format` where it says `ColorFormat`.
  An override is a statement that someone read the header, and a wrong one fails to compile rather
  than silently measuring the wrong field. Four managed-only types are excluded with reasons
  (`CnaFloatBuffer256` and `CnaNativeAbiProfile` are not ABI types; `CnaReservedBytes3`/`7` are how
  this binding spells inline padding), and `CnaHandle` is measured for size and alignment only,
  because `CNA_Handle` is a `uint64_t` typedef and `offsetof` on a typedef does not compile.

  `CnaRect` and `CnaRectangle` are two managed spellings of one C type. Both are measured, and a
  disagreement between them raises rather than letting the second dictionary write win -- which is
  the case actually worth catching.
- B2. **Done.** The prototype probe is generated from `CNA.Interop.Native` by reflection, so
  **PROTO_IMPORTS = PROTO_VERIFIED = 881** and `PROTO_UNMAPPABLE = 0`. Each import becomes a
  file-scope function pointer declared with the prototype derived from the *managed* declaration and
  initialised with the real C function; C's compatibility rules then make a wrong return type, arity,
  parameter type, by-ref direction or pointer depth a diagnostic, and `-Werror` makes a diagnostic a
  failed gate. The C compiler is the authority on what the header means -- nothing here reimplements
  C's type rules.

  **It found four real defects on the first run**, all now fixed:
  `cna_game_components_insert` was declared with a signed 32-bit index against a `uint64_t`
  parameter, and the three `draw_*_primitives` routes passed `int` for a `CNA_PrimitiveType`
  (`uint32_t`). The index one is the serious one: only x86-64's zero-extending 32-bit moves made it
  work. A fifth followed from it -- `CnaUserPrimitives.PrimitiveType` was `int` against a `uint32_t`
  field, which B1's offset probe cannot see because the widths agree. **That follow-up is done**, and
  this sentence used to say it was not. The layout probe emits, for every field of every measured
  struct, a pointer initialised from that field's address with the type derived from the *managed*
  declaration -- `uint32_t* const pf_CNA_SpriteScaledCommand_effects = &s_CNA_SpriteScaledCommand.effects;`
  -- so C's own type rules reject a difference in signedness or width that the offsets agree about.
  Two of the twelve negative controls are exactly those two mutations, and both are rejected.

  110 parameters are listed in an explicit override manifest, in four categories, none of which is an
  ABI difference: `const T*` (C# cannot qualify a pointer, and `const` is not part of a calling
  convention), `char*` (C's `char` is a third type distinct from both signed and unsigned char, so no
  C# pointer is exact), `void*` against `byte*`, and 24 callbacks the managed side declares `nint`,
  which carries no signature at all. **Every entry was produced by the compiler, not by hand** -- the
  generator emitted the managed-derived prototype, the compiler rejected it, and the recorded type is
  the one the diagnostic named. So the manifest cannot excuse a difference that was not measured, and
  any *other* change to the same parameter still fails.

  Twelve negative controls run as part of the gate, and all twelve are rejected: wrong return type,
  signedness change at the same width, wrong pointer depth, `in`->`out`, `out`->`in`, wrong versioned
  descriptor, wrong callback shape, swapped same-width parameters, absent import. A control whose
  target declaration is no longer generated reports itself stale rather than passing silently -- which
  it did, for all nine, the first time they were written from memory instead of from the generated
  text. The gate was also proven to *fail*: making one mutation identical to its original produces
  `ABI_STATUS=failed`.

  Two tool defects were fixed on the way. `Run` read one pipe to the end and then the other, which
  deadlocks the moment a child fills the pipe nobody is draining -- a C compiler with a hundred
  diagnostics does exactly that, and the first full run hung silently until it was killed. And the
  self-test originally ran *after* the report was built, so its findings could not affect
  `ABI_STATUS`.

  **The callbacks are now covered too.** The prototype probe checks the routes; for a callback the
  managed side declares `nint` it can only prove that a pointer is passed, and what actually crosses
  at run time is a managed function nobody had compared with the C typedef.
  `InteropCallbacks` declares a C function with the signature derived from the *managed* member --
  an `UnmanagedCallersOnly` method, or a function-pointer field of an interop struct -- and assigns
  it to CNA's typedef. `CALLBACKS_CHECKED=5`, `CALLBACKS_UNRESOLVED=0`.

  **Two more real defects, both found this way:**
  `NativeEventBridge.OnNativeEventWithSender` took the sender as `nint` where CNA passes a
  `CNA_Handle` (`uint64_t`). On a 64-bit target the two coincide, which is why nothing showed; on a
  32-bit one the sender is four bytes short and every argument after it is read from the wrong
  place. And `CnaContentTypeReaderCallbacks.Destroy` returned a result code where
  `CNA_ContentTypeReaderDestroyCallback` returns `void`.

  Both were confirmed to be *caught* by reintroducing them: the sender defect produces
  `initialization of CNA_GraphicsResourceDisposingCallback ... from incompatible pointer type
  void (*)(void*, void*)`, and the destroy defect no longer even compiles in C#, because the field
  and the method have to agree.

  The pairing of managed callback to C typedef is listed rather than derived: a function pointer is
  handed to a route at a call site, not declared as implementing anything, so metadata cannot supply
  it. The list is five entries and each says the call site was read.
- B2 (was). Extend the prototype probe from 4 routes to every callback-taking route and every route whose
  managed declaration uses `in`/`out`/`ref` on a versioned descriptor -- the shapes `sweep.py`'s
  arity check cannot see.
- B3. **Done.** `CONSTANTS_CHECKED=286`, 0 mismatches. Every enum-like identity this binding
  consumes is a `_Static_assert` against the macro of the same identity, so the preprocessor supplies
  the value and the managed literal has to agree. These are the values no other check constrains: a
  renumbered constant passes every layout and prototype check, compiles cleanly, and goes wrong at
  run time.

  Coverage is CNA.Interop's 20 `Cna*` enums plus **30 CNA.Framework enums whose values cross to
  native as plain integers** -- a framework enum cast to `uint` at a call site is as much an ABI value
  as one declared in the interop assembly. The 30 were chosen by measurement: their derived prefix
  exists among the 883 macro groups the headers define.

  The other 24 framework enums are listed with reasons rather than dropped:
  `ContainmentType` and `PlaneIntersectionType` are results of managed geometry and never cross;
  `ClearOptions` and `TouchLocationState` do cross but CNA spells the group differently and they are
  already checked through their CNA.Interop twin; `Keys` is a 100-plus identity set CNA names per key
  rather than as a group. `CnaVertexType` and `CnaUserVertexSource` are this binding's own vocabulary,
  which no header declares.

  The macro name is derived (`CnaGraphicsProfile.HiDef` is `CNA_GRAPHICS_PROFILE_HI_DEF`), with three
  prefix and twelve member exceptions -- every one found by the compiler refusing the derived spelling
  rather than by reading the header hopefully. One rule differs from the struct-field spelling: a
  capital after a digit takes no separator, because C writes `TEXTURE1D` and `VECTOR2`.

- B3 (was). Emit every consumed enum-like constant from the C probe and compare it against the managed
  enum member, so a renumbered identity fails a gate instead of a game.
- B4. Execute the Windows PE and macOS Mach-O `portable-abi-header` jobs. They are wired and have
  never run.

### C. Close or re-adjudicate the remaining native blockers

[`docs/native-behavior-blockers.md`](docs/native-behavior-blockers.md) is the list. Six rows closed
when the binding moved to 0.19.0/0.20.0. The ones that need managed work rather than upstream work:

- C1. Done: both caller-owned `GraphicsDevice` constructors now warn at the call site that creating
  a device while a game is running takes the GL context and kills that game's next present. The
  underlying fix is upstream's -- save and restore the current context around device creation.
- C2. **Done, and it found a false row.** The table is measured against `17b5a90a0` (C ABI 0.20.0),
  whose C API headers are byte-identical to those at `72262a33e` where the previous pass stopped, so
  the three intervening revisions needed no remeasurement. `Verify-BlockerTable.sh`: 15 routes named,
  15 present.

  The row claiming the buffer families never got a `ContentLost` route was **wrong on both of its
  clauses** -- `cna_vertex_buffer_subscribe_content_lost` and its index twin have existed since
  2026-08-15, `CNA.Interop` consumes them, and `DynamicVertexBuffer.cs` says so in its own doc
  comment. It survived because the mechanical check verifies that a *named* route exists and cannot
  catch a row that is wrong about a route being there. The residual question -- whether the
  subscription is delivered to -- is now measured and the row is closed.

  Three rows were added, all found by running against a HEADLESS build rather than by reading:
  render-target CPU readback and cube-face storage have **no capability identity** to ask for, and a
  2D-only renderer's refusal arrives as `CNA_RESULT_INTERNAL` rather than `NOT_SUPPORTED` because
  `HandleUnsupported3DCall` throws a bare `std::runtime_error`.

  The non-`Color` texture row's counts rose from 20/5 to 26/7 for a good reason: models that used to
  fail before reaching their textures now reach them.

### D. CNA API beyond XNA 4.0

**D0 and the cursor surface are done.** The engine layer's availability and revision are bound
(`cna_graphics_ext_is_available`, `cna_engine_layer_get_version`) and exposed as
`CnaGraphicsDeviceExtensions.IsCnaEngineLayerAvailable()` / `CnaEngineLayerVersion()`, and the five
graphics capabilities CNA 0.8 added -- float and half-float render targets, half-float linear
filtering, compute shaders, indirect draw -- are reachable from managed code for the first time;
`CnaGraphicsCapability` had stopped at 13 through three ABI admissions. On the measured OPENGLES3
build the engine layer reports available, revision 2, with all five capabilities true.

`CnaMouse`/`CnaMouseCursor` are the second piece, and they arrived from the opposite direction:
the real-game compile probe found that a MonoGame-derived game's one unresolvable call was
`Mouse.SetCursor`, CNA has the whole cursor surface behind it, and the strict facade cannot carry
a member XNA lacks. That is the shape every future item here should take -- a measured need, a
route that exists, and a home outside the strict contract.

The ordering rule matters for everything else in this section: the whole engine-layer surface is
exported by every CNA build and returns `NOT_SUPPORTED` when the layer is absent, so a resolved
symbol is not evidence of a capability. Any further engine-layer binding must gate on the
availability query rather than on the symbol existing.

CNA 0.20.0 exports 4,051 routes; this binding consumes 910. The remainder is not all product
surface -- most of `vectors.h`, `matrix.h`, `math.h`, `quaternion.h`, `curve.h`, `geometry.h` and
`color.h` is deliberately managed by design invariant 3, and much of the rest is engine-internal.
What is genuinely a CNA-beyond-XNA product surface, by header and unbound count:

| Header | Unbound | What it is | Placement |
| --- | --- | --- | --- |
| `engine_layer.h` | 836 | CNAEXT: storage buffers, compute shaders, GPU timers, render-target pools, shader-effect caches, full-screen/post-process passes, PBR material binding, clustered lighting, shadows, SSAO/SSR/bloom/tonemap, particles, decals, LOD | `CNA.Framework.Extensions` + `CNA.XnaCompat.Extensions` |
| `cnb.h` | 244 | CNA's own binary content format: encode/decode for textures, models, video, documents, plus the tooling front ends | `CNA.Content.Cnb` |
| `gamer_services.h` | 204 | Gamers, identities, achievements, leaderboards, avatars, guide | separate future profile; inventory-only today |
| `models.h` | 161 | Model/mesh/bone/morph-target surface beyond the XNA subset already bound | `CNA.Framework.Extensions` |
| `sensors.h` | 109 | Accelerometer/compass/inclinometer/motion | separate future profile |
| `net_sessions.h`, `net.h`, `net_gamers.h` | 165 | Networking and session APIs | separate future profile |
| `effects.h` | 75 | Effect surface beyond the XNA stock effects | `CNA.Framework.Extensions` |
| `input_haptics.h`, `input_joystick.h`, `input_devices.h` | 87 | Haptics, joysticks, device hotplug | `CNA.Framework.Extensions` |

Rules for all of it, unchanged from the existing extension policy: nothing here may appear in the
strict `Microsoft.Xna.Framework` contract, the leak gate must stay at zero, and each addition
records authority, source-portability value, implementation status, and namespace. Order of work:

- D1. Done; see D0 above.
- D2. **First vertical slice done.** `CNA.Content.Cnb.CnbDocument` opens a `.cnb`, reports its
  container version, asset type identity and schema version, enumerates its table of contents and
  copies a chunk's decompressed bytes. Outside `Microsoft.Xna.Framework` deliberately: XNA has one
  content container and this is a second, so routing it through `ContentManager.Load<T>` would change
  a contract checked member for member against XNA's metadata.

  `cnb.h` is 272 routes. Fourteen are bound. Projecting the rest -- encoders, model builders, sprite
  tooling -- would be a worse API than none; what a game needs first is to open a container, find out
  what it holds and reach its bytes.

  **Ownership**: the document handle is owned and destroyed by the facade. A `CnbChunk` is a copied
  snapshot rather than a view into the document, and chunk data is copied into a caller array, so
  nothing can outlive the memory it describes -- a span over native bytes would keep looking valid
  after `Dispose`.

  **Fixtures are authored, not vendored.** `CnbTestWriter` writes a container through CNA's own
  encoder at test time. A byte array assembled here from a reading of the format would test this
  repository's understanding against itself and would keep passing if reader and fixture were wrong
  the same way. Three tests: the written identity round-trips, a chunk's payload round-trips byte for
  byte (a reader returning correctly sized zeros would pass every size assertion), and a file that is
  not a container is refused.

  One binding defect found by running it: `cna_cnb_read_limits_init` takes a **caller-initialised**
  versioned descriptor, and the binding declared it `out`, so the stamp was never written and the
  route answered "the read-limits structure is not a known size and version". Worth noting for B2 --
  a caller-initialised pointer and an output pointer are both `T*`, so no C prototype check can tell
  them apart. Runtime is the only authority for that one.

- D2 (was). `cnb.h` load path: `ContentManager` extension that loads `.cnb` alongside `.xnb`, starting
  with textures and models, using the same ownership model as the XNB path.
- D3. **First vertical slice done.** `CNA.Graphics.Experimental.RenderTargetPool` and
  `PooledRenderTarget`: CNA's engine-layer pool of reusable render targets, which is the object a
  post-process chain is built on and the smallest piece of the engine layer with an ownership
  contract worth testing.

  `engine_layer.h` is 857 routes. Six are bound. The namespace is `CNA.Graphics.Experimental`, in
  CNA's own vocabulary rather than `Microsoft.Xna.Framework` -- XNA has no such concept, and a game
  allocating render targets by hand is what XNA offers.

  **Second slice done: the post-process chain**, which is the object the pool exists to serve, so the
  two meet at `BorrowTargetPool` rather than sitting beside each other. Fifteen more routes, 21 of
  857 bound. `PostProcessChain`, `PostProcessPass` and `PostProcessFrame`.

  Three ownerships meet in one type and each is asserted: `Add` borrows, `AddOwned` transfers, and
  the target pool is a counted borrow the chain refuses to be destroyed underneath.

  **`AddOwned` is where a test that could not fail was found and fixed.** CNA consumes the pass handle
  whether or not the call succeeds, so ownership is surrendered before the result is checked -- and
  the first version stopped there. The negative control showed the test passed either way: disposing
  a still-owning wrapper releases a consumed handle, native answers a failure result, and
  `NativeResourceHandle` discards it. A surrendered pass now goes *inert* instead, because
  `SafeHandle`'s detach leaves the value readable and the wrapper stayed usable and wrong. With that,
  removing the surrender fails the test.

  **The pixel test had the same problem.** A blit is the identity and an empty chain also copies its
  source, so deleting the pass left every pixel assertion green. What distinguishes them is the pool:
  a two-pass chain ping-pongs and takes exactly one intermediate target; zero- and one-pass chains
  take none. That count is what the test asserts now.

  Two assumptions were measured wrong and recorded as measured: an empty chain refuses a null source
  with `InvalidArgument` rather than leaving the destination alone, and a one-pass chain allocates no
  pooled target.

  **Ownership is the interesting part and it is tested, not documented.** The pool is owned; an
  acquired target is a *borrowed view* released with `cna_render_target_destroy`, which does not
  dispose the pool-owned target; and the pool refuses to reset while any view is outstanding. The
  managed `RenderTarget2D` wrapper is non-owning (`RenderTarget2D.CreateBorrowed`, following
  `Texture2D.CreateBorrowed`), so one handle never has two owners. Measured: reset while borrowed
  answers `InvalidState`, and reset after release empties the pool.

  **Availability is asked, not inferred.** Every engine-layer route resolves in every build and a
  build without the engine layer answers `NOT_SUPPORTED` at call time, so symbol resolution proves
  nothing. This build reports the engine layer available at version 2, so the supported path is
  genuinely exercised: a pooled target is bound, cleared to `0,128,255`, and read back as exactly
  that.

- D3 (was). The post-process/render-pipeline objects, as an explicitly experimental namespace.
- D4. Everything else stays inventory-only until D1-D3 have shipped and been measured.

### E. Template

- E1. The checked-in project builds again and `verify-template.sh` now checks it; keep that check.
- E2. **Premise confirmed, and partly actionable.** The renderer removal has merged: sokol,
  diligent, llgl, igl, wicked, magnum, skia, blend2d, nanovg, openvg and tinygl are absent from the
  live `CNA_GRAPHICS_RENDERER_*` enum -- checked, not assumed, and the identity numbering has gaps at
  10, 19 and 20 where they were. cnanext offers 39 renderer CMake options.

  | renderer | state |
  | --- | --- |
  | OPENGLES3 | `VERIFIED_60_600` -- the template drew 600 frames; native integration 159/159 in Debug and Release |
  | HEADLESS | `VERIFIED_60_600` -- 600 frames and native integration 159/159, and it is what made nine absent capabilities testable |
  | VULKAN | `UPSTREAM_BLOCKED` -- see below |
  | the other 36 | `NOT_BUILT` |

  `NOT_BUILT` rather than untested-by-omission: this cnanext configuration bakes in a single
  renderer (`CNA_GRAPHICS_RENDERER=OPENGLES3`), so each additional one needs its own out-of-tree
  build, and the resulting library is ~170-190 MB apiece. Building 37 of them to run a 600-frame
  smoke test is not a reasonable use of this machine's SSD, and most are unreachable here anyway --
  fourteen DirectX variants, Metal, GDI and Glide are Windows/Apple/legacy, and Canvas, HTML_DOM and
  PixiJS are web.

  **HEADLESS is built and is the most valuable second renderer, for a reason that had nothing to do
  with frame counts.** It was made by turning `CNA_BUILD_C_API=ON` in cnanext's existing
  `cmake-build-headless` directory, which already held 596 objects, so it cost an incremental build
  rather than a fresh one. What it bought is a renderer that genuinely *lacks* things:

  | absent on HEADLESS | present on OPENGLES3 |
  | --- | --- |
  | `Texture3D`, `MultiStreamVertexInput`, `AdditiveBlending`, `CompiledEffects`, `FloatRenderTargets`, `HalfFloatRenderTargets`, `HalfFloatTextureLinearFiltering`, `ComputeShaders`, `IndirectDraw` | all nineteen capabilities |
  | the engine layer (`IsCnaEngineLayerAvailable()` false, version 0) | available, revision 2 |
  | render-target CPU readback, cube-face storage (neither has a capability identity) | both |

  That is what turned A7's remaining half from speculation into measurement -- see A7. A renderer
  matrix measured only for frames would have recorded HEADLESS as uninteresting, because it draws
  nothing; measured for *capability breadth* it is the most informative build on this host.

  HEADLESS does complete 600 template frames, and reports `3D pipeline: yes` while rasterising
  nothing -- which is worth stating rather than assuming either way: the frame count there measures
  the game loop and the binding's call sequence, not the picture, and that is still a different
  thing from OPENGLES3 passing.

  **VULKAN was attempted and is blocked upstream, not by this binding.** Its build directory already
  holds 1,723 objects and needed only `CNA_BUILD_C_API=ON`, but every cnanext CMake configure at
  `a2013068` is refused by cnanext's own `PlatformRatchet` SDL audit over
  `modules/c-api/tests/pure_c/GameSecondaryGraphicsDeviceContextSmoke.c`. It first appeared as an
  untracked file mid-session and is now committed, so it is a property of the tree rather than
  somebody's work in progress.

  Confirmed rather than assumed: re-configuring `cmake-build-headless` -- which configured cleanly
  an hour earlier and produced the library this session tests against -- now fails identically. The
  audit runs at *configure* time, so an already-configured directory still builds, which is why
  upstream's own incremental builds are unaffected and why this is easy to miss. Every renderer
  needing a fresh configure is blocked, SOFTWARE and OPENGL33 included.

  Not worked around: cnanext is read-only here. Recorded in
  [`docs/native-behavior-blockers.md`](docs/native-behavior-blockers.md) with the audit's own three
  suggested remedies. The `cmake-build-vulkan` option was set back to `OFF` afterwards, so that
  directory is as it was found.

- E3. **Done.** The FNA configuration built cleanly and failed at startup with `Game framework
  dependency could not be loaded: FNA`, which is a confusing pair of outcomes and turned out not to
  be a CNA.NET defect at all. FNA's repository ships four project files and only `FNA.Core.csproj`
  produces an assembly a `net8.0` host can load; the only FNA build on this machine was the .NET
  Framework one, which a `net8.0` reference accepts at compile time and cannot load at run time.

  With `FNA.Core` built and `FNA_FRAMEWORK_PATH` pointed at its output, the template runs **600
  frames on FNA over Vulkan (AMD Radeon 780M)** and 600 on CNA over OPENGLES3, from the same game
  source. FNA's native `libFNA3D` and SDL2 were already present; both are FNA's dependencies rather
  than the template's. The requirement is now in the template README so the next person does not
  repeat the diagnosis.

- E3 (was). Diagnose the configured FNA assembly/runtime load failure and complete an FNA frame run.
- E4. Expand the passing MonoGame and Kni runs beyond this one Linux/x64 environment.

### F. Still blocked on things this repository cannot supply

Listed so they are not mistaken for oversights: the Windows XNA 4.0 runtime snapshot, legally
redistributable XACT bank / song / video fixtures, sanitizer evidence from an ABI-matched
instrumented build, and RID coverage beyond Linux x64.

## P0 — public contract and ownership

### 1. Facade-first hierarchy repair: structurally complete

Use composition/internal adapters wherever inheriting a `CNA.*` implementation changes XNA's
public hierarchy. Every exported strict-profile type now has an XNA/BCL base rather than a
`CNA.*` base, and the standalone public/protected leak gate is clean.

Completed on 2026-08-22 across the two facade-repair runs:

1. `Game`, `GraphicsDeviceManager`, `GraphicsDevice`, `GameWindow`, and their service/device-setting
   types are facade-first and no longer expose CNA implementation inheritance.
2. The managed content-reader type system and ordinary custom `Content.Load<T>()` path are
   implemented; its remaining work is fixture breadth and behavioral parity, not hierarchy repair.
3. `Model`, `ModelBone`, `ModelMesh`, `ModelMeshPart`, their exact read-only collections, and nested
   enumerators are composition facades. Loaded models are lifetime-tracked by `ContentManager`.
4. The complete strict Audio/XACT family is facade-first, including exact exception bases and the
   `AudioCategory`/`RendererDetail` kind/layout corrections.
5. Media and Storage use composition and exact BCL/XNA collection bases. The public
   `MediaLibraryObject<TBase>`, `ReadOnlyMediaCollection<TCompat,TBase>`, and
   `NamedModelCollection<T>` implementation bases are no longer exported.
6. All 60 public conversion operators connecting strict XNA value types to `CNA.*` were replaced
   with internal conversion helpers; internal behavior is preserved without adding 120 public
   contract findings.
7. Remaining direct-inheritance graphics/core types were repaired, including `BoundingFrustum`,
   `OcclusionQuery`, `SpriteFont`, state/texture collections, graphics exceptions, `DisplayMode`,
   and `LaunchParameters`.

The final metadata-completion run added all design converters, `GamerServicesComponent`, and the
exact touch enumerator; completed vector, quaternion, matrix, color, plane, ray, rectangle and
bounding-volume families; repaired input state/enums; and closed effects, vertex declarations,
viewport, packed vectors, modifiers, attributes, accessors, constants and parameter names. The
same compile corpus builds against both CNA and Microsoft XNA references, and deterministic
math/geometry and input corpora now provide a cross-engine behavior baseline.

Next work must preserve every zero above. Priorities are now:

1. Execute and archive the 470-observation differential corpus on a Windows XNA 4.0 runtime.
   Direct XNA source/IL has adjudicated implemented audio validation, parent/child ownership and
   frame-buffer behavior, but only Windows can provide the independent runtime snapshot.
2. Continue graphics behavior work from the exact blockers in
   [`docs/native-behavior-blockers.md`](docs/native-behavior-blockers.md): device-event ordering,
   cross-device state/resource validation, additional format/mip/rectangle combinations, and the
   native routes that cannot yet represent XNA behavior.
3. Adjudicate the now-measured 46 Content observations on Windows XNA, then add only legal,
   deterministic fixture breadth that is still missing. The current corpus already includes
   built-in readers, legal/truncated/malformed LZX, external-reference normalization, a
   shared-resource cycle, nested graph failures, and multiple throwing disposables.
4. Do not add native-input CI assertions until CNA exposes deterministic state injection. ABI
   0.20.0 still has hotplug/reset hooks and gamepad/mouse output routes, but no keyboard, mouse or
   gamepad *state injection* route.
5. Preserve the release-qualification CI gates. Protected XNA and native artifacts are optional
   configured jobs; absent configuration must remain explicit rather than silently green.
6. Keep additional XNA profiles separate. GamerServices/Avatar, Net, and Content Pipeline now
   have measured inventories; Xbox and Phone remain pending authoritative legal reference packs.

### 2. Resource ownership

The implementation must distinguish owned, borrowed, adopted, and parent-owned native handles.
`StockEffect.Dispose()` now releases its base reflection handles, fixing a reproduced game-destroy
failure, and `GameComponent.Dispose()` no longer destroys a native component twice.

The separate ownership runner now completes 100 cycles in both Debug and Release. Each cycle
creates a game, exercises textures, buffers, SpriteBatch, SpriteFont atlas ownership, effects and
parent-owned reflection objects, SoundEffect, media, storage, an adopted `Texture2D.FromStream`
handle, and a content-managed model graph, then destroys and recreates the game. Even cycles use
explicit/double disposal; odd cycles abandon resources and force finalization. This found and
fixed a real leak: service-provider-created `CNA.Content.ContentManager` instances held an owned
raw handle without a finalizer, preventing the next game from being created. Owned content
managers now use `NativeResourceHandle`; game-borrowed managers remain non-owning.

The SafeHandle audit now protects native calls with explicit managed liveness (`GC.KeepAlive`),
and owner-thread destruction is queued, drained, and retried at game safe points. Regression tests
cover finalizer deferral, cross-thread disposal, parent-release retry, callback exception
containment, failed unsubscribe rooting, and XNA-order `GraphicsDevice.Disposing` delivery after
the disposed state becomes visible. Managed teardown stays in `finally` when a handler throws.

XNA IL establishes strong `Cue`/bank-to-engine parent edges and engine-first dependent disposal;
the implementation now follows it. Authored-bank success and disposal ordering still need a legal
XGS/XSB/XWB fixture. `VideoPlayer.GetTexture` is now safe but necessarily transient: CNA exposes a
player-owned borrowed alias valid only until the next player call, while XNA maintains two stable
managed frame-buffer objects. Exact XNA identity is an upstream ABI blocker. Remaining criteria
include asset-backed XACT/video success, further multithreaded evidence, sanitizer evidence from an
exact ABI-matched instrumented build, and an upstream owner-thread pump if handles may outlive every
managed `Game` safe point. The 1000-cycle deep run has passed, without an allocator-level leak claim.

## P0 — content compatibility

The structural managed work is complete. `ContentManager` has the correct public base relationship,
service-provider constructors, cache, unload and resource-manager facade. `ContentReader` derives
from `BinaryReader`; abstract and generic `ContentTypeReader` contracts, reader-table activation
and versioning, shared resources, nested/existing objects, disposable tracking, LZX handling and
ordinary custom `Content.Load<MyType>()` are implemented.

The pure corpus now has 46 deterministic content-error observations covering invalid magic,
platform/version/profile, truncated and inconsistent headers/payloads, compression flags, reader
table activation/version/index failures, shared-resource indices, wrong target type, missing
assets, reader exceptions, duplicate disposables, cache/root-directory behavior, and unload/dispose
after failure. The managed path now normalizes truncation to `ContentLoadException`, validates XNB
headers and reader versions/indices, records duplicate disposable occurrences like XNA, clears
unload state in `finally`, and remains disposed after an unload exception.

The latest twelve observations add uncompressed built-in reader success/failures, a legal LZX
uncompressed block, truncated and malformed LZX blocks, a normalized external-reference chain, a
shared-resource cycle, late shared-graph cleanup, multiple throwing disposables during both
`Unload` and `Dispose`, and deterministic nested-failure/cache/stream state. Remaining work:

1. Expand additional built-in-reader and legal compressed fixtures only where they add a distinct
   failure/ownership route; keep MonoGame LZ4 inventoried separately as an extension.
2. Add further representable partial shared-resource graphs and deeper external-reference failure
   chains without using copyrighted content.
3. Execute the identical 46 observations on Windows XNA and preserve the zero-diagnostic content
   surface while adjudicating any differences.

Completion: a user-defined reader/content type loads through unchanged XNA-style source and the
same fixture produces normalized equivalent results on an available reference implementation.

## XNA profile inventory

| Profile/family | Current treatment | Completion criterion |
| --- | --- | --- |
| XNA 4.0 Windows runtime | Metadata complete: 257/257 types, 0 differences, 0 CNA leaks | Preserve zero; complete behavioral corpus |
| XACT runtime | Included in current seven-assembly profile | Exact API plus authored-bank tests where assets exist |
| GamerServices/Avatar | 2 legally supplied assemblies measured: 51 types, 502 members, 0 overlap with the 257-type baseline | Inventory-only; services/runtime availability remains unqualified |
| Networking/session APIs | 1 Net assembly measured: 23 types, 174 members, 0 overlap | Inventory-only separate future profile |
| Windows Phone sensors/devices | No authoritative legal reference pack configured | Pending; no type/platform claim |
| Xbox-only APIs | No authoritative legal reference pack configured | Pending; no type/platform claim |
| Content Pipeline/build-time assemblies | 7 assemblies measured: 128 types, 743 members, 0 overlap | Separate future build-time product; never count it as runtime completion |

## Behavioral audit

The earlier 2026-08-22 source scan found 0 `NotImplementedException` sites, 36 explicit
`throw new NotSupportedException` sites, and 2 `TODO` comments (both in the LZX decoder's documented
reference-compatible edge paths). This run added deliberate `NotSupportedException` guards where
the native ABI cannot represent XNA semantics; the exact active native blockers are classified in
[`docs/native-behavior-blockers.md`](docs/native-behavior-blockers.md), so raw exception counts are
not a progress metric.

- Keep searching `TODO`, `FIXME`, unsupported exceptions, fallbacks, constant answers, empty
  methods, and documented deviations.
- Add golden/differential tests for validation order, exception types, null/range/disposed cases,
  event order, collection mutation, math edge cases, graphics state, content caching, input
  transitions, audio/media state, and lifecycle ordering.
- Keep alternate-engine results as comparators rather than authorities. The current 470-observation
  snapshot preserves the earlier corpus and derives every category/probe count from one manifest.
  FNA's finalizer abort and MonoGame's audio initialization failure demonstrate why comparator
  crashes cannot define XNA behavior. Actual XNA metadata/source/IL, documentation, and the Windows
  runtime snapshot decide strict behavior.
- Preserve strict behavior in `CNA.XnaCompat` even where `CNA.Framework` intentionally differs.
  `CurveKeyCollection.Clone()` now demonstrates this: compat is shallow while the CNA API remains
  independently designed.

Completion is a published behavior matrix identifying the reference implementation and exact test
corpus, not a percentage inferred from source.

## CNA ABI and interop

`tools/coverage` now discovers repositories/libraries relatively or through `CNA_ROOT`,
`CNA_NATIVE_LIBRARY`, and `CNA_NATIVE_DIR`; ELF, PE, and Mach-O symbol tools are separated. The
latest header sweep against the 0.20.0 headers found no declared imports absent and no arity
mismatches. The selected ABI 0.20.0 library passes all 122 integration tests in both
configurations. `tools/abi-verify` uses the platform C compiler as authority: Linux ELF x64 passes
86 native/managed measurements with zero mismatches and compiles the reviewed function/callback
prototypes against the 0.20.0 header tree. PE and Mach-O jobs reuse the same source and test logic,
but have not been executed in this local Linux checkpoint.

`tools/coverage/baselinediff.py` supplies the upstream release-to-release evidence the policy asks
for, and is tested against a planted failure in both directions. See
[`tools/coverage/README.md`](tools/coverage/README.md).

Native admission is now explicit policy `cna-cs-native-abi/1`; see
[`docs/native-abi-compatibility.md`](docs/native-abi-compatibility.md) and the machine-readable
[`eng/cna-native-abi-policy.json`](eng/cna-native-abi-policy.json). CNA's experimental 0.x minors
are not assumed compatible. The reviewed matrix accepts exactly 0.19.0. It previously accepted
0.6.0, 0.7.0 and 0.8.0; those entries were retired when this binding began importing four routes
CNA added after 0.8.0, which no earlier library exports -- a consequence of the consumer moving,
not a finding against those reviews. Every other version is rejected by default. The loader
additionally requires all 881 imported symbols and executes guarded core signature/struct-shape
probes. Eleven dependency-free fixture libraries prove two positive and nine negative cases in
fresh processes, including that a retired generation and both neighbours of the accepted one are
refused.

Next criteria:

- widen the portable manifest from its current floor to every interop struct, prototype and
  enum-like constant (see Open work B);
- require a reviewed policy-matrix entry and upstream baseline diff before admitting any new CNA
  ABI generation; never restore a same-major 0.x range;
- symbol resolution against current Linux, Windows, and macOS builds using the platform's real
  export mechanism;
- callback rooting/concurrency tests and systematic SafeHandle add-ref/keep-alive audit;
- never infer cross-platform ABI validity from `nm` alone.

## Extensions

Current evidence is deliberately narrow:

- Strict XNA: exact public metadata for the selected Windows runtime profile; behavioral parity and
  additional product profiles remain incomplete.
- FNA: the template source compiles against a configured FNA.dll; no FNA extension subset is yet
  promised.
- MonoGame: the template compiles and completes 60 frames against DesktopGL 3.8.1.303; extension
  inventory and broader runtime matrix are pending.
- Kni: the template compiles and completes 60 frames against the 4.2.9001 framework and
  4.2.9001.1 SDL2.GL backend, while the strict compile corpus records Kni's non-XNA
  `VertexDeclaration` ancestry; broader runtime matrix is pending.
- CNA: renderer diagnostics live in `CNA.XnaCompat.Extensions`; the strict namespace now has zero
  public/protected CNA-type leaks.

For each proposed FNA/MonoGame addition, record authority, source-portability value, implementation
status, and whether it belongs in an extension namespace/assembly. Do not merge APIs into a random
union.

## Template

The sibling `cna-cs-template` is now CNA-first and installable as `cna-game`. It uses raw
`Texture2D.FromStream` for the PNG, isolates CNA diagnostics, exercises 2D and a guarded 3D path,
and supports `--smoke-test`, `--stability-test`, and `--frames N`. Development mode preserves the
sibling project-reference workflow. Package acceptance mode emits only `PackageReference` and has
passed an isolated local-feed build plus native smoke/stability execution.

Next criteria:

- 600-frame CNA stability run on every additional claimed renderer (OPENGLES3 passes);
- diagnose the configured FNA assembly/runtime load failure and complete an FNA frame run;
- expand the passing MonoGame and Kni runs beyond this one Linux/x64 environment;
- generated project build outside both repositories on all supported hosts;
- generated output reduced to the clean consumer template if repository-only portability harness
  files become intrusive.

## Packaging and platform matrix

The intended package graph and measured local harness are documented in `docs/packaging.md`.
Production projects remain non-packable by default. The explicit acceptance property has produced
and installed local preview packages in an isolated consumer, including a locally selected
`runtimes/linux-x64/native/libcna_c_api.so`; this proves mechanics only, not publication or RID
support. Missing/wrong ABI, missing symbol, invalid explicit path, conflicts, package-native
resolution, and explicit-override precedence are exercised.

No OS/architecture is supported by claim until restore, Debug/Release build, ABI check, generated
template build, and native smoke/stability runs pass there. Current runtime evidence is Linux x64
with OPENGLES3 only.

## CI/tooling gates

The release-qualification workflow now wires:

1. restore and Debug/Release solution build;
2. both managed test suites and the compile probe;
3. strict metadata diff plus a separately runnable CNA leak check;
4. portable ABI/header validation;
5. the upstream release-to-release ABI diff over the consumed surface, which must report zero
   breaking differences;
6. fixture-based native ABI policy admission plus native integration when an admitted library is supplied;
7. repository-project, development-template and generated-project builds;
8. pure behavior corpus/count/generated-doc validation;
9. local package creation and isolated package-consumer install/build test;
10. protected native integration, ownership, full corpus, template native runs, and package-native
    acceptance when an ABI-matched library is legally supplied.

The strict API job is green with zero diagnostics and an empty allowlist. Treat any future
diagnostic—including a leak, hierarchy/interface regression, unexpected extension, or parameter
name change—as a gate failure. CI reference assemblies must be supplied legally through
`XNA_REFERENCE_PATH` or an equivalent protected artifact. The checked-in workflow never downloads
Microsoft assemblies publicly and reports protected XNA/native gates as `not-configured` when their
artifacts are absent.

## Precise upstream CNA requirements

Managed work should continue around these. Do not modify upstream without authorization. The
argument shape, observed fallback, and required versioned C ABI for each graphics limitation are
specified in [`docs/native-behavior-blockers.md`](docs/native-behavior-blockers.md). In summary:

- `Present(source,destination,window)` needs a versioned descriptor; the current route carries no
  arguments, so only the all-default tuple is forwarded.
- Dynamic index window uploads reject non-`None` options, and upstream states that as intended:
  "a windowed upload preserves the rest of the buffer, so it accepts no SetDataOptions other than
  None". XNA accepts them, and the vertex family took the opposite decision in 0.19.0, so this
  needs a decision rather than a route.
- Vertex-buffer partial-element strided transfers need independent element-size and buffer-stride
  scatter/gather fields; complete declaration-sized and contiguous full-byte windows work.
- Texture2D arbitrary compatible structs, broader backbuffer readback, raw Texture3D readback, and
  raw per-face TextureCube transfers need format/element-size-aware byte routes.
- Resource create/destroy callbacks need a stable resource identity and round-trippable tag. The
  historical managed two-argument thunk did not match the native three-argument callback and could
  crash immediately; the current managed API safely emits no fake event.
- Dynamic vertex/index-buffer `ContentLost` needs real renderer loss/recreation notifications.
  Render targets got `cna_render_target_subscribe_content_lost` in 0.19.0 and are bound to it; the
  buffer families did not, and the render-target route is the shape to follow.
- The selected backend needs XNA-compatible SpriteBatch treatment for unknown sort values and
  non-finite sprite data. Compressed DXT upload is no longer on this list: the renderer reports
  Dxt1/Dxt3/Dxt5 and the authored font fixture loads.
- `VideoPlayer.GetTexture` needs stable frame-slot identity or an explicit validity generation;
  the current borrowed alias expires on the next player call and cannot reproduce XNA's two stable
  frame `Texture2D` objects.
- Independent-device creation exists as of 0.19.0 and is bound, but on OPENGLES3 it makes its own
  GL context current and does not restore the game's, so a game in the same process dies on its
  next `SwapBuffers`. Cross-device validation stays not-run behind
  `CNA_RUNTIME_PROBE_CROSS_DEVICE=1` until the context is saved and restored. Deterministic
  `DeviceLost` and native input transitions still need explicit test hooks. No second device
  inside a running game, loss event, or physical input state is fabricated.
- `cna_storage_container_subscribe_disposing` is documented as synchronous and exactly once but
  emitted zero callbacks in the native regression. Explicit managed disposal safely emits the
  known one-shot event until native honors the route.
- Compiled effect bytecode needs a renderer/native implementation before `.fx` runtime support can
  be claimed on that renderer.
- The historical SIGTERM `pure virtual method called` shutdown report remains open until reproduced
  or disproved on the current native build.

Custom managed content readers are not listed here because their managed XNB machinery is now
implemented and does not require an upstream CNA change.

## Reproducible commands

```bash
dotnet build CNA.sln -c Debug
dotnet build CNA.sln -c Release
dotnet test tests/CNA.Framework.Tests/CNA.Framework.Tests.csproj
dotnet test tests/CNA.XnaCompat.Tests/CNA.XnaCompat.Tests.csproj

CNA_NATIVE_LIBRARY=/path/to/libcna_c_api.so \
  xvfb-run -a dotnet test tests/CNA.Integration.Tests/CNA.Integration.Tests.csproj -c Debug
CNA_NATIVE_LIBRARY=/path/to/libcna_c_api.so \
  xvfb-run -a dotnet test tests/CNA.Integration.Tests/CNA.Integration.Tests.csproj -c Release

dotnet run -c Release --project tests/CNA.XnaCompat.CompileProbe
CNA_NATIVE_LIBRARY=/path/to/libcna_c_api.so \
  XNA_GRAPHICS_PROBE_DRAW_VALIDATION=1 \
  XNA_GRAPHICS_PROBE_DESTRUCTIVE_LIFECYCLE=1 \
  XNA_GRAPHICS_PROBE_UNSAFE_CONSTRUCTORS=1 \
  xvfb-run -a dotnet run -c Release --project tests/CNA.XnaCompat.GraphicsProbe

CNA_NATIVE_LIBRARY=/path/to/libcna_c_api.so \
  CNA_OWNERSHIP_STRESS_FAMILY=all \
  xvfb-run -a dotnet run -c Release \
  --project tests/CNA.OwnershipStress/CNA.OwnershipStress.csproj -- 100

CNA_NATIVE_LIBRARY=/path/to/libcna_c_api.so \
  CNA_OWNERSHIP_STRESS_DEEP=1 \
  xvfb-run -a dotnet run -c Release \
  --project tests/CNA.OwnershipStress/CNA.OwnershipStress.csproj

dotnet run --project tools/behavior-corpus -c Release -- verify
CNA_NATIVE_LIBRARY=/path/to/libcna_c_api.so \
  scripts/Capture-CnaSnapshots.sh --output /tmp/cna-snapshots --force

CNA_UPSTREAM_ROOT=/path/to/cnanext scripts/Verify-Abi.sh /tmp/cna-abi.json

python3 tools/coverage/baselinediff.py \
  --from /path/to/cna@<last accepted revision> --to /path/to/cnanext

scripts/Verify-NativeAbiCompatibility.sh --output /tmp/cna-abi-policy

scripts/Package-Acceptance.sh \
  --native-library /path/to/libcna_c_api.so \
  --output /tmp/cna-package-acceptance

XNA_REFERENCE_PATH=/path/to/xna-reference-assemblies \
  dotnet run --project tools/api-compat -c Release -- --format json

CNA_ROOT=/path/to/cnanext python3 tools/coverage/sweep.py
```

On Windows with XNA 4.0 and the .NET Framework 4.8 developer pack:

```powershell
.\scripts\Capture-XnaSnapshots.ps1 `
  -XnaReferencePath 'C:\path\to\XNA\References\Windows\x86' `
  -CnaSnapshotPath 'C:\path\to\cna-snapshots\cna-all.txt' `
  -OutputDirectory 'C:\path\to\xna-snapshots'
```
