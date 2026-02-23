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

FROM cgr.dev/chainguard/wolfi-base AS test
RUN apk add --no-cache bash ca-certificates libstdc++ coreutils
ARG EXE ./kcksefcli
COPY ${EXE} ${EXE}
ENV EXE=${EXE}
RUN ${EXE} --help
COPY tests tests
RUN time ./tests/unit.sh ${EXE}
# RUN time ./tests/integration.sh -r WystawFaktureOffline ${EXE}

