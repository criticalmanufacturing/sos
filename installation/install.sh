#!/bin/sh

# This script install cmf-sos and its dependencies on a Linux system. It should be run with root privileges.
# This can be obtained using:
#     curl -sfSL https://github.com/criticalmanufacturing/sos/raw/refs/heads/main/installation/install.sh | sh

set -e

[ "$(id -u)" -ne 0 ] && SUDO="sudo" || SUDO=""

echo "Removing any existing cmf-sos installation..."
$SUDO rm -f /usr/local/bin/cmf-sos

echo "Installing cmf-sos and its dependencies..."
$SUDO curl -sfSL https://github.com/criticalmanufacturing/sos/releases/latest/download/cmf-sos -o /usr/local/bin/cmf-sos
$SUDO chmod +x /usr/local/bin/cmf-sos

echo "cmf-sos installed successfully. You can now run 'cmf-sos' to start the application."
