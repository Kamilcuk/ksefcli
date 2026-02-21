FROM mcr.microsoft.com/dotnet/sdk:10.0
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

