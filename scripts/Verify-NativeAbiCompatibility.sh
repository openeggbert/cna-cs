#!/usr/bin/env bash
set -euo pipefail

script_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
repo_root=$(cd -- "$script_dir/.." && pwd)
dotnet_command=${DOTNET_COMMAND:-dotnet}
configuration=Release
native_library=
output_root=
no_build=0

while (($# > 0)); do
  case "$1" in
    --configuration)
      configuration=${2:?--configuration requires a value}
      shift 2
      ;;
    --native-library)
      native_library=${2:?--native-library requires a path}
      shift 2
      ;;
    --output)
      output_root=${2:?--output requires a path}
      shift 2
      ;;
    --no-build)
      no_build=1
      shift
      ;;
    *)
      echo "Unknown argument: $1" >&2
      exit 2
      ;;
  esac
done

if [[ "$(uname -s)" != Linux ]]; then
  echo "The shared-library fixture gate currently requires Linux; this host is $(uname -s)." >&2
  exit 2
fi

cleanup_output=0
if [[ -z "$output_root" ]]; then
  output_root=$(mktemp -d)
  cleanup_output=1
else
  output_root=$(realpath -m "$output_root")
  mkdir -p "$output_root"
fi
trap 'if [[ "$cleanup_output" == 1 ]]; then rm -rf "$output_root"; fi' EXIT

fixtures_root="$output_root/fixtures"
logs_root="$output_root/logs"
mkdir -p "$fixtures_root" "$logs_root"

probe_project="$repo_root/tools/native-abi-probe/CNA.NativeAbiProbe.csproj"
if [[ "$no_build" == 0 ]]; then
  "$dotnet_command" build "$probe_project" -c "$configuration" -m:1
fi
probe_dll="$repo_root/tools/native-abi-probe/bin/$configuration/net8.0/CNA.NativeAbiProbe.dll"
if [[ ! -f "$probe_dll" ]]; then
  echo "Native ABI probe was not built: $probe_dll" >&2
  exit 2
fi

