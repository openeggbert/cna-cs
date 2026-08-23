#!/usr/bin/env bash
set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
dotnet_command="${DOTNET_COMMAND:-dotnet}"
output_directory="$repository_root/artifacts/cna-snapshots"
force=0
pure_only=0

while (($#)); do
  case "$1" in
    --output)
      output_directory="$(realpath -m "$2")"
      shift 2
      ;;
    --force)
      force=1
      shift
      ;;
    --pure-only)
      pure_only=1
      shift
      ;;
    --help|-h)
      echo "Usage: $0 [--output directory] [--force] [--pure-only]"
      echo "Full capture requires CNA_NATIVE_LIBRARY or CNA_NATIVE_DIR and a usable display."
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      exit 2
      ;;
  esac
done

if [[ -e "$output_directory" ]]; then
  if [[ "$force" != 1 ]]; then
    echo "Output directory already exists; pass --force to replace it: $output_directory" >&2
    exit 2
  fi
  case "$output_directory" in
    /|"$repository_root"|"$(dirname "$repository_root")")
      echo "Refusing unsafe output directory: $output_directory" >&2
      exit 2
      ;;
  esac
  rm -rf -- "$output_directory"
fi

capture_root="$(mktemp -d)"
trap 'rm -rf -- "$capture_root"' EXIT
mkdir -p "$capture_root/output"

export DOTNET_CLI_HOME="$capture_root/dotnet-home"
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1

behavior_tool="$repository_root/tools/behavior-corpus/CNA.BehaviorCorpus.csproj"

manifest_value() {
  "$dotnet_command" run --project "$behavior_tool" -c Release --no-build -- get "$@"
}

run_probe() {
  local probe_id="$1"
  local project="$2"
  local snapshot_file="$3"
  local raw_file="$capture_root/$probe_id.raw.txt"

  "$dotnet_command" run --project "$project" -c Release --no-build >"$raw_file"
  "$dotnet_command" run --project "$behavior_tool" -c Release --no-build -- \
    validate --probe "$probe_id" --input "$raw_file" \
    --output "$capture_root/output/$snapshot_file"
}

cd "$repository_root"
"$dotnet_command" build "$behavior_tool" -c Release -m:1
compile_project="$(manifest_value --probe compile --field sourceProject)"
compile_snapshot="$(manifest_value --probe compile --field expectedSnapshotFilename)"
graphics_project="$(manifest_value --probe graphics --field sourceProject)"
graphics_snapshot="$(manifest_value --probe graphics --field expectedSnapshotFilename)"
runtime_project="$(manifest_value --probe runtime --field sourceProject)"
runtime_snapshot="$(manifest_value --probe runtime --field expectedSnapshotFilename)"
combined_snapshot="$(manifest_value --field candidateCombinedSnapshotFilename)"

"$dotnet_command" build "$compile_project" -c Release -m:1
run_probe compile \
  "$compile_project" \
  "$compile_snapshot"

if [[ "$pure_only" == 0 ]]; then
  if [[ -z "${CNA_NATIVE_LIBRARY:-}" && -z "${CNA_NATIVE_DIR:-}" ]]; then
    echo "Set CNA_NATIVE_LIBRARY or CNA_NATIVE_DIR for a full CNA snapshot." >&2
    exit 2
  fi

  "$dotnet_command" build "$graphics_project" -c Release -m:1
  "$dotnet_command" build "$runtime_project" -c Release -m:1
  export XNA_GRAPHICS_PROBE_DRAW_VALIDATION=1
  export XNA_GRAPHICS_PROBE_DESTRUCTIVE_LIFECYCLE=1
  export XNA_GRAPHICS_PROBE_UNSAFE_CONSTRUCTORS=1
  export SDL_AUDIODRIVER="${SDL_AUDIODRIVER:-dummy}"
  export XDG_DATA_HOME="$capture_root/xdg-data"

  runner=()
  if [[ -z "${DISPLAY:-}" ]] && command -v xvfb-run >/dev/null 2>&1; then
    runner=(xvfb-run -a)
  fi

  "${runner[@]}" "$dotnet_command" run \
    --project "$graphics_project" \
    -c Release --no-build >"$capture_root/graphics.raw.txt"
  "$dotnet_command" run --project "$behavior_tool" -c Release --no-build -- \
    validate --probe graphics --input "$capture_root/graphics.raw.txt" \
    --output "$capture_root/output/$graphics_snapshot"

  "${runner[@]}" "$dotnet_command" run \
    --project "$runtime_project" \
    -c Release --no-build >"$capture_root/runtime.raw.txt"
  "$dotnet_command" run --project "$behavior_tool" -c Release --no-build -- \
    validate --probe runtime --input "$capture_root/runtime.raw.txt" \
    --output "$capture_root/output/$runtime_snapshot"

  "$dotnet_command" run --project "$behavior_tool" -c Release --no-build -- combine \
    --input "compile=$capture_root/output/$compile_snapshot" \
    --input "graphics=$capture_root/output/$graphics_snapshot" \
    --input "runtime=$capture_root/output/$runtime_snapshot" \
    --output "$capture_root/output/$combined_snapshot"
fi

mkdir -p "$(dirname "$output_directory")"
mv "$capture_root/output" "$output_directory"
echo "CNA snapshot captured in $output_directory"
