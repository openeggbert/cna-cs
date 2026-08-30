#!/usr/bin/env bash
set -euo pipefail

script_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
repo_root=$(cd -- "$script_dir/.." && pwd)
template_root=${CNA_TEMPLATE_ROOT:-"$repo_root/../cna-cs-template"}
dotnet_command=${DOTNET_COMMAND:-dotnet}
native_library=${CNA_ACCEPTANCE_NATIVE_LIBRARY:-}
package_version=${CNA_PACKAGE_VERSION:-0.1.0-local.1}
output_root=

while (($# > 0)); do
  case "$1" in
    --native-library)
      native_library=${2:?--native-library requires a path}
      shift 2
      ;;
    --package-version)
      package_version=${2:?--package-version requires a value}
      shift 2
      ;;
    --output)
      output_root=${2:?--output requires a path}
      shift 2
      ;;
    *)
      echo "Unknown argument: $1" >&2
      exit 2
      ;;
  esac
done

if [[ "$(uname -s)" != Linux || "$(uname -m)" != x86_64 ]]; then
  echo "This native-package experiment is evidence-scoped to linux-x64; this host is $(uname -s)/$(uname -m)." >&2
  exit 2
fi
if [[ -z "$native_library" || ! -f "$native_library" ]]; then
  echo "Pass --native-library with an explicitly selected ABI-matched linux-x64 CNA C API build." >&2
  exit 2
fi
if [[ ! -f "$template_root/.template.config/template.json" ]]; then
  echo "CNA template checkout was not found at: $template_root" >&2
  exit 2
fi

cleanup_output=0
if [[ -z "$output_root" ]]; then
  output_root=$(mktemp -d)
  cleanup_output=1
else
  output_root=$(realpath -m "$output_root")
  if [[ -e "$output_root" ]]; then
    echo "Acceptance output already exists: $output_root" >&2
    exit 2
  fi
  mkdir -p "$output_root"
fi

work_root="$output_root/work"
feed_root="$output_root/feed"
logs_root="$output_root/logs"
mkdir -p "$work_root" "$feed_root" "$logs_root"
trap 'if [[ "$cleanup_output" == 1 ]]; then rm -rf "$output_root"; fi' EXIT

export DOTNET_CLI_HOME="$work_root/dotnet-home"
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export NUGET_AUDIT_MODE=direct

"$dotnet_command" clean "$repo_root/CNA.sln" -c Release -m:1 -v:quiet
"$dotnet_command" build "$repo_root/CNA.sln" -c Release --no-restore -m:1

abi_compatibility_root="$work_root/abi-compatibility"
DOTNET_COMMAND="$dotnet_command" "$script_dir/Verify-NativeAbiCompatibility.sh" \
  --configuration Release --native-library "$native_library" --output "$abi_compatibility_root"

pack_project()
{
  local project=$1
  shift
  "$dotnet_command" pack "$repo_root/$project" -c Release --no-restore -m:1 \
    -p:CnaPackageAcceptance=true -p:CnaPackageVersion="$package_version" \
    -o "$feed_root" "$@"
}

pack_project src/CNA.Interop/CNA.Interop.csproj \
  -p:CnaNativeRid=linux-x64 -p:CnaNativeLibrary="$(realpath "$native_library")"
pack_project src/CNA.Framework/CNA.Framework.csproj
pack_project src/CNA.XnaCompat/CNA.XnaCompat.csproj

for package_id in CNA.Interop CNA.Framework CNA.XnaCompat; do
  package="$feed_root/$package_id.$package_version.nupkg"
  symbols="$feed_root/$package_id.$package_version.snupkg"
  [[ -f "$package" && -f "$symbols" ]] || { echo "Missing package output for $package_id." >&2; exit 1; }
  entries=$(unzip -Z1 "$package")
  for required in "lib/net8.0/$package_id.dll" "lib/net8.0/$package_id.xml" LICENSE NOTICE.md README.md; do
    if ! grep -Fxq "$required" <<<"$entries"; then
      echo "$package_id package is missing $required." >&2
      exit 1
    fi
  done
  if ! unzip -Z1 "$symbols" | grep -Fxq "lib/net8.0/$package_id.pdb"; then
    echo "$package_id symbol package is missing its portable PDB." >&2
    exit 1
  fi
done

interop_entries=$(unzip -Z1 "$feed_root/CNA.Interop.$package_version.nupkg")
if ! grep -Fxq 'runtimes/linux-x64/native/libcna_c_api.so' <<<"$interop_entries"; then
  echo "CNA.Interop package is missing the selected linux-x64 native asset." >&2
  exit 1
fi

DOTNET_COMMAND="$dotnet_command" CNA_CS_ROOT="$repo_root" \
  "$template_root/scripts/verify-template.sh" --mode development
