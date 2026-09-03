#!/usr/bin/env bash
set -euo pipefail

# Builds EngineBench against each requested XNA implementation and runs the same workloads through
# all of them. See tools/engine-bench/README.md -- in particular its "Reading the result honestly"
# section, which this script's own output header exists to serve.

script_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
repo_root=$(cd -- "$script_dir/.." && pwd)
dotnet_command=${DOTNET_COMMAND:-dotnet}
project="$repo_root/tools/engine-bench/CNA.EngineBench.csproj"

engines=${ENGINES:-"CNA MonoGame Kni"}
modes=${MODES:-"loop tris sprites spritesplit"}
frames=${FRAMES:-300}
repeats=${REPEATS:-3}
out_root=${OUT_ROOT:-"$repo_root/artifacts/engine-bench"}

# A CNA run needs a C API library, and which one decides what the numbers mean. Left to the caller
# rather than guessed: an adjacent build tree is not evidence that it is the intended one.
if [[ " $engines " == *" CNA "* && -z "${CNA_NATIVE_LIBRARY:-}" && -z "${CNA_NATIVE_DIR:-}" ]]; then
  echo "Set CNA_NATIVE_LIBRARY (or CNA_NATIVE_DIR) to the CNA C API library to benchmark." >&2
  exit 2
fi

# Xvfb only when there is no display. A headless X server is not a headless renderer -- CNA may
# still select HEADLESS underneath it, which the run banner reports and the README explains.
run_prefix=()
if [[ -z "${DISPLAY:-}" ]] && command -v xvfb-run >/dev/null 2>&1; then
  run_prefix=(xvfb-run -a -s "-screen 0 1024x768x24")
fi

if [[ -n "${CNA_NATIVE_LIBRARY:-}" ]]; then
  echo "native library: $CNA_NATIVE_LIBRARY"
fi
echo "frames=$frames repeats=$repeats"
echo

for engine in $engines; do
  out="$out_root/$engine"
  "$dotnet_command" publish "$project" -c Release -p:Engine="$engine" -o "$out" --nologo -v q

  # The renderer CNA chose is a property of the library, not of the workload, so it is reported
  # once per engine rather than buried in each run's noise.
  if [[ "$engine" == "CNA" ]]; then
    renderer=$("${run_prefix[@]}" "$out/EngineBench" loop 1 2>&1 | grep -oE 'graphics renderer: [A-Z_]+' || true)
    echo "$engine ${renderer:-(renderer not reported)}"
  else
    echo "$engine"
  fi

  for mode in $modes; do
    printf '  %-12s' "$mode"
    for _ in $(seq 1 "$repeats"); do
      "${run_prefix[@]}" "$out/EngineBench" "$mode" "$frames" 2>/dev/null \
        | grep -oE '(fps|drawUsPerFrame|endUsPerFrame|bytesPerFrame)=[0-9.]+' \
        | paste -sd' ' -
    done
  done
  echo
done
