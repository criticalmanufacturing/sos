FROM registry.access.redhat.com/ubi8/ubi:latest

RUN yum install -y procps-ng curl bash tar gzip libicu

RUN curl -sSL https://aka.ms/getvsdbgsh | bash /dev/stdin -v latest -l /vsdbg

# Make lib64 writable so our non-root user (1001) can create the framework symlink
# Pre-create and give permissions to common structural paths to allow non-root symlinking
RUN chmod 777 /usr/lib64 && \
    mkdir -p /usr/share && chmod 777 /usr/share && \
    mkdir -p /usr/lib && chmod 777 /usr/lib && \
    mkdir -p /app && chmod 777 /app && \
    mkdir -p /src && chmod 777 /src && \
    mkdir -p /workspace && chmod 777 /workspace && \
    mkdir -p /opt && chmod 777 /opt && \
    mkdir -p /var/www && chmod 777 /var/www

# Create the wrapper script. 
RUN { \
      echo '#!/bin/bash'; \
      echo 'echo "Starting wrapper execution..." > /tmp/wrapper.log'; \
      echo '# Select correct dotnet PID by looking for loaded coreclr'; \
      echo 'DOTNET_PID=$(grep -l libcoreclr.so /proc/[0-9]*/maps 2>/dev/null | cut -d"/" -f3 | head -n 1)'; \
      echo 'if [ -z "$DOTNET_PID" ]; then'; \
      echo '  echo "Fallback to cmdline PID detection..." >> /tmp/wrapper.log'; \
      echo '  DOTNET_PID=$(for p in /proc/[0-9]*; do cmd=$(tr -d "\0" < $p/cmdline 2>/dev/null); case "$cmd" in dotnet*) echo $(basename $p); break;; esac; done)'; \
      echo 'fi'; \
      echo 'echo "Selected PID=$DOTNET_PID" >> /tmp/wrapper.log'; \
      echo '# 1. Export TMPDIR to natively bridge the IPC Socket without symlinks'; \
      echo 'SOCKET_PATH=$(ls /proc/$DOTNET_PID/root/tmp/dotnet-diagnostic-* 2>/dev/null | head -n 1)'; \
      echo 'if [ -n "$SOCKET_PATH" ]; then'; \
      echo '  export TMPDIR=$(dirname "$SOCKET_PATH")'; \
      echo '  echo "Exported TMPDIR=$TMPDIR" >> /tmp/wrapper.log'; \
      echo 'fi'; \
      echo '# 2. Bridge the .NET Framework and application assemblies'; \
      echo 'if [ -n "$DOTNET_PID" ]; then'; \
      echo '  for fw_dir in "/usr/share/dotnet" "/usr/lib64/dotnet" "/usr/lib/dotnet"; do'; \
      echo '    if [ -d "/proc/$DOTNET_PID/root$fw_dir" ]; then'; \
      echo '      ln -sfn "/proc/$DOTNET_PID/root$fw_dir" "$fw_dir" >> /tmp/wrapper.log 2>&1 || true'; \
      echo '    fi'; \
      echo '  done'; \
      echo '  APP_DIRS=$(grep -i "\.dll$" /proc/$DOTNET_PID/maps 2>/dev/null | awk "{print \$6}" | xargs -I {} dirname {} | sort -u | grep -v "^/usr/")'; \
      echo '  for d in $APP_DIRS; do'; \
      echo '    if [ -n "$d" ] && [ "$d" != "/" ] && [ -d "/proc/$DOTNET_PID/root$d" ]; then'; \
      echo '      mkdir -p "$d" 2>/dev/null || true'; \
      echo '      if [ -d "$d" ]; then'; \
      echo '        for item in "/proc/$DOTNET_PID/root$d"/*; do'; \
      echo '          if [ -e "$item" ]; then ln -sfn "$item" "$d/$(basename "$item")" >> /tmp/wrapper.log 2>&1 || true; fi'; \
      echo '        done'; \
      echo '      fi'; \
      echo '    fi'; \
      echo '  done'; \
      echo 'fi'; \
      echo '# 3. Launch vsdbg'; \
      echo 'exec /vsdbg/vsdbg "$@"'; \
    } > /vsdbg/wrapper.sh && \
    chmod +x /vsdbg/wrapper.sh