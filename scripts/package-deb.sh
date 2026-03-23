#!/usr/bin/env bash
set -euo pipefail

APP_NAME="ECoopSystem"
PACKAGE_NAME="ecoopsystem"
VERSION="1.0.0"
ARCH="amd64"
CONFIGURATION="Release"
RUNTIME="linux-x64"
SELF_CONTAINED="true"

PUBLISH_DIR=""
OUTPUT_DIR="output/installer"
WORK_DIR="output/deb"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --version)
      VERSION="$2"
      shift 2
      ;;
    --configuration)
      CONFIGURATION="$2"
      shift 2
      ;;
    --publish-dir)
      PUBLISH_DIR="$2"
      shift 2
      ;;
    --framework-dependent)
      SELF_CONTAINED="false"
      shift
      ;;
    --output)
      OUTPUT_DIR="$2"
      shift 2
      ;;
    *)
      echo "Unknown argument: $1"
      echo "Usage: $0 [--version 1.0.0] [--configuration Release] [--publish-dir <path>] [--framework-dependent] [--output <dir>]"
      exit 1
      ;;
  esac
done

if [[ -z "$PUBLISH_DIR" ]]; then
  echo "Publishing $APP_NAME for $RUNTIME..."
  if [[ "$SELF_CONTAINED" == "true" ]]; then
    dotnet publish -c "$CONFIGURATION" -r "$RUNTIME" --self-contained
  else
    dotnet publish -c "$CONFIGURATION" -r "$RUNTIME" --no-self-contained
  fi
  PUBLISH_DIR="bin/$CONFIGURATION/net9.0/$RUNTIME/publish"
fi

if [[ ! -d "$PUBLISH_DIR" ]]; then
  echo "Publish directory not found: $PUBLISH_DIR"
  exit 1
fi

echo "Preparing .deb package structure..."
rm -rf "$WORK_DIR"
mkdir -p "$WORK_DIR/DEBIAN"
mkdir -p "$WORK_DIR/opt/$PACKAGE_NAME"
mkdir -p "$WORK_DIR/usr/bin"
mkdir -p "$WORK_DIR/usr/share/applications"
mkdir -p "$WORK_DIR/usr/share/icons/hicolor/256x256/apps"
mkdir -p "$OUTPUT_DIR"

cp -a "$PUBLISH_DIR/." "$WORK_DIR/opt/$PACKAGE_NAME/"

if [[ -f "Assets/Images/logo.png" ]]; then
  cp "Assets/Images/logo.png" "$WORK_DIR/usr/share/icons/hicolor/256x256/apps/$PACKAGE_NAME.png"
fi

cat > "$WORK_DIR/usr/share/applications/$PACKAGE_NAME.desktop" << EOF
[Desktop Entry]
Type=Application
Name=$APP_NAME
Exec=/opt/$PACKAGE_NAME/$APP_NAME
Icon=$PACKAGE_NAME
Terminal=false
Categories=Office;Utility;
StartupNotify=true
EOF

cat > "$WORK_DIR/usr/bin/$PACKAGE_NAME" << EOF
#!/usr/bin/env sh
exec /opt/$PACKAGE_NAME/$APP_NAME "\$@"
EOF
chmod 755 "$WORK_DIR/usr/bin/$PACKAGE_NAME"

INSTALLED_SIZE=$(du -sk "$WORK_DIR/opt/$PACKAGE_NAME" | awk '{print $1}')

if [[ "$SELF_CONTAINED" == "true" ]]; then
  DEPENDS="libx11-6, libxext6, libxrender1, libxrandr2, libxi6, libxcursor1, libxdamage1, libxfixes3, libxcomposite1, libgtk-3-0, libnss3, libnspr4, libasound2, libatk1.0-0, libcups2, libdrm2, libgbm1, libatspi2.0-0"
else
  DEPENDS="dotnet-runtime-9.0, libx11-6, libxext6, libxrender1, libxrandr2, libxi6, libxcursor1, libxdamage1, libxfixes3, libxcomposite1, libgtk-3-0, libnss3, libnspr4, libasound2, libatk1.0-0, libcups2, libdrm2, libgbm1, libatspi2.0-0"
fi

cat > "$WORK_DIR/DEBIAN/control" << EOF
Package: $PACKAGE_NAME
Version: $VERSION
Section: utils
Priority: optional
Architecture: $ARCH
Depends: $DEPENDS
Maintainer: Lands Horizon <support@landshorizon.com>
Installed-Size: $INSTALLED_SIZE
Description: ECoopSystem secure cross-platform desktop client
 A secure Avalonia/.NET desktop application for ECoopSystem.
EOF

cat > "$WORK_DIR/DEBIAN/postinst" << 'EOF'
#!/usr/bin/env sh
set -e

if command -v update-desktop-database >/dev/null 2>&1; then
  update-desktop-database /usr/share/applications || true
fi

if command -v gtk-update-icon-cache >/dev/null 2>&1; then
  gtk-update-icon-cache -f /usr/share/icons/hicolor >/dev/null 2>&1 || true
fi
EOF

cat > "$WORK_DIR/DEBIAN/postrm" << 'EOF'
#!/usr/bin/env sh
set -e

if command -v update-desktop-database >/dev/null 2>&1; then
  update-desktop-database /usr/share/applications || true
fi

if command -v gtk-update-icon-cache >/dev/null 2>&1; then
  gtk-update-icon-cache -f /usr/share/icons/hicolor >/dev/null 2>&1 || true
fi
EOF

chmod 755 "$WORK_DIR/DEBIAN/postinst"
chmod 755 "$WORK_DIR/DEBIAN/postrm"

DEB_PATH="$OUTPUT_DIR/${APP_NAME}_${VERSION}_${ARCH}.deb"
dpkg-deb --build "$WORK_DIR" "$DEB_PATH"

echo "Done: $DEB_PATH"
echo "Install with: sudo apt install ./$DEB_PATH"