DOTNET_COMMAND="$dotnet_command" CNA_CS_ROOT="$repo_root" \
  "$template_root/scripts/verify-template.sh" --mode package \
    --package-feed "$feed_root" --package-version "$package_version"

consumer_root="$work_root/IsolatedConsumer"
"$dotnet_command" new install "$template_root"
"$dotnet_command" new cna-game --name IsolatedConsumer --output "$consumer_root" \
  --consumerMode Package --cnaPackageVersion "$package_version"
"$dotnet_command" new nugetconfig --output "$work_root"
config_file="$work_root/nuget.config"
"$dotnet_command" nuget remove source nuget --configfile "$config_file"
"$dotnet_command" nuget add source "$feed_root" --name cna-local --configfile "$config_file"
"$dotnet_command" restore "$consumer_root/IsolatedConsumer.csproj" --configfile "$config_file" \
  --packages "$work_root/packages"
"$dotnet_command" build "$consumer_root/IsolatedConsumer.csproj" -c Release --no-restore -m:1

if rg -n 'CnaCsRoot|CNA_CS_ROOT|ProjectReference' "$consumer_root/IsolatedConsumer.csproj"; then
  echo "Isolated package consumer contains source-reference configuration." >&2
  exit 1
fi
if rg -n -F "$repo_root" "$consumer_root" -g '*.csproj' -g 'project.assets.json'; then
  echo "Isolated package consumer contains a CNA.NET source checkout path." >&2
  exit 1
fi

consumer_output="$consumer_root/bin/Release/net8.0"
consumer_dll="$consumer_output/IsolatedConsumer.dll"
packaged_native="$consumer_output/runtimes/linux-x64/native/libcna_c_api.so"
[[ -f "$consumer_dll" && -f "$packaged_native" ]] || {
  echo "The isolated consumer output is missing its managed or packaged native runtime." >&2
  exit 1
}

runtime_dir="$work_root/runtime"
mkdir -p "$runtime_dir"
chmod 700 "$runtime_dir"

run_consumer()
{
  local log=$1
  shift
  env -u CNA_NATIVE_LIBRARY -u CNA_NATIVE_DIR \
    XDG_RUNTIME_DIR="$runtime_dir" SDL_AUDIODRIVER=dummy SDL_VIDEODRIVER=offscreen \
    "$dotnet_command" "$consumer_dll" "$@" >"$log" 2>&1
}

run_consumer "$logs_root/frames-60.log" --frames 60
run_consumer "$logs_root/frames-600.log" --frames 600
grep -Fq 'drew 60 frames' "$logs_root/frames-60.log"
grep -Fq 'drew 600 frames' "$logs_root/frames-600.log"

mv "$packaged_native" "$work_root/libcna_c_api.saved.so"
if run_consumer "$logs_root/missing-native.log" --frames 1; then
  echo "Missing-native diagnostic case unexpectedly succeeded." >&2
  exit 1
fi
mv "$work_root/libcna_c_api.saved.so" "$packaged_native"
grep -Fq 'No CNA C API native library was found' "$logs_root/missing-native.log"
grep -Fq 'Platform/RID:' "$logs_root/missing-native.log"

cc -m32 -shared -nostdlib -fPIC -Wall -Wextra -Werror \
  "$script_dir/package-fixtures/wrong_architecture.c" -o "$work_root/wrong-architecture.so"
if CNA_NATIVE_LIBRARY="$work_root/wrong-architecture.so" \
   "$dotnet_command" "$consumer_dll" --frames 1 >"$logs_root/wrong-architecture.log" 2>&1; then
  echo "Wrong-architecture diagnostic case unexpectedly succeeded." >&2
  exit 1
fi
grep -Fq 'wrong architecture or binary format' "$logs_root/wrong-architecture.log"
grep -Fq 'Platform/RID:' "$logs_root/wrong-architecture.log"

cc -shared -fPIC -Wall -Wextra -Werror "$script_dir/package-fixtures/wrong_abi.c" \
  -o "$work_root/wrong-abi.so"
if CNA_NATIVE_LIBRARY="$work_root/wrong-abi.so" CNA_NATIVE_DIR=/deliberately/ignored \
   XDG_RUNTIME_DIR="$runtime_dir" SDL_AUDIODRIVER=dummy SDL_VIDEODRIVER=offscreen \
   "$dotnet_command" "$consumer_dll" --frames 1 >"$logs_root/wrong-abi.log" 2>&1; then
  echo "Wrong-ABI diagnostic case unexpectedly succeeded." >&2
  exit 1
