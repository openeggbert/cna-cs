#!/usr/bin/env bash
#
# Reproduce-or-disprove the SIGTERM "pure virtual method called" shutdown report.
#
# The report sat in plan.md as "remains open until reproduced or disproved on the current native
# build" for long enough that it was worth settling. It reproduces, and the renderer matrix is the
# part worth having: this script exists so the next person re-measures rather than re-reads.
#
#   scripts/Reproduce-SigtermShutdown.sh software
#
# Needs the template built at ../cna-cs-template/bin/Debug/net8.0/CnaCsTemplate and a CNA build at
# ../../cnanext/cmake-build-<renderer>. Prints PURE_VIRTUAL_HITS and ABORT_HITS; anything above zero
# is the defect. A normal exit is clean on every renderer, so run the template's --smoke-test first
# if a result looks like something other than the signal path.
set -u
renderer=${1:?usage: Reproduce-SigtermShutdown.sh <renderer-build-suffix>}
log=${SIGTERM_LOG:-$(mktemp -t "cna-sigterm-$renderer-XXXXXX.log")}
echo "LOG $renderer=$log"
script_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
repo_root=$(cd -- "$script_dir/.." && pwd)
upstream=${CNA_UPSTREAM_ROOT:-"$repo_root/../../cnanext"}
template=${CNA_TEMPLATE_ROOT:-"$repo_root/../cna-cs-template"}
export CNA_NATIVE_LIBRARY="$upstream/cmake-build-$renderer/modules/c-api/libcna_c_api.so"
cd "$template"
xvfb-run -a ./bin/Debug/net8.0/CnaCsTemplate --frames 100000 > "$log" 2>&1 &
runner=$!
sleep 10
# The game is a descendant of xvfb-run; find it by its own executable name only.
game=$(pgrep -x CnaCsTemplate | head -1)
if [ -z "$game" ]; then echo "RESULT $renderer: game process not found"; kill -KILL $runner 2>/dev/null; exit 0; fi
kill -TERM "$game"
for _ in $(seq 1 40); do kill -0 "$game" 2>/dev/null || break; sleep 0.25; done
if kill -0 "$game" 2>/dev/null; then
  echo "RESULT $renderer: still running 10s after SIGTERM; killing"
  kill -KILL "$game" 2>/dev/null
else
  echo "RESULT $renderer: exited on SIGTERM"
fi
wait $runner 2>/dev/null
echo "RUNNER_EXIT $renderer=$?"
grep -icE "pure virtual method called" "$log" | sed "s/^/PURE_VIRTUAL_HITS $renderer=/"
grep -icE "terminate called|Aborted|Segmentation fault" "$log" | sed "s/^/ABORT_HITS $renderer=/"
tail -3 "$log" | sed "s/^/TAIL $renderer| /" | cut -c1-160
