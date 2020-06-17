ARG base_tag=3.1.4-nanoserver-1809
FROM mcr.microsoft.com/dotnet/core/runtime:3.1

ARG EXE_DIR=.

WORKDIR /app

COPY $EXE_DIR/ ./

CMD ["dotnet", "get-twin.dll"]