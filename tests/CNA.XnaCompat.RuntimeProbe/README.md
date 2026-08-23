# XNA audio/media/storage runtime behavior probe

This same-source executable records 104 native/device observations in six groups: Audio (36), XACT
(7), Media (20), Video (17), Storage (20), and DeviceLifecycle (4). It uses generated silence and
an isolated storage container. No Microsoft or other proprietary assets are included.

Authored XACT banks, song event ordering, video frame identity, real device loss, and cross-device
validation remain explicit `not-run(...)` observations because the repository has no legal fixture
or the CNA C ABI has no deterministic route. Those lines are evidence of the boundary, not claimed
coverage.

Run CNA on Linux with a compatible native library and a writable isolated data root:

```bash
CNA_NATIVE_LIBRARY=/path/to/libcna_c_api.so \
SDL_AUDIODRIVER=dummy \
XDG_DATA_HOME=/tmp/cna-runtime-probe-data \
dotnet run -c Release --project tests/CNA.XnaCompat.RuntimeProbe
```

The probe deletes its named `cna-cs-runtime-probe` storage container in `finally`. The Windows XNA
build and capture are integrated into `scripts/Capture-XnaSnapshots.ps1`; reference/runtime
assemblies remain external to this repository.
