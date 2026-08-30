#!/usr/bin/env bash
set -euo pipefail

script_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
repo_root=$(cd -- "$script_dir/.." && pwd)
dotnet_command=${DOTNET_COMMAND:-dotnet}
# cnanext is the development target. The older ../../cna checkout is a different ABI generation
# (0.7.0 at the time of writing) and must not become authoritative just because it is adjacent.
upstream_root=${CNA_UPSTREAM_ROOT:-"$repo_root/../../cnanext"}
include_dir=${CNA_C_INCLUDE_DIR:-"$upstream_root/modules/c-api/include"}
output_path=${1:-"$repo_root/artifacts/abi/$(uname -s | tr '[:upper:]' '[:lower:]')-$(uname -m).json"}

# The negative controls run as part of the gate. A verifier that only ever passes has demonstrated
# nothing, so every run proves it still rejects a wrong return type, a signedness change, a wrong
# pointer depth, a flipped by-ref direction, a swapped parameter pair and an absent import.
export CNA_ABI_SELF_TEST=${CNA_ABI_SELF_TEST:-1}

"$dotnet_command" run --project "$repo_root/tools/abi-verify/CNA.AbiVerify.csproj" -- \
  --include "$include_dir" \
  --output "$output_path"
