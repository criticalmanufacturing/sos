# This script install cmf-sos and its dependencies on a Linux system. It should be run with root privileges.
# This can be obtained using "curl -O https://github.com/criticalmanufacturing/sos/installation/install.sh" and then "sudo bash install.sh".

echo "Removing any existing cmf-sos installation..."
rm -f /usr/local/bin/cmf-sos

echo "Installing cmf-sos and its dependencies..."
gh release download v1.0.0 -R criticalmanufacturing/sos -p cmf-sos
mv cmf-sos /usr/local/bin/
chmod +x /usr/local/bin/cmf-sos

echo "cmf-sos installed successfully. You can now run 'cmf-sos' to start the application."