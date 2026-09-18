#!/usr/bin/env bash
# G7: bootstraps a PINNED, checksum-verified gitleaks into the repo-local .tools/ (gitignored), so the
# secret-scan gate needs no system-wide install (O6: setup is a script, not a document). Idempotent.
# Prints the path of the binary on stdout; everything else goes to stderr.
set -euo pipefail

VERSION="8.30.1"
TOOLS_DIR=".tools"

case "$(uname -s)" in
  MINGW*|MSYS*|CYGWIN*) platform="windows"; ext="zip";    bin="$TOOLS_DIR/gitleaks.exe" ;;
  Linux)                platform="linux";   ext="tar.gz"; bin="$TOOLS_DIR/gitleaks" ;;
  Darwin)               platform="darwin";  ext="tar.gz"; bin="$TOOLS_DIR/gitleaks" ;;
  *) echo "install-gitleaks: unsupported OS $(uname -s)" >&2; exit 1 ;;
esac
case "$(uname -m)" in
  x86_64|amd64)  cpu="x64" ;;
  arm64|aarch64) cpu="arm64" ;;
  *) echo "install-gitleaks: unsupported architecture $(uname -m)" >&2; exit 1 ;;
esac

# SHA-256 of every supported archive, copied from gitleaks_8.30.1_checksums.txt of the v8.30.1 release and
# committed here on purpose: a hash fetched from the same place as the archive would only catch a broken
# download, never a tampered release. To bump VERSION, replace this table from the new release's checksums file:
#   curl -fsSL https://github.com/gitleaks/gitleaks/releases/download/v<VERSION>/gitleaks_<VERSION>_checksums.txt
case "${platform}_${cpu}" in
  windows_x64)   expected="d29144deff3a68aa93ced33dddf84b7fdc26070add4aa0f4513094c8332afc4e" ;;
  windows_arm64) expected="b95f5e4f5c425cedca7ee203d9afd29597e692c4924a12ed42f970537c72cc0f" ;;
  linux_x64)     expected="551f6fc83ea457d62a0d98237cbad105af8d557003051f41f3e7ca7b3f2470eb" ;;
  linux_arm64)   expected="e4a487ee7ccd7d3a7f7ec08657610aa3606637dab924210b3aee62570fb4b080" ;;
  darwin_x64)    expected="dfe101a4db2255fc85120ac7f3d25e4342c3c20cf749f2c20a18081af1952709" ;;
  darwin_arm64)  expected="b40ab0ae55c505963e365f271a8d3846efbc170aa17f2607f13df610a9aeb6a5" ;;
  *) echo "install-gitleaks: no pinned checksum for ${platform}_${cpu}" >&2; exit 1 ;;
esac

if [ -x "$bin" ] && "$bin" version 2>/dev/null | grep -q "$VERSION"; then
  echo "$bin"
  exit 0
fi

asset="gitleaks_${VERSION}_${platform}_${cpu}.${ext}"
base="https://github.com/gitleaks/gitleaks/releases/download/v${VERSION}"
mkdir -p "$TOOLS_DIR"

echo "install-gitleaks: downloading $asset (pinned v${VERSION})" >&2
curl -fsSL -o "$TOOLS_DIR/$asset" "$base/$asset"

if command -v sha256sum >/dev/null 2>&1; then
  actual=$(sha256sum "$TOOLS_DIR/$asset" | awk '{print $1}')
else
  actual=$(shasum -a 256 "$TOOLS_DIR/$asset" | awk '{print $1}')   # macOS ships shasum, not sha256sum
fi
if [ "$expected" != "$actual" ]; then
  echo "install-gitleaks: CHECKSUM MISMATCH for $asset (pinned '$expected', got '$actual') - refusing to use it" >&2
  rm -f "$TOOLS_DIR/$asset"
  exit 1
fi

if [ "$ext" = "zip" ]; then
  unzip -oq "$TOOLS_DIR/$asset" gitleaks.exe -d "$TOOLS_DIR"
else
  tar -xzf "$TOOLS_DIR/$asset" -C "$TOOLS_DIR" gitleaks
fi
rm -f "$TOOLS_DIR/$asset"
chmod +x "$bin"

echo "install-gitleaks: verified sha256 against the pinned value and installed $bin" >&2
echo "$bin"
