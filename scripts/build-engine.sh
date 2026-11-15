#!/usr/bin/env bash
set -euo pipefail
repo_root="$(cd "$(dirname "$0")/.." && pwd)"
build_root="$repo_root/build"
commit=05b0275cb1b726592700e47a6e0307544b3d3a52
mkdir -p "$build_root"
if [[ ! -d "$build_root/UxPlay/.git" ]]; then
  git clone https://github.com/FDH2/UxPlay.git "$build_root/UxPlay"
fi
cd "$build_root/UxPlay"
git fetch origin "$commit"
git checkout --detach "$commit"
cmake -S . -B build -G Ninja -DCMAKE_BUILD_TYPE=Release -DUSE_MDNS=ON
cmake --build build --parallel 2
mkdir -p "$repo_root/dist/payload/runtime/bin" "$repo_root/dist/corresponding-source"
cp build/uxplay.exe "$repo_root/dist/payload/runtime/bin/uxplay.exe"
git archive --format=tar.gz --prefix=UxPlay/ HEAD > "$repo_root/dist/corresponding-source/UxPlay-$commit.tar.gz"
cp LICENSE "$repo_root/dist/corresponding-source/UxPlay-LICENSE.txt"