fi
grep -Fq 'implements C ABI 1.0.0' "$logs_root/wrong-abi.log"
grep -Fq 'consumer ABI 0.20.0' "$logs_root/wrong-abi.log"
grep -Fq 'explicit CNA_NATIVE_LIBRARY' "$logs_root/wrong-abi.log"

if CNA_NATIVE_LIBRARY="$abi_compatibility_root/fixtures/missing-required-symbol.so" \
   "$dotnet_command" "$consumer_dll" --frames 1 >"$logs_root/missing-symbol.log" 2>&1; then
  echo "Missing-symbol diagnostic case unexpectedly succeeded." >&2
  exit 1
fi
grep -Fq "required symbol 'cna_game_destroy' is missing" "$logs_root/missing-symbol.log"

if CNA_NATIVE_LIBRARY="$work_root/does-not-exist.so" \
   "$dotnet_command" "$consumer_dll" --frames 1 >"$logs_root/invalid-explicit-path.log" 2>&1; then
  echo "Invalid explicit-path diagnostic case unexpectedly succeeded." >&2
  exit 1
fi
grep -Fq 'CNA_NATIVE_LIBRARY selected' "$logs_root/invalid-explicit-path.log"
grep -Fq 'no fallback is attempted' "$logs_root/invalid-explicit-path.log"

conflict_dir="$work_root/conflict"
mkdir -p "$conflict_dir"
cp "$packaged_native" "$conflict_dir/libcna_c_api.so"
cp "$packaged_native" "$conflict_dir/libcna-native.so"
if env -u CNA_NATIVE_LIBRARY CNA_NATIVE_DIR="$conflict_dir" \
   "$dotnet_command" "$consumer_dll" --frames 1 >"$logs_root/conflict.log" 2>&1; then
  echo "Conflicting-library diagnostic case unexpectedly succeeded." >&2
  exit 1
fi
grep -Fq 'Conflicting CNA native libraries were found' "$logs_root/conflict.log"

valid_override="$work_root/selected-explicit-native.so"
cp "$packaged_native" "$valid_override"
CNA_NATIVE_LIBRARY="$valid_override" CNA_NATIVE_DIR=/deliberately/ignored \
  XDG_RUNTIME_DIR="$runtime_dir" SDL_AUDIODRIVER=dummy SDL_VIDEODRIVER=offscreen \
  "$dotnet_command" "$consumer_dll" --frames 60 >"$logs_root/explicit-override.log" 2>&1
grep -Fq 'drew 60 frames' "$logs_root/explicit-override.log"

jq -n \
  --arg version "$package_version" \
  --arg nativeSource "$(realpath "$native_library")" \
  --arg interop "CNA.Interop.$package_version.nupkg" \
  --arg framework "CNA.Framework.$package_version.nupkg" \
  --arg compat "CNA.XnaCompat.$package_version.nupkg" \
  '{
    schemaVersion: 1,
    status: "passed",
    qualifiedEvidenceScope: "linux-x64 local experiment only",
    packageVersion: $version,
    nativeSource: $nativeSource,
    packages: [$interop, $framework, $compat],
    contents: ["managed DLLs", "XML documentation", "LICENSE", "NOTICE.md", "README.md", "portable PDB symbol packages", "runtimes/linux-x64/native/libcna_c_api.so"],
    isolatedRestore: "passed",
    isolatedBuild: "passed",
    sourceOrSiblingPaths: "absent",
    packagedNativeWithoutEnvironment: "passed",
    frames60: "passed",
    frames600: "passed",
    missingNativeDiagnostic: "passed",
    wrongArchitectureDiagnostic: "passed",
    wrongAbiDiagnostic: "passed",
    missingSymbolDiagnostic: "passed",
    invalidExplicitPathDiagnostic: "passed",
    conflictingLibrariesDiagnostic: "passed",
    explicitOverridePrecedence: "passed",
    nativeAbiPolicy: "cna-cs-native-abi/1",
    nativeAbiCompatibilityFixtures: "4 accepted / 6 rejected",
    nativeAbiSelectedLibrary: "passed",
    published: false,
    supportedRidClaim: false
  }' >"$output_root/acceptance-report.json"

echo "PACKAGE_ACCEPTANCE_STATUS=passed"
echo "PACKAGE_ACCEPTANCE_OUTPUT=$output_root"
echo "PACKAGE_FILES=CNA.Interop.$package_version.nupkg,CNA.Framework.$package_version.nupkg,CNA.XnaCompat.$package_version.nupkg"
echo "PACKAGE_NATIVE_ENV_REQUIRED=no"
echo "PACKAGE_FRAMES_60=passed"
echo "PACKAGE_FRAMES_600=passed"
echo "PACKAGE_OVERRIDE_PRECEDENCE=passed"
