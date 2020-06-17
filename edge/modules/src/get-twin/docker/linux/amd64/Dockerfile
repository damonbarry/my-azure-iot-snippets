FROM mcr.microsoft.com/dotnet/core/runtime:3.1-alpine

ARG EXE_DIR=.

WORKDIR /app

COPY $EXE_DIR/ ./

CMD echo "$(date --utc +"%Y-%m-%d %H:%M:%S %:z") Starting get-twin" && \
    exec /usr/bin/dotnet get-twin.dll
