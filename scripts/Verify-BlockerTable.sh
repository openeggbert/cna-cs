#!/usr/bin/env bash
# Checks the native blocker table against the live headers.
#
# A blocker is a claim about upstream: "this route does not exist", "that one carries only these
# arguments". Claims rot silently -- a route gets added upstream and the row keeps saying it is
# missing, which is how a table becomes a museum. This does not re-litigate each row's prose; it
# checks the two things that can be checked mechanically: every cna_* route a table row names still exists
# in the canonical headers, and the generation the document says it was measured against is the
# generation of the headers it is being checked against.
#
# Both are enforced, which they were not. This printed BLOCKER_ROUTE_ABSENT and then exited 0, so a
# row naming a route that exists in nobody's header passed the gate -- proven, not theorised: the
# render-target clear row was first written naming `cna_graphics_device_clear`, which does not
# exist (the three real routes are `_clear_rgba`, `_clear_color_depth` and `_clear_options`), and
# this script reported it and passed. It printed the header's ABI minor beside a document that
# names its own measured ABI in prose, and compared neither, so a table left at 0.20.0 while the
# headers moved to 0.21.0 was also a green run.
set -euo pipefail

script_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
repo_root=$(cd -- "$script_dir/.." && pwd)
upstream_root=${CNA_UPSTREAM_ROOT:-"$repo_root/../../cnanext"}
include_dir=${CNA_C_INCLUDE_DIR:-"$upstream_root/modules/c-api/include"}
doc=${1:-"$repo_root/docs/native-behavior-blockers.md"}

[ -d "$include_dir" ] || { echo "No CNA include directory at $include_dir" >&2; exit 2; }

declared=$(grep -rhoP 'CNA_C_API CNA_Result \K\w+' "$include_dir"/CNA/C/*.h | sort -u)

# Table rows only, which is what this check has always claimed to be about: a row is a claim about
# upstream, and prose is not. The distinction became load-bearing the moment the document started
# explaining a mistake -- the paragraph recording that a row once named a route which does not exist
# has to be able to write that route's name without the check reading it as a fresh claim.
named=$(grep '^|' "$doc" | grep -oP '`\Kcna_[a-z0-9_]+' | sort -u)

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
header_abi_minor=$(grep -oP 'CNA_ABI_VERSION_MINOR UINT32_C\(\K[0-9]+' "$include_dir/CNA/C/abi.h")
header_abi_major=$(grep -oP 'CNA_ABI_VERSION_MAJOR UINT32_C\(\K[0-9]+' "$include_dir/CNA/C/abi.h")
header_abi_patch=$(grep -oP 'CNA_ABI_VERSION_PATCH UINT32_C\(\K[0-9]+' "$include_dir/CNA/C/abi.h")
header_abi="$header_abi_major.$header_abi_minor.$header_abi_patch"
echo "BLOCKER_TABLE_HEADER_ABI=$header_abi_minor"

# The document states the generation it was measured against, in its own opening sentence. Reading
# it back is what turns "these routes exist" into "these routes exist in the tree this table claims
# to describe" -- without it the check is happy to validate today's headers against last month's
# prose.
doc_abi=$(grep -oP 'C ABI \K[0-9]+\.[0-9]+\.[0-9]+' "$doc" | head -n 1)
echo "BLOCKER_TABLE_DOCUMENT_ABI=${doc_abi:-none}"

status=passed
if [ "$absent" -gt 0 ]; then
  echo "The blocker table names $absent route(s) that no canonical header declares." >&2
  status=failed
fi
if [ -z "$doc_abi" ]; then
  echo "The blocker table does not state the C ABI generation it was measured against." >&2
  status=failed
elif [ "$doc_abi" != "$header_abi" ]; then
  echo "The blocker table says it was measured against C ABI $doc_abi; these headers are $header_abi." >&2
  status=failed
fi

echo "BLOCKER_TABLE_STATUS=$status"
[ "$status" = passed ]
