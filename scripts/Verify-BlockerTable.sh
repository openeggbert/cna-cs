#!/usr/bin/env bash
# Checks the native blocker table against the live headers.
#
# A blocker is a claim about upstream: "this route does not exist", "that one carries only these
# arguments". Claims rot silently -- a route gets added upstream and the row keeps saying it is
# missing, which is how a table becomes a museum. This does not re-litigate each row's prose; it
# checks the one thing that can be checked mechanically: every cna_* route a row names still exists
# in the canonical headers, and every route named as absent is still absent.
set -euo pipefail

script_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
repo_root=$(cd -- "$script_dir/.." && pwd)
upstream_root=${CNA_UPSTREAM_ROOT:-"$repo_root/../../cnanext"}
include_dir=${CNA_C_INCLUDE_DIR:-"$upstream_root/modules/c-api/include"}
doc=${1:-"$repo_root/docs/native-behavior-blockers.md"}

[ -d "$include_dir" ] || { echo "No CNA include directory at $include_dir" >&2; exit 2; }

declared=$(grep -rhoP 'CNA_C_API CNA_Result \K\w+' "$include_dir"/CNA/C/*.h | sort -u)
named=$(grep -oP '`\Kcna_[a-z0-9_]+' "$doc" | sort -u)

present=0
absent=0
absent_names=()
for route in $named; do
  if grep -qx "$route" <<<"$declared"; then
    present=$((present + 1))
  else
    absent=$((absent + 1))
    absent_names+=("$route")
  fi
done

echo "BLOCKER_ROUTES_NAMED=$(wc -w <<<"$named")"
echo "BLOCKER_ROUTES_PRESENT_UPSTREAM=$present"
echo "BLOCKER_ROUTES_ABSENT_UPSTREAM=$absent"
if [ "$absent" -gt 0 ]; then
  printf 'BLOCKER_ROUTE_ABSENT=%s\n' "${absent_names[@]}"
fi
echo "BLOCKER_TABLE_HEADER_ABI=$(grep -oP 'CNA_ABI_VERSION_MINOR UINT32_C\(\K[0-9]+' "$include_dir/CNA/C/abi.h")"
