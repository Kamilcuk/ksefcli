#!/bin/bash
set -x
dotnet run --project ./src/KSeFCli/ -- "$@"
