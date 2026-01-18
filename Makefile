.PHONY: all build clean

all: build

build:
	dotnet build

clean:
	dotnet clean

solution:
	dotnet new sln -o ksefcli.sln
