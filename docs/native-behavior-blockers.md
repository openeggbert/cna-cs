# Native blockers for strict XNA behavior

Measured against read-only CNA commit `599d14e54e073b566d77b3d6fb30ac52d3d810b7` on the `next`
branch (C ABI 0.21.0) on 2026-08-31, on the OPENGLES3 **and HEADLESS** renderers, with cnanext
built against sharp-runtimenext `4a49afb0cfe6a41e6e0af0bb62dc5175976731bb`. No upstream source was
modified. The C API headers are **not** byte-identical to the previous measurement's: six changed,
so every row was re-measured rather than carried forward.

**Three rows closed in this pass and one was added**, which is the most this table has moved in a
single generation. The three closed because upstream fixed them, and each closure is proven against
the running artifact rather than read from a commit message -- a distinction that earned its place
here, because a fourth upstream change with an equally promising subject line ("preserve threaded
EasyGL context handoff") is the direct cause of the row that was added.

A second renderer remains part of this measurement and it mattered. HEADLESS reports nine of
nineteen graphics capabilities absent and contains no engine layer, so refusals this table describes
can be observed rather than only reasoned about -- and one row turned out to be false.

`scripts/Verify-BlockerTable.sh` checks the two things about this table that can be checked
mechanically: every `cna_*` route a row names still exists in the canonical headers, and the
generation this document says it was measured against is the generation of the headers it is being
checked against. A blocker is a claim about upstream, and claims rot silently -- a route gets added
and the row keeps saying it is missing, which is how a table becomes a museum. At the commit above:
**17 routes named, 17 present, document ABI 0.21.0 = header ABI 0.21.0.**

**It enforces both now, and it enforced neither before.** It printed `BLOCKER_ROUTE_ABSENT` and then
exited 0, so a row naming a route no header declares was a green run -- which is not a hypothesis:
the render-target clear row below was first written naming `cna_graphics_device_clear`, a route that
does not exist under any of its three real spellings, and the script reported it and passed. The ABI
line had the same shape, printing the header's minor beside a document that states its own measured
generation in prose and comparing neither. Both failure modes are now exit 1, and both were proven
by mutating a copy of this file and watching it go red.

The prose of each row still needs a human, and the buffer `ContentLost` row is the proof: every
route it named was present, so the mechanical check passed, while the row's own sentence claimed the
routes did not exist. **A route-presence check cannot catch a row that is wrong about a route being
there.** See [Closed since ABI 0.7.0](#closed-since-abi-070) for what it said and what is true.
This file is an implementation handoff, not permission to change the upstream `cna` repository.
Microsoft XNA 4.0 source/IL and a future Windows runtime snapshot are authoritative; FNA and
MonoGame results are only differential evidence.

The previous revision of this file was measured against ABI 0.7.0. Eleven of its rows are gone --
six because CNA closed them, five because they were mistaken about what the ABI made impossible.
All eleven are listed under [Closed since ABI 0.7.0](#closed-since-abi-070) so a reader can tell a
resolved blocker from a forgotten one.

| Affected XNA API | Missing or defective native route | Observable CNA.NET behavior | Required upstream change | Safe managed fallback |
| --- | --- | --- | --- | --- |
| `GraphicsDevice.Present(Rectangle?, Rectangle?, IntPtr)` | `cna_graphics_device_present` accepts only the device. It cannot carry either rectangle or an override window. | The all-default argument tuple uses parameterless presentation. Any non-default argument throws `NotSupportedException` instead of silently presenting the wrong region/window. | Add a versioned presentation descriptor with independent optional source/destination rectangles and a pointer-width opaque override-window token, then implement clipping and override-target behavior for each backend. | Deterministic refusal for every non-default tuple. |
| `GraphicsDevice.Clear` into a bound render target | Not a missing route: `cna_graphics_device_clear_options` -- the route this binding calls, of the three the header declares alongside `cna_graphics_device_clear_rgba` and `cna_graphics_device_clear_color_depth` -- exists and returns success. The clear is simply not there afterwards. `RenderTargetClearTests` binds a 4x4 `RenderTarget2D`, clears it to `0,128,255,255`, unbinds and reads back `0,0,0,0`. **Its twin passes**: draw one sprite between the clear and the unbind and both the sprite *and* the clear underneath it read back exactly as asked. So render targets, readback and the binding's clear arguments are all fine -- what is lost is a clear that nothing follows before the target is unbound. Appeared with ABI 0.21.0, where `GraphicsDevice::Clear` and `Present` gained a renderer thread-context lease whose release unbinds the GL context. | Three integration tests fail: `PostProcessChainTests` twice (a blit chain produces black) and `RenderTargetPoolTests` once (a pooled target will not take a clear). They are one defect wearing three costumes, which is why the minimal statement of it was added as its own test rather than left implicit in theirs. Nothing is worked around and no result is swallowed. | Make a clear issued into a bound target survive the unbind -- flush it at the context-lease boundary, or at `SetRenderTargets`, or do not release the context there. | **None, deliberately.** A managed workaround would mean issuing a dummy draw after every clear, which would hide the defect and cost a draw call per clear on every renderer. The tests stay red. |
| `GraphicsDevice.ResourceCreated` / `ResourceDestroyed` | The current callbacks correctly have three arguments `(device, info*, context)`. However, created info contains only `has_resource`; destroyed info contains name and only `has_tag`. No stable resource identity or round-trippable tag crosses the ABI. Native content paths can create resources without a managed facade constructor. | CNA.NET does not subscribe and emits zero fake events. The historical two-argument managed thunk would misread `info` as `context` and can crash on the first callback. | Supply a stable opaque resource identity that every direct and content-created resource can correlate with its eventual managed wrapper, and a round-trippable tag token on destruction. Define construction/destruction ordering and keep callback exceptions contained. A new versioned callback/info contract is preferable to changing the existing signature in place. | Store handlers safely but emit no event until the actual facade resource and tag can be supplied. |
| `AudioEngine(string, TimeSpan, string)` renderer and look-ahead semantics | `cna_audio_engine_create_with_renderer` carries both arguments, but the ABI documentation and implementation explicitly accept and ignore them because CNA exposes one backend and no scheduling look-ahead. | Strict managed XGSF validation and exception ordering run first, then the actual three-argument ABI route is called. Any renderer id and look-ahead therefore produce backend-default behavior when the authored settings file is otherwise valid; no renderer-selection success is claimed. | Implement renderer enumeration/selection and scheduling look-ahead, reject unknown renderer ids with XNA-compatible timing/type, and document representable look-ahead bounds. The current ABI parameter shape is sufficient. | Forward the caller's values unchanged. Do not silently substitute a claimed renderer or scheduling policy; document that current native behavior ignores both. |
| `VideoPlayer.GetTexture()` identity and lifetime | The ABI returns a borrowed frame-texture handle documented as valid only until the next call on that player. XNA's implementation owns two managed `Texture2D` frame buffers and reuses their identities; the ABI supplies neither a stable frame slot identity nor a lifetime beyond the next call. | Before `Play`, the strict facade throws `InvalidOperationException`. With a frame, it exposes at most one non-owning transient `Texture2D` facade and invalidates it before every subsequent player call. Disposing/finalizing the facade never destroys the player-owned native texture. Asset-backed identity/frame-advance tests remain pending. | Expose stable player-owned frame slots (or a stable opaque identity plus an explicit validity generation) and device association, with documented ownership and frame-advance ordering. | One-live borrowed facade with deterministic invalidation. Never cache duplicate owning wrappers or claim XNA-stable identity. |
| Deterministic `GraphicsDevice.DeviceLost` transition | Reset is directly invocable and produces measured `DeviceResetting` then `DeviceReset` ordering. The ABI has no deterministic loss trigger; backend/window loss is environmental. | Reset observations run; loss emits `not-run(no-deterministic-loss-route)`. No fake `DeviceLost` event is raised. | Add a test-only or supported deterministic loss/recovery trigger with defined event/state ordering. | Preserve real backend events only; do not simulate them in the facade. |
| Deterministic native keyboard/mouse/gamepad state | CNA exposes polling plus device-hotplug/reset support, but no state injection for keys, buttons, wheel, focus, packet number, dead zones, triggers, or thumbsticks. Physical state would make CI nondeterministic. | The existing 23 pure input observations remain authoritative for value semantics; no native polling claim is added. | Add a test/injection backend that can supply timestamped per-player keyboard, mouse and gamepad snapshots and focus/connect transitions without physical devices. | Document the missing hook and keep native state out of deterministic CI assertions. |
| `StorageContainer.Disposing` | ABI 0.6.0 documents `cna_storage_container_subscribe_disposing` as a synchronous exactly-once callback during `cna_storage_container_dispose`, but the pinned implementation emitted zero callbacks in the native regression. | Explicit managed `Dispose` calls native first, then raises the known sender exactly once in managed code. Handler exceptions propagate to the caller and never cross unmanaged frames. No native registration is retained. | Make the documented callback fire exactly once and add a native C API regression for explicit dispose and destroy-if-needed paths. | A managed one-shot event is safe because the wrapper exclusively owns and initiates explicit container disposal; double dispose remains silent. |
| Render-target CPU readback has no capability identity | `CNA_GRAPHICS_CAPABILITY_*` names nineteen identities and readback is not one. HEADLESS reports every one of the nineteen that this operation might plausibly be covered by, including `ThreeD` and `MultipleRenderTargets`, and still answers `NOT_SUPPORTED` with "Texture2D::GetData: this graphics renderer cannot read a render target's colour attachment back to the CPU". | Five pixel-evidence integration tests could not ask the device and failed on HEADLESS as though the binding were broken. They now *measure* the ability once per renderer and assert the refusal where it is absent. | Add a capability identity for render-target colour readback, so a caller can branch before performing an operation that is permanently unavailable rather than after. | Measure once per renderer, in one place, and assert both branches. Never catch the refusal inside a test. |
| Cube-face storage has no capability identity | Same shape: HEADLESS reports `ThreeD` and answers `NOT_SUPPORTED` with "TextureCube::SetData: this graphics renderer did not store the complete requested cube face region". `ThreeD` is not a proxy for it. | The per-face upload test failed on HEADLESS. It now measures and asserts both branches. | Add a capability identity for cube-face storage. | As above. |
| A 2D-only renderer's refusal is reported as an internal error | `IGraphicsRenderer::HandleUnsupported3DCall` throws a bare `std::runtime_error` under the default `Throw` policy, and the C API's exception barrier maps that through its `catch (const std::exception&)` arm to `CNA_RESULT_INTERNAL` / `CNA_ERROR_CATEGORY_INTERNAL`. A renderer whose own `Ensure3DSupported` throws `System::NotSupportedException` instead maps to `NOT_SUPPORTED`. | Nothing yet: no renderer available on this host lacks `ThreeD`, so the thirteen `ThreeD`-gated integration tests leave their absent branch unasserted and say why at the call site. The distinction cannot be made from the result code, only from the message text. | Throw `System::NotSupportedException` from `HandleUnsupported3DCall`, or add an arm for it, so "this renderer cannot do 3D" and "something went wrong inside CNA" are different result codes. `CNA_RESULT_NOT_SUPPORTED` already exists and is the right one. | Do not assert a result code that has not been observed. Recorded here rather than guessed at in a test. |
| SpriteBatch invalid sort modes and non-finite draw values | The current native SpriteBatch route rejects some values before/during `End`; XNA `Begin` stores an unknown sort enum and XNA draw code propagates floating-point bit patterns. | Unknown sort mode and NaN/Infinity probes produce native `CnaException` outcomes; a failed `End` deliberately leaves the managed batch begun so state is not falsely reported as ended. | Align native validation and failure timing with XNA: preserve unknown-enum state until XNA's corresponding flush/sort path, and accept the same non-finite sprite values the XNA vertex path accepts. | Report the real failure and preserve recoverable managed Begin/End state. |

## Closed in ABI 0.21.0

Three rows, all closed by upstream between `71576a7b9` and `599d14e54`, and all three verified by
running the thing the row was about rather than by matching a commit subject to a row title. That
rule is not ceremony here: the same upstream step that closed these three opened the render-target
clear row above, and the commit most likely to be mistaken for a fix for *that* one --
`fix(SAMPLE-067): preserve threaded EasyGL context handoff` -- is its most likely cause.

| Was blocked | Closed by | Proof taken against the running artifact |
| --- | --- | --- |
| `ContentManager.Load<Texture2D>` for a non-`Color` surface format | `fix(CAPI): preserve non-Color texture content formats`, which deleted the loader's Color-only guard and moved format admission to the renderer's own support query | `tools/content-survey --load` over both corpora: `CONTENT_LOAD_NATIVE_NOT_SUPPORTED` is **0** where it was 33 across the XNA sample collection and 7 across `cna-samples`. Those were normal maps and other non-`Color` surfaces; they now materialise. `cna-samples` reaches **823 of 830 loaded with 0 runtime failures**. |
| Cross-device graphics resource/state validation | `fix(CAPI): preserve active GL context across secondary devices`, which saves and restores the caller's binding around device creation | The runtime probe's `CNA_RUNTIME_PROBE_CROSS_DEVICE` gate is **deleted**, not flipped. Both `devicelifecycle.cross_device.create` and `devicelifecycle.cross_device` report `ok` by default, and -- the part that actually mattered, since one destroyed frame would have invalidated every observation after it -- the same process still emits all 105 observations afterwards. Two placeholders that stood through eleven ABI generations are measurements again. |
| A fresh cnanext configure is refused by cnanext's own audit | `test(CAPI): keep secondary-context regression audit-clean`, which removed the unclassified SDL use from `GameSecondaryGraphicsDeviceContextSmoke.c` | `tools/platform/sdl_ratchet.py --check --strict` exits 0 with "at budget (0 files, 0 references)", and -- the check that matters, since the audit runs at *configure* time -- `cmake-build-headless` reconfigured cleanly (`configure audit 'platform-ratchet': passed`) and rebuilt to ABI 0.21.0. Renderer breadth beyond the two built renderers is no longer blocked upstream. |

## Closed since ABI 0.7.0

Each of these was a row in the table above when it was measured against 0.7.0. They are recorded
rather than deleted because a blocker list that only ever grows tells a reader nothing about whether
anything is being fixed, and because the managed side of each closure is a place a regression would
be silent.

The "closed by" column is worth reading for what it does *not* say. Six rows closed because CNA
added a route. Five closed with no upstream change at all, and those five had described the ABI
where they should have described the operation: an ABI with no scatter descriptor does not make a
strided update unrepresentable, and an ABI whose transfers are tagged does not make XNA's untyped
byte copy impossible. A blocker list is only useful if entries leave it for the second reason as
well as the first.

| Was blocked | Closed by | What the binding does now |
| --- | --- | --- |
| `DynamicVertexBuffer.ContentLost` / `DynamicIndexBuffer.ContentLost` | nothing -- the row was wrong | It said "Both headers state that CNA never raises ContentLost for a buffer. Render targets got `cna_render_target_subscribe_content_lost` in 0.19.0; the buffer families did not." Both clauses are false. `cna_vertex_buffer_subscribe_content_lost` and `cna_index_buffer_subscribe_content_lost` have existed since 2026-08-15, `CNA.Interop` consumes them, and `DynamicVertexBuffer.cs`'s own doc comment already recorded that the previous "the C API has no counterpart" claim was wrong. What was genuinely unproven was **delivery**, and that is now measured: `NotifyContentLostResourcesEXT` walks every `IContentLosable` on the device and the two dynamic buffer types are two of its four implementers, so `DynamicBuffers_ContentLostFiresWhenNativeIsToldContentIsGone` observes the surviving handler firing exactly once per buffer, the removed handler not firing, and the buffer as sender. |
| `DynamicVertexBuffer.SetData(..., SetDataOptions)` on raw/custom-stride/windowed uploads | `cna_vertex_buffer_set_data_raw_with_options` and `cna_vertex_buffer_set_data_raw_at_with_options` | Forwards the option on every representable raw upload. The header records a cost-only deviation for a windowed `NoOverwrite`: the renderer receives the whole buffer, and the bytes land where XNA puts them. Only genuinely unrepresentable scatter/gather still refuses. |
| `SoundEffectInstance.Apply3D(AudioListener[], AudioEmitter)` with more than one listener | CABI-6, which made the canonical overload accept any count of one or more | Forwards the whole array through the atomic route and lets the dominant-listener rule apply. It never loops applying one listener at a time. A runtime that still refuses is surfaced as `NotSupportedException` rather than approximated. |
| `ContentManager.Load<SpriteFont>` for a compressed atlas | REMED-GFX-244, which stores DXT blocks where the driver can and decodes them where it cannot | The authored DXT3 `FontCalibri14` fixture loads, measures and draws through the ordinary content path, and is carried by the native ownership stress cycle it used to be excluded from. |
| `RenderTarget2D.ContentLost` / `RenderTargetCube.ContentLost` | `cna_render_target_subscribe_content_lost` / `_unsubscribe_content_lost` | A real subscription, taken on the first `+=` and released before the render-target handle it is registered against. Only a renderer family that can genuinely lose a device raises it; elsewhere the subscription is valid and silent, which is the renderer's answer rather than a managed stub. |
| `GraphicsDevice(GraphicsAdapter, GraphicsProfile, PresentationParameters)` | `cna_graphics_device_create` / `cna_graphics_device_destroy` | Creates a real independent device the caller owns and `Dispose` destroys, instead of adopting the running game's device and overwriting its presentation parameters -- or refusing outright when the adapter came from the static enumeration. The remaining GL-context interaction is a live row above, not a reason to keep the old behaviour. |
| A second device existing at all | the same pair | See the cross-device row above: the route exists, and what is left is a context-restoration bug rather than a missing capability. |
| `Texture2D.SetData<T>` / `GetData<T>` for any element type the tag list does not name | none -- the ABI's tagged transfers were being read as the only model | The tag now comes from the texture's own surface format, so the native side converts nothing and the bytes that cross are the surface's. `T` contributes only its size, which is what XNA's untyped byte copy does. `GetData<uint>` works; a `Vector4` read of a Color surface is still a size error, from CNA's own `cna_texture_validate_get_data_format`. |
| `GraphicsDevice.GetBackBufferData<T>`, `TextureCube` and `Texture3D` transfers for any element type but `Color` | none | Those ABI routes take a `CNA_Color*`, which fixes the transfer at RGBA8 and four bytes per texel -- it does not fix the *element type*. Any type whose size divides four now reads and writes the same four bytes. A non-Color surface is still converted by the route, which is its own long-standing behaviour. |
| `VertexBuffer.SetData<T>`/`GetData<T>` with a partial stride | none -- the ABI has no scatter descriptor, but the operation does not need one | Composed as a read-modify-write over the declaration-aligned window: read it, patch the caller's bytes at their strides, write it back. The gaps are preserved because they are read and written unchanged. The cost is a read, so a buffer the renderer refuses to read back fails through the native error rather than through a guess. |
| `IndexBuffer.GetData` from a nonzero `offsetInBytes` | `cna_index_buffer_get_data` always starts at native index zero | Reads the prefix too and keeps the tail. One temporary buffer; the destination is written only after the native call returns, so the read stays atomic. |
| `DynamicIndexBuffer.SetData(offsetInBytes, ..., SetDataOptions)` | `cna_index_buffer_set_data_at` deliberately accepts only `None` | The hint is dropped on that route rather than the call refused. It changes cost, not result: `NoOverwrite` only promises the caller will not touch what the GPU is reading, and `Discard` says the rest of the buffer may become undefined where this route preserves it -- a stronger guarantee than asked for. Refusing broke the commonest use of the overload, a batcher rewriting one slice per frame. |

## Ownership/thread-affinity note

The current C API requires game-child destruction on the game owner thread. `NativeResourceHandle`
therefore queues finalizer and cross-thread releases, and `Game` drains/retries them at owner-thread
safe points and before game destruction. This closes the exercised game lifecycle, including a
failed parent release retried after its child. An upstream process-wide owner-thread pump would be
needed if future owned native handles can legitimately outlive every managed `Game` safe point;
the binding must not destroy those handles directly from the finalizer thread.

The measured 100-cycle Debug and Release runs each queued 1,500 owner-thread releases and completed
2,900 release attempts with zero failure/retry residue or refused game destroys. The optional
1,000-cycle Release run queued 15,000 and completed 29,000 with the same zero residue. These are
lifecycle/order observations, not allocator-level leak or sanitizer evidence.

## Legal fixture inventory

No valid XGS/XSB/XWB authored-bank fixture exists in this repository, the read-only CNA tree, or
the inspected local example repositories. A valid future fixture must be legally redistributable,
produced by a compatible XNA audio authoring tool, and include matched XGS settings, XSB sound bank,
XWB wave bank, and license/provenance.

The local `cna-examples` repository does contain an MIT-licensed, procedurally generated H.264/AAC
MP4. It proves CNA's direct-file extension route but is not an XNA-compatible compiled Video XNB or
a supported Windows XNA WMV asset, so it was not mislabeled as reference-XNA fixture evidence. A
future XNA video fixture needs a legally generated supported source plus its compiled Video XNB and
documented redistribution rights.
