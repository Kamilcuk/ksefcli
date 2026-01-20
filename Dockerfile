# Use the .NET 10.0 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /app

# Copy the solution file and project files
COPY src/KSeFCli/KSeFCli.csproj ./src/KSeFCli/
COPY thirdparty/ksef-client-csharp/KSeF.Client/KSeF.Client.csproj ./thirdparty/ksef-client-csharp/KSeF.Client/
COPY thirdparty/ksef-client-csharp/KSeF.Client.Core/KSeF.Client.Core.csproj ./thirdparty/ksef-client-csharp/KSeF.Client.Core/

# Restore dependencies
RUN dotnet restore src/KSeFCli/KSeFCli.csproj

# Copy the rest of the source code
COPY src/ src/
COPY thirdparty/ksef-client-csharp/KSeF.Client/ thirdparty/ksef-client-csharp/KSeF.Client/
COPY thirdparty/ksef-client-csharp/KSeF.Client.Core/ thirdparty/ksef-client-csharp/KSeF.Client.Core/

# Publish the application as a self-contained single file
RUN dotnet publish src/KSeFCli/KSeFCli.csproj -c Release -o /app/publish -r linux-x64 --self-contained true /p:PublishSingleFile=true

# Use the .NET 10 runtime dependencies for the final image
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0 AS final

WORKDIR /app

# Set PATH for the application
ENV PATH="$PATH:/app"

# Copy the published output from the build stage
COPY --from=build /app/publish .

RUN ["ksefcli", "--help"]

# Set the entrypoint for the application
ENTRYPOINT ["ksefcli"]
