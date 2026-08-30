# CNA.NET engineering roadmap

Last measured: 2026-08-30, against CNA `next` `e178282fcd70f4cd1e9be922fde35a9a2b779cf3`
(C ABI 0.20.0) and the `sharp-runtime` revision that generation builds on. Session history and
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
| Managed tests | 560 `CNA.Framework` + 208 `CNA.XnaCompat`, all passing |
| Native integration | 125/125 passing in Debug and Release on Linux x64 against the ABI 0.20.0 CNA OPENGLES3 library |
| Native ABI admission | Consumer ABI 0.20.0; the reviewed `cna-cs-native-abi/1` matrix accepts exactly that generation, requires all 854 imports, and runs signature/shape canaries. 11 isolated fixtures: 2 accepted, 9 rejected |
| Upstream ABI diff | `tools/coverage/baselinediff.py` measures 0.8.0 → 0.19.0 as strictly additive over the consumed surface (1,189 exports added, nothing removed or changed), and 0.19.0 → 0.20.0 as 12 renderer-identity constant differences and nothing else |
| Compile probe | Same source builds for CNA and FNA; the MonoGame pure probe builds after recording absent `RendererDetail` dynamically. The future XNA net48/x86 build remains integrated in the Windows snapshot command. Kni still differs at `VertexDeclaration : GraphicsResource` |
| Behavior corpora | One manifest defines 470 observations: 83 Math, 23 Input, 153 Graphics, 13 Resource, 46 Content, 83 Audio, 7 XACT, 20 Media, 17 Video, 20 Storage, and 5 DeviceLifecycle. CNA executes all 470: 199 pure, 166 device, and 105 native-runtime. Windows XNA runtime capture remains pending |
| Windows XNA snapshots | Release-grade validation/build/normalize/manifest/compare workflow implemented; platform-independent manifest/count/compare paths pass locally. Actual Windows XNA execution is not-run/pending |
| Ownership stress | Normal Debug and Release each pass 100/100 cycles, now including the authored DXT3 `SpriteFont` the cycle used to exclude: 1,600 queued owner-thread releases, 3,000 successful release attempts, 0 retries/failures/pending releases, 0 refused game destroys, 0 native crashes. This is not allocator-level leak proof |
| Sanitizers | `not-run`: no exact ABI-compatible ASan/UBSan CNA build was available; no sanitizer-cleanliness inference is made |
| ABI layout evidence | Portable C-authority probe passes on Linux ELF x64: 86 native and 86 managed layout/type measurements with 0 mismatches, plus prototype compilation for reviewed callbacks/functions. That is a floor, not coverage: it measures 13 of the 80 interop structs. Windows PE and macOS Mach-O jobs are wired but actual execution remains pending |
| XNA Windows runtime metadata | 257 reference types, 257 target types, 0 differences, empty allowlist. Run locally against a legally obtained reference set with `XNA_REFERENCE_PATH`; the gate caught three signature regressions during this session and is worth running after every facade change |
| CNA public-type leakage | 0 findings in public/protected strict-profile signatures |
| Real-game compile probe | An unmodified 18,391-line Windows Phone XNA game ported to MonoGame compiles against the facade with one unresolved call: `Mouse.SetCursor`, which is MonoGame's addition rather than XNA 4.0. Now offered as `CnaMouse.SetCursor` in the CNA extensions |
| Compiled-content survey | 517 assets of the XNA 4.0 sample collection: 498 load through CNA's own content loader, 18 name a type only the game's own assembly supplies, **0 need a built-in reader this binding does not have**, 0 unreadable |
| Template | The checked-in repository project, the generated development project and the isolated package consumer all build. The package-generated project contains no source root, sibling `ProjectReference`, or developer absolute path; native 60/600-frame acceptance passes against 0.20.0 |
| Other engines | Source builds pass for FNA, MonoGame, and Kni; 60-frame MonoGame and Kni runs pass; configured FNA runtime reports unavailable with exit 2 |
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
| A1. `SpriteBatch.DrawString` through the native text route | `cna_sprite_batch_draw_string` | Measure it against the current per-glyph quad path first. Adopt only if it is not observably different in glyph placement on the authored `FontCalibri14` fixture; record the measurement either way. |
| A2. Batched sprite submission | `cna_sprite_batch_submit_many` | A `SpriteBatch` flush issues one native call per batch rather than one per sprite, with the same draw order and the same `End` failure behaviour. |
| A3. `PresentationParameters` bounds and clone | `cna_presentation_parameters_get_bounds`, `cna_presentation_parameters_clone` | `PresentationParameters.Bounds` and `Clone()` read/copy through native instead of managed reconstruction, and round-trip in the corpus. |
| A4. Preferred presentation mode | `cna_graphics_device_manager_get/set_preferred_presentation_mode_ext` | Exposed in `CNA.XnaCompat.Extensions`, not in the strict namespace. |
| A5. Explicit content-lost notification | `cna_graphics_device_notify_content_lost_resources_ext` | Only as a test hook, to drive the render-target `ContentLost` subscription deterministically on a renderer that cannot lose a device. Never called from an ordinary game path. |

