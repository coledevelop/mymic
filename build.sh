#!/usr/bin/env bash
set -euo pipefail

ARCH="${1:-$(uname -m)}"
case "$ARCH" in
  arm64)         RID=osx-arm64 ;;
  x86_64|x64)    RID=osx-x64 ;;
  *) echo "unknown arch: $ARCH (expected arm64 or x86_64)" >&2; exit 1 ;;
esac

ROOT="$(cd "$(dirname "$0")" && pwd)"
PROJ="$ROOT/MyMic/MyMic.csproj"
PUB="$ROOT/build/publish-$RID"
APP="$ROOT/dist/MyMic.app"
INFO="$ROOT/MyMic/macOS/Info.plist"
ICON="$ROOT/MyMic/macOS/AppIcon.icns"

echo "==> publishing for $RID"
rm -rf "$PUB" "$APP"
mkdir -p "$ROOT/dist"

dotnet publish "$PROJ" \
  -c Release \
  -r "$RID" \
  --self-contained true \
  -p:PublishSingleFile=false \
  -p:DebugType=none \
  -p:DebugSymbols=false \
  -o "$PUB" \
  >/dev/null

echo "==> assembling $APP"
mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"
cp "$INFO" "$APP/Contents/Info.plist"
cp "$ICON" "$APP/Contents/Resources/AppIcon.icns"
cp -R "$PUB"/. "$APP/Contents/MacOS/"

chmod +x "$APP/Contents/MacOS/MyMic"

echo "==> ad-hoc codesigning"
codesign --force --deep --sign - "$APP" 2>/dev/null

echo
echo "built: $APP"
echo "run:   open '$APP'   (or double-click in Finder)"
