#!/bin/bash
set -e

if [ -n "$PORT" ]; then
    echo "PORT detected: $PORT"
    export ASPNETCORE_URLS="http://*:$PORT"
    echo "ASPNETCORE_URLS set to: $ASPNETCORE_URLS"
else
    echo "No PORT variable found, using default port 8080"
    export ASPNETCORE_URLS="http://*:8080"
fi

echo "Starting application..."

exec dotnet WhearApp.WebApi.dll