#!/usr/bin/env bash
set -euo pipefail
repo_root="$(cd "$(dirname "$0")/.." && pwd)"
payload="$repo_root/dist/payload"
runtime="$payload/runtime"
mkdir -p "$runtime/lib/gstreamer-1.0" "$runtime/libexec/gstreamer-1.0" "$payload/licenses/runtime" "$repo_root/build"
# Keep the plugins needed for mirroring, audio and codec negotiation.
plugins=(coreelements app playback typefindfunctions videoparsersbad videoconvertscale videofilter videorate autodetect d3d11 wasapi wasapi2 directsound audioconvert audioresample audioparsers volume audiofx libav isomp4 jpeg png rtp rtpmanager videobox compositor audiomixer)
manifest="$repo_root/build/runtime-files.txt"
: > "$manifest"
queue=()
declare -A dll_files
for file in /ucrt64/bin/*.dll; do
  name="$(basename "$file")"
  dll_files["${name,,}"]="$file"
done
printf 'Bundling runtime plugins and DLL dependencies...\n' 
copy_file() {
  local src="$1" rel="$2"
  if [[ ! -f "$runtime/$rel" ]]; then
    mkdir -p "$(dirname "$runtime/$rel")"
    cp "$src" "$runtime/$rel"
    printf '%s\n' "$src" >> "$manifest"
    queue+=("$src")
  fi
}
# Engine exists in the payload already, but must be scanned for imported DLLs.
queue+=("$runtime/bin/uxplay.exe")
for plugin in "${plugins[@]}"; do
  file="/ucrt64/lib/gstreamer-1.0/libgst${plugin}.dll"
  if [[ -f "$file" ]]; then copy_file "$file" "lib/gstreamer-1.0/$(basename "$file")"; fi
done
for required in coreelements app playback videoparsersbad videoconvertscale videofilter d3d11 wasapi audioconvert audioresample volume libav; do
  test -f "$runtime/lib/gstreamer-1.0/libgst${required}.dll" || { echo "Missing required plugin: $required" >&2; exit 1; }
done
copy_file /ucrt64/libexec/gstreamer-1.0/gst-plugin-scanner.exe libexec/gstreamer-1.0/gst-plugin-scanner.exe
copy_file /ucrt64/bin/gst-inspect-1.0.exe bin/gst-inspect-1.0.exe
# Recursively copy only imported UCRT64 DLLs. Windows system DLLs stay in Windows.
for ((i=0;i<${#queue[@]};i++)); do
  while IFS= read -r dll; do
    [[ -n "$dll" ]] || continue
    file="${dll_files[${dll,,}]:-}"
    if [[ -n "$file" && -f "$file" ]]; then copy_file "$file" "bin/$(basename "$file")"; fi
  done < <(objdump -p "${queue[i]}" | awk '/DLL Name:/{sub(/.*DLL Name: */,"");gsub(/\r/,"");print}')
done
sort -u "$manifest" -o "$manifest"
packages="$repo_root/build/runtime-packages.txt"
: > "$packages"
xargs -d '\n' -n 40 pacman -Qqo < "$manifest" | sort -u > "$packages"
while IFS= read -r license; do
  [[ -f "$license" ]] || continue
  rel="${license#/ucrt64/share/licenses/}"
  mkdir -p "$payload/licenses/runtime/$(dirname "$rel")"
  cp "$license" "$payload/licenses/runtime/$rel"
done < <(xargs -n 40 pacman -Ql < "$packages" | awk '$2 ~ /^\/ucrt64\/share\/licenses\// {print $2}' | sort -u)
printf 'Bundled %s runtime files from %s packages.\n' "$(wc -l < "$manifest")" "$(wc -l < "$packages")"
pacman -Q $(cat "$packages") > "$payload/runtime-versions.txt"
# All source archives include the upstream sources and the MSYS2 build recipe.
/usr/bin/python "$repo_root/scripts/collect-sources.py" "$repo_root"
