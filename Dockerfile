FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
COPY ./src/KCKSeFCli/*.cs ./src/KCKSeFCli/
COPY ./src/KCKSeFCli/*.csproj ./src/KCKSeFCli/
COPY ./thirdparty/ksef-client-csharp/KSeF.Client/ ./thirdparty/ksef-client-csharp/KSeF.Client/
COPY ./thirdparty/ksef-client-csharp/KSeF.Client.Core/ ./thirdparty/ksef-client-csharp/KSeF.Client.Core/
COPY ./thirdparty/ksef-client-csharp/KSeF.Client.ClientFactory/ ./thirdparty/ksef-client-csharp/KSeF.Client.ClientFactory/
COPY ./thirdparty/ksef-client-csharp/KSeF.Client.Tests.Utils/ ./thirdparty/ksef-client-csharp/KSeF.Client.Tests.Utils/
COPY ./thirdparty/ksef-client-csharp/KSeF.Client.Tests.Core/ ./thirdparty/ksef-client-csharp/KSeF.Client.Tests.Core/
COPY ./thirdparty/ksef-client-csharp/Directory.Build.props ./thirdparty/ksef-client-csharp/
ARG RUNTIME=linux-x64
RUN dotnet publish src/KCKSeFCli/KCKSeFCli.csproj -c Release -r $RUNTIME -o dist

# Smallest docker image with glibc
FROM cgr.dev/chainguard/wolfi-base@sha256:73de6aadd7e28fb516fa1270fcb411b94ee79949635e7de2a4bdb8705f6c120c AS test
COPY tests/setup-wolfie.sh tests/setup-wolfie.sh
RUN ./tests/setup-wolfie.sh
ARG EXE ./kcksefcli
COPY ${EXE} ${EXE}
RUN ./kcksefcli --help
COPY tests tests
RUN time ./tests/unit.sh ${EXE}

FROM test AS itest
COPY secrets/ secrets/
RUN time ./tests/integration.sh -r WystawFaktureOffline ${EXE}

FROM mono:latest AS mono
WORKDIR /build
COPY . .
RUN nuget restore
RUN msbuild src/KCKSeFCli/KCKSeFCli.csproj /p:Configuration=Release /p:Platform=x86