### A6. Content, now measured rather than guessed

`tools/content-survey` answers "how much of a real game's compiled content can this read" against
any `Content` folder. Against the XNA 4.0 sample collection there are no missing built-in readers
left. What that does *not* say is that the bytes after each reader table are read correctly, which
is the honest limit of a resolution survey. Remaining work:

- A6a. Done for a second corpus, and it paid for itself. `cna-samples` is 527 assets and **80
  distinct readers** against the first corpus's much narrower spread: 0 missing built-ins, 0
  malformed, 507 native-backed, 19 naming a game's own types. Doing this found A6b's premise to be
  false. Still worth pointing at more games; one corpus is one corpus, and two is two.
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
- A6c. The compressed assets are analysed but never fully read here. A loading survey mode, behind a
  graphics device, would close that gap.

### A7. Dynamic skip does not work in the integration suite

`Xunit.Sdk.SkipException.ForSkip(...)` is used in three integration tests to mean "this renderer
cannot do the thing". Under `dotnet test` with `xunit.runner.visualstudio` 2.8.2 the runner does not
honour it: the skip arrives as a failure whose message is the marker
`$XunitDynamicSkip$...`. None of the three has fired yet, so nothing has gone red, but each is a
test that will report a defect the first time it meets a renderer it was written to tolerate.

Bumping the runner to 3.1.5 was tried and rejected: it did not honour the skip either, and it broke
an unrelated test. The mouse-cursor test therefore asserts both outcomes instead of skipping one,
which works on any runner. Either convert the remaining three the same way, or find a runner that
honours dynamic skip and prove it by making one fire.

Related, and found the same way: `GraphicsDevice_GetVertexBuffers_ReportsWhatWasBound` asserted that
no vertex buffer was bound when it started. Every test in that assembly shares one device, so that
precondition held only by accident of test order and adding a test to the class broke it. Worth a
sweep for the same shape elsewhere -- an assertion about state a test did not establish is an
assertion about what ran before it.

### A8. Speedy Blupi reports silent audio that no measurement reproduces

Reported from a real play session: the game runs and is playable, but sound is inaudible except for
one click. Every layer was then measured rather than reasoned about, using SDL's `disk` audio driver
(which writes the mixed output to a file, so "was it audible" becomes a number) and a PipeWire
sink-monitor capture (which says what actually reached the speakers):

- all 93 of the game's sound assets load, none empty, durations 0.052s to 4.489s
- the generated-PCM route, the managed reader route and CNA's own loader route all produce full-scale
  audio, and the three agree
- pitch is correct, not merely accepted: 440 Hz becomes 880 Hz at pitch 1.0, XNA's own semantics
- an instance returns to `Stopped` when its sound ends, which the game's `IsFree` depends on
- eighteen overlapping undisposed instances plus a looping one keep playing, with nothing refused
- **the game itself emits audio**, headless and on the real display: peak 29363 into the mixer and
  peak 30602 measured on the speaker sink's own monitor
- its stream is uncorked, unmuted, at 100%, on the default sink, which wakes SUSPENDED -> RUNNING

So the binding is not silent and the reported fault is not reproducible from here. It is also
intermittent for the reporter -- sound worked once and then stopped again -- which is the shape of an
environment or a device-state problem rather than a code path. `scripts/Capture-GameAudio.sh` exists
to settle it from the reporter's own machine while the silence is happening: it captures the mixer
output, the sink monitor, and the audio server's view of the stream, which are three things a
listener cannot tell apart. Nothing further should be changed in the audio binding without that
capture, because every hypothesis cheap enough to test blind has now been tested and rejected.

The last gap in the measurement is now closed too: the game was driven past its menu and into a
real level with `xdotool`, and sound played throughout, peak 29290 at the speaker sink. Every
earlier measurement had it sitting in the menu, which was the one difference from the reporter's
"I played the game" that could still have mattered.

