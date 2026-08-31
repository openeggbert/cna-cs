#!/usr/bin/env bash
#
# Proves that a behavior probe's snapshot is a statement about the binding rather than about the
# CNA renderer linked underneath it.
#
# The behavior corpus exists to be compared, observation for observation, against a capture taken
# on Windows XNA. That comparison is only meaningful if this side of it is fixed: if the graphics
# probe answered `graphics.blend.state=...` differently on Vulkan than on OpenGL, a difference
# against XNA would say nothing about whether the binding is faithful. So the property is not
# "the snapshots are similar" -- it is that they are byte-identical.
#
# Measured when this script was written: the graphics probe's 166 observations are identical under
# OPENGLES3, OPENGL33 and VULKAN, names and values both. Two further renderers cannot be captured
# at all -- SOFTWARE has no volume texture storage and SDL_RENDERER has no vertex buffers -- and
# that is the documented precondition in tests/behavior-corpus-counts.json rather than a failure:
# a probe must not skip a route on a weak renderer, because skipping is exactly what would make
# the observation set renderer-dependent.
#
# What the gate can and cannot see: it compares captured values, so it catches an observation that
# leaks the renderer's identity into the corpus -- proved by planting one and watching the run go
# from IDENTICAL to DIFFERENT with the offending line printed. Two earlier attempts to plant a
# renderer-dependent value did *not* register, and that is itself the measurement: neither
# `GraphicsAdapter.DefaultAdapter.Description` (a constant "Default Display") nor the shared parent
# directory of the two libraries actually varies. Nothing the corpus currently observes through the
# XNA surface distinguishes the GL backends from Vulkan.
#
# Usage:
#   scripts/Verify-CorpusRendererInvariance.sh --probe graphics \
#     --library /path/to/a/libcna_c_api.so --library /path/to/another/libcna_c_api.so [...]
#
# Exits non-zero if any two capturable renderers disagree. A library the probe refuses is
# reported as REFUSED and does not fail the run; fewer than two capturable libraries does.

set -euo pipefail

script_directory="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repository_root="$(cd "$script_directory/.." && pwd)"
dotnet_command="${DOTNET:-dotnet}"

probe_id=graphics
libraries=()

while [[ $# -gt 0 ]]; do
  case "$1" in
    --probe) probe_id="$2"; shift 2 ;;
    --library) libraries+=("$2"); shift 2 ;;
    -h|--help) sed -n '2,26p' "${BASH_SOURCE[0]}"; exit 0 ;;
    *) echo "Unknown argument: $1" >&2; exit 2 ;;
  esac
done

if [[ ${#libraries[@]} -lt 2 ]]; then
  echo "Give at least two --library paths; invariance needs something to compare." >&2
  exit 2
fi

cd "$repository_root"

behavior_tool=tools/behavior-corpus
project="$("$dotnet_command" run --project "$behavior_tool" -c Release -- \
  get --probe "$probe_id" --field sourceProject)"

"$dotnet_command" build "$project" -c Release -m:1 >/dev/null

work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT

runner=()
if [[ -z "${DISPLAY:-}" ]] && command -v xvfb-run >/dev/null 2>&1; then
  runner=(xvfb-run -a)
fi

captured=()
refused=()
for library in "${libraries[@]}"; do
  name="$(basename "$(dirname "$(dirname "$(dirname "$library")")")")"
  snapshot="$work/$name.txt"
  if CNA_NATIVE_LIBRARY="$library" \
     XNA_GRAPHICS_PROBE_DESTRUCTIVE_LIFECYCLE=1 \
     XNA_GRAPHICS_PROBE_UNSAFE_CONSTRUCTORS=1 \
     SDL_AUDIODRIVER="${SDL_AUDIODRIVER:-dummy}" \
     "${runner[@]}" "$dotnet_command" run --project "$project" -c Release --no-build -- \
       --output "$snapshot" >/dev/null 2>&1; then
    captured+=("$name=$snapshot")
    echo "CAPTURED $name ($(wc -l < "$snapshot") observations)"
  else
    refused+=("$name")
    echo "REFUSED  $name (the probe needs a capability this renderer lacks)"
  fi
done

if [[ ${#captured[@]} -lt 2 ]]; then
  echo "Only ${#captured[@]} renderer(s) could be captured; invariance is unproven." >&2
  echo "CORPUS_RENDERER_INVARIANCE_STATUS=not_run"
  exit 1
fi

reference_name="${captured[0]%%=*}"
reference_file="${captured[0]#*=}"
status=passed
for entry in "${captured[@]:1}"; do
  name="${entry%%=*}"

  file="${entry#*=}"
  if diff -u "$reference_file" "$file" >"$work/$name.diff"; then
    echo "IDENTICAL $reference_name vs $name"
  else
    status=failed
    echo "DIFFERENT $reference_name vs $name:" >&2
    head -n 40 "$work/$name.diff" >&2
  fi
done

echo "CORPUS_RENDERER_INVARIANCE_PROBE=$probe_id"
echo "CORPUS_RENDERER_INVARIANCE_CAPTURED=${#captured[@]}"
echo "CORPUS_RENDERER_INVARIANCE_REFUSED=${refused[*]:-none}"
echo "CORPUS_RENDERER_INVARIANCE_STATUS=$status"
[ "$status" = passed ]
