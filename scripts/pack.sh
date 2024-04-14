#!/bin/bash

# Check if the version parameter is provided
if [ -z "$1" ]; then
    echo "Usage: $0 <version>"
    exit 1
fi

version="$1"

# Pack main project
dotnet pack ../src/ -c Release -o ../dist -p:PackageVersion=$version
