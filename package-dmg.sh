#!/usr/bin/env bash
set -euo pipefail

ARCH="${1:-$(uname -m)}"
case "$ARCH" in
  arm64)        ARCH_LABEL="arm64" ;;
  x86_64|x64)   ARCH_LABEL="x64" ;;
  *) echo "unknown arch: $ARCH" >&2; exit 1 ;;
esac

VERSION="${VERSION:-0.1.0}"
ROOT="$(cd "$(dirname "$0")" && pwd)"
APP="$ROOT/dist/MyMic.app"
DMG="$ROOT/dist/MyMic-$VERSION-$ARCH_LABEL.dmg"
STAGE="$ROOT/dist/dmg-stage"

if [[ ! -d "$APP" ]]; then
  echo "MyMic.app not found at $APP. Run ./build.sh first." >&2
  exit 1
fi

rm -rf "$STAGE" "$DMG"
mkdir -p "$STAGE"

cp -R "$APP" "$STAGE/"
ln -s /Applications "$STAGE/Applications"

echo "==> creating $DMG"
hdiutil create \
  -volname "MyMic" \
  -srcfolder "$STAGE" \
  -ov \
  -format UDZO \
  "$DMG" \
  >/dev/null

rm -rf "$STAGE"
echo "built: $DMG"
