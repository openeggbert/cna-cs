#!/usr/bin/env bash
set -euo pipefail

script_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
repo_root=$(cd -- "$script_dir/.." && pwd)
dotnet_command=${DOTNET_COMMAND:-dotnet}
reference_dir=${XNA_REFERENCE_DIR:-}

if [[ -z "$reference_dir" || ! -d "$reference_dir" ]]; then
  echo "Set XNA_REFERENCE_DIR to a legally supplied local XNA 4.0 reference-assembly directory." >&2
  exit 2
fi

"$dotnet_command" run --project "$repo_root/tools/profile-inventory/CNA.ProfileInventory.csproj" -- \
  --manifest "$repo_root/tools/profile-inventory/profiles.json" \
  --reference-dir "$reference_dir" \
  --json "$repo_root/docs/generated/xna-profile-inventory.json" \
  --markdown "$repo_root/docs/generated/xna-profile-inventory.md"
