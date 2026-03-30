FROM dev.criticalmanufacturing.io/platformengineering/ubuntu-base-24.04:latest AS sos

ENV NODE_MAJOR=20
ENV DOTNET_SDK=8.0

RUN apt-get update && \
    apt-get install -y gnupg npm && \
    curl -fsSL https://deb.nodesource.com/gpgkey/nodesource-repo.gpg.key | gpg --dearmor -o /etc/apt/keyrings/nodesource.gpg && \
    echo "deb [signed-by=/etc/apt/keyrings/nodesource.gpg] https://deb.nodesource.com/node_$NODE_MAJOR.x nodistro main" | tee /etc/apt/sources.list.d/nodesource.list && \
    apt-get update

# Install NodeJS Tooling
RUN apt-get install -y nodejs

ENV DOTNET_TOOLS_PATH=/usr/lib/dotnet/tools

# Install DotNet Tooling
RUN apt-get install -y dotnet-sdk-$DOTNET_SDK && \
    mkdir -p $DOTNET_TOOLS_PATH && \
    dotnet tool install dotnet-dump --version $DOTNET_SDK.* --tool-path=$DOTNET_TOOLS_PATH && \
    dotnet tool install dotnet-counters --version $DOTNET_SDK.* --tool-path=$DOTNET_TOOLS_PATH && \
    dotnet tool install dotnet-symbol --tool-path $DOTNET_TOOLS_PATH && \
    chown -R $USER_ID:$USER_GROUP /usr/lib/dotnet/ && \
    chown -R $USER_ID:$USER_GROUP $DOTNET_TOOLS_PATH && \
    chown -R $USER_ID:$USER_GROUP /tmp

ENV PATH=$PATH:$DOTNET_TOOLS_PATH

# Run as root by default
USER 0