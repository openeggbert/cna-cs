#!/usr/bin/env bash
set -euo pipefail

script_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
repo_root=$(cd -- "$script_dir/.." && pwd)
dotnet_command=${DOTNET_COMMAND:-dotnet}
upstream_root=${CNA_UPSTREAM_ROOT:-"$repo_root/../../cna"}
include_dir=${CNA_C_INCLUDE_DIR:-"$upstream_root/modules/c-api/include"}
output_path=${1:-"$repo_root/artifacts/abi/$(uname -s | tr '[:upper:]' '[:lower:]')-$(uname -m).json"}

"$dotnet_command" run --project "$repo_root/tools/abi-verify/CNA.AbiVerify.csproj" -- \
  --include "$include_dir" \
  --output "$output_path"