The first capture the reporter ran did fail, and the failure was in the script rather than the game:
it did not pass `CNA_NATIVE_LIBRARY` through, so the game died in its constructor, and the script
then reported "never opened the device" -- which reads like a finding. It now supplies the library,
prints which one it used, and distinguishes a game that is running and silent from one that is not
running.

One real defect did come out of the session, fixed below.

### A9. The window had no title (fixed)

XNA names the window before the game's `Initialize` runs -- `WindowsGameWindow`'s constructor does
`base.Title = GetDefaultTitleName()` -- and almost no game sets a title itself. Speedy Blupi does
not, and its window came up blank. CNA supplies no default either, so nothing named it.

`GameWindow.ApplyDefaultTitle` now reproduces XNA's rule: the entry assembly's
`AssemblyTitleAttribute` when non-empty, then the executable's file name, then the literal `Game`,
with one addition XNA never needed -- a framework-dependent .NET app runs as `dotnet`, which names
the host rather than the game, so that name is skipped in favour of the assembly name. It is applied
in `Game`'s initialize path and yields to any title the game set itself, since a game that sets one
in its constructor does so before the window exists.

### B. Deepen the ABI evidence

The C-authority probe measures 13 of the 80 interop structs and compiles 4 of 854 prototypes. That
was defensible against a one-minor step; against a twelve-minor one it is a floor, and it is now the
weakest link in an admission that otherwise rests on the upstream baseline diff.

- B1. Extend `tools/abi-verify/native_layout_probe.c` to every struct `CNA.Interop` declares, and
  `BuildManagedValues()` to match. A managed struct with no measured native counterpart must fail
  rather than be skipped.
- B2. Extend the prototype probe from 4 routes to every callback-taking route and every route whose
  managed declaration uses `in`/`out`/`ref` on a versioned descriptor -- the shapes `sweep.py`'s
  arity check cannot see.
- B3. Emit every consumed enum-like constant from the C probe and compare it against the managed
  enum member, so a renumbered identity fails a gate instead of a game.
- B4. Execute the Windows PE and macOS Mach-O `portable-abi-header` jobs. They are wired and have
  never run.

### C. Close or re-adjudicate the remaining native blockers

[`docs/native-behavior-blockers.md`](docs/native-behavior-blockers.md) is the list. Six rows closed
when the binding moved to 0.19.0/0.20.0. The ones that need managed work rather than upstream work:

- C1. Done: both caller-owned `GraphicsDevice` constructors now warn at the call site that creating
  a device while a game is running takes the GL context and kills that game's next present. The
  underlying fix is upstream's -- save and restore the current context around device creation.
- C2. Re-measure the whole blocker table against a 0.20.0 build. The renderer removal has merged,
  and several rows are backend statements rather than ABI statements, so a smaller renderer set can
  change them in either direction.

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

CNA 0.20.0 exports 4,051 routes; this binding consumes 854. The remainder is not all product
surface -- most of `vectors.h`, `matrix.h`, `math.h`, `quaternion.h`, `curve.h`, `geometry.h` and
`color.h` is deliberately managed by design invariant 3, and much of the rest is engine-internal.
What is genuinely a CNA-beyond-XNA product surface, by header and unbound count:

| Header | Unbound | What it is | Placement |
| --- | --- | --- | --- |
| `engine_layer.h` | 812 | CNAEXT: storage buffers, compute shaders, GPU timers, render-target pools, shader-effect caches, full-screen/post-process passes, PBR material binding, clustered lighting, shadows, SSAO/SSR/bloom/tonemap, particles, decals, LOD | `CNA.Framework.Extensions` + `CNA.XnaCompat.Extensions` |
| `cnb.h` | 272 | CNA's own binary content format: encode/decode for textures, models, video, documents, plus the tooling front ends | `CNA.Content.Cnb` |
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
- D2. `cnb.h` load path: `ContentManager` extension that loads `.cnb` alongside `.xnb`, starting
  with textures and models, using the same ownership model as the XNB path.
- D3. The post-process/render-pipeline objects, as an explicitly experimental namespace.
- D4. Everything else stays inventory-only until D1-D3 have shipped and been measured.

### E. Template

- E1. The checked-in project builds again and `verify-template.sh` now checks it; keep that check.
- E2. 600-frame runs on every additional claimed renderer, once the renderer set settles.
- E3. Diagnose the configured FNA assembly/runtime load failure and complete an FNA frame run.
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
additionally requires all 854 imported symbols and executes guarded core signature/struct-shape
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