declared_symbols=$(perl -0777 -ne '
  while (/internal\s+static\s+(?:unsafe\s+)?partial\s+\S+\s+(cna_[a-z0-9_]+)\s*\(/g) { $symbols{$1}=1 }
  END { print join("\n", grep { $_ ne "cna_get_abi_version" &&
                                $_ ne "cna_error_get_last_message_size" &&
                                $_ ne "cna_touch_capabilities_init" &&
                                $_ ne "cna_game_destroy" } sort keys %symbols), "\n" }
' "$repo_root/src/CNA.Interop/Native.cs")
fixture_symbols=$(sed -n 's/^CNA_ABI_FIXTURE_STUB(\(cna_[a-z0-9_]*\))$/\1/p' \
  "$script_dir/abi-fixtures/required_symbols.inc")
if [[ "$declared_symbols" != "$fixture_symbols" ]]; then
  echo "The native ABI fixture symbol inventory is stale relative to CNA.Interop.Native." >&2
  exit 1
fi

compile_fixture()
{
  local name=$1
  shift
  cc -std=c11 -shared -fPIC -Wall -Wextra -Werror \
    -I "$script_dir/abi-fixtures" "$@" \
    "$script_dir/abi-fixtures/cna_abi_fixture.c" -o "$fixtures_root/$name.so"
}

run_accept()
{
  local name=$1
  if ! env -u CNA_NATIVE_DIR CNA_NATIVE_LIBRARY="$fixtures_root/$name.so" \
      "$dotnet_command" "$probe_dll" >"$logs_root/$name.log" 2>&1; then
    echo "ABI fixture '$name' was rejected but should be accepted." >&2
    sed -n '1,100p' "$logs_root/$name.log" >&2
    exit 1
  fi
  grep -Fq 'CNA_ABI_PROBE_STATUS=accepted' "$logs_root/$name.log"
}

run_reject()
{
  local name=$1
  local diagnostic=$2
  if env -u CNA_NATIVE_DIR CNA_NATIVE_LIBRARY="$fixtures_root/$name.so" \
      "$dotnet_command" "$probe_dll" >"$logs_root/$name.log" 2>&1; then
    echo "ABI fixture '$name' was accepted but should be rejected." >&2
    exit 1
  fi
  grep -Fq 'CNA_ABI_PROBE_STATUS=rejected' "$logs_root/$name.log"
  grep -Fq "$diagnostic" "$logs_root/$name.log"
}

compile_fixture exact-0.6.0 -DCNA_ABI_FIXTURE_VERSION=0x00000600U
compile_fixture additive-0.7.0 -DCNA_ABI_FIXTURE_VERSION=0x00000700U
compile_fixture additive-0.7.0-extra-symbol -DCNA_ABI_FIXTURE_VERSION=0x00000700U \
  -DCNA_ABI_FIXTURE_EXTRA_SYMBOL
compile_fixture reviewed-subset-0.8.0 -DCNA_ABI_FIXTURE_VERSION=0x00000800U
compile_fixture missing-required-symbol -DCNA_ABI_FIXTURE_VERSION=0x00000600U \
  -DCNA_ABI_FIXTURE_MISSING_REQUIRED_SYMBOL
compile_fixture changed-required-signature -DCNA_ABI_FIXTURE_VERSION=0x00000600U \
  -DCNA_ABI_FIXTURE_CHANGED_SIGNATURE
compile_fixture incompatible-major-1.0.0 -DCNA_ABI_FIXTURE_VERSION=0x00010000U
compile_fixture structurally-incompatible-0.7.0 -DCNA_ABI_FIXTURE_VERSION=0x00000700U \
  -DCNA_ABI_FIXTURE_INCOMPATIBLE_STRUCT
compile_fixture malformed-metadata-0.0.0 -DCNA_ABI_FIXTURE_VERSION=0x00000000U
compile_fixture unreadable-metadata -DCNA_ABI_FIXTURE_UNREADABLE_METADATA

run_accept exact-0.6.0
run_accept additive-0.7.0
run_accept additive-0.7.0-extra-symbol
run_accept reviewed-subset-0.8.0
run_reject missing-required-symbol "required symbol 'cna_game_destroy' is missing"
run_reject changed-required-signature "failed required signature/shape probe 'cna_error_get_last_message_size'"
run_reject incompatible-major-1.0.0 'major 1 differs from consumer major 0'
run_reject structurally-incompatible-0.7.0 "failed required signature/shape probe 'cna_touch_capabilities_init'"
run_reject malformed-metadata-0.0.0 'metadata encodes 0.0.0'
run_reject unreadable-metadata "required symbol 'cna_get_abi_version' is missing"

selected_native_status=not-run
if [[ -n "$native_library" ]]; then
  native_library=$(realpath "$native_library")
  if ! env -u CNA_NATIVE_DIR CNA_NATIVE_LIBRARY="$native_library" \
      "$dotnet_command" "$probe_dll" >"$logs_root/selected-native.log" 2>&1; then
    echo "The selected CNA native library failed the ABI contract." >&2
    sed -n '1,100p' "$logs_root/selected-native.log" >&2
    exit 1
  fi
  selected_native_status=passed
fi

jq -n \
  --arg selectedNative "$selected_native_status" \
  '{
    schemaVersion: 1,
    policyVersion: "cna-cs-native-abi/1",
    status: "passed",
    consumerAbi: "0.6.0",
    requiredSymbolCount: 841,
    accepted: ["exact-0.6.0", "additive-0.7.0", "additive-0.7.0-extra-symbol", "reviewed-subset-0.8.0"],
    rejected: ["missing-required-symbol", "changed-required-signature", "incompatible-major-1.0.0", "structurally-incompatible-0.7.0", "malformed-metadata-0.0.0", "unreadable-metadata"],
    selectedNative: $selectedNative
  }' >"$output_root/abi-compatibility-report.json"

echo "CNA_ABI_POLICY=cna-cs-native-abi/1"
echo "CNA_ABI_REQUIRED_SYMBOLS=841"
echo "CNA_ABI_FIXTURES_ACCEPTED=4"
echo "CNA_ABI_FIXTURES_REJECTED=6"
echo "CNA_ABI_SELECTED_NATIVE=$selected_native_status"
echo "CNA_ABI_COMPATIBILITY_STATUS=passed"
