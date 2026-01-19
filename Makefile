.PHONY: all build clean run

all: build

build:
	dotnet build src/KSeFCli

run: build
	dotnet run --project src/KSeFCli --

format:
	dotnet format src/KSeFCli

test: build
	dotnet run --project src/KSeFCli -- --help
	dotnet format src/KSeFCli --verify-no-changes

clean:
	dotnet clean src/KSeFCli
