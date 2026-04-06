FROM registry.access.redhat.com/ubi8/ubi:latest

# Install necessary tools for debugging and process management
RUN yum install -y procps-ng curl bash tar gzip libicu

# Install vsdbg
RUN curl -sSL https://aka.ms/getvsdbgsh | bash /dev/stdin -v latest -l /vsdbg

# Make lib64 writable so our non-root user (1001) can create the framework symlink
RUN chmod 777 /usr/lib64

# Create the wrapper script. 
RUN { \
      echo '#!/bin/bash'; \
      echo 'echo "Starting wrapper execution..." > /tmp/wrapper.log'; \
      echo '# 1. Export TMPDIR to natively bridge the IPC Socket without symlinks'; \
      echo 'SOCKET_PATH=$(ls /proc/[0-9]*/root/tmp/dotnet-diagnostic-* 2>/dev/null | head -n 1)'; \
      echo 'if [ -n "$SOCKET_PATH" ]; then'; \
      echo '  export TMPDIR=$(dirname "$SOCKET_PATH")'; \
      echo '  echo "Exported TMPDIR=$TMPDIR" >> /tmp/wrapper.log'; \
      echo 'fi'; \
      echo '# 2. Bridge the .NET Framework using the exact lib64 path RHEL expects'; \
      echo 'FRAMEWORK_DIR=$(ls -d /proc/[0-9]*/root/usr/lib64/dotnet 2>/dev/null | head -n 1)'; \
      echo 'if [ -n "$FRAMEWORK_DIR" ]; then'; \
      echo '  ln -sfn "$FRAMEWORK_DIR" /usr/lib64/dotnet >> /tmp/wrapper.log 2>&1 || true'; \
      echo '  echo "Symlinked framework from $FRAMEWORK_DIR" >> /tmp/wrapper.log'; \
      echo 'fi'; \
      echo '# 3. Launch vsdbg'; \
      echo 'exec /vsdbg/vsdbg "$@"'; \
    } > /vsdbg/wrapper.sh && \
    chmod +x /vsdbg/wrapper.sh