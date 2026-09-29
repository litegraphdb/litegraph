#!/usr/bin/env bash
# Build and push the multi-architecture image with the given tag (and :latest).
# Mirrors build-server.bat.  Run from the repository root.
set -euo pipefail

if [ "$#" -ne 1 ]; then
  echo
  echo "Provide a tag argument."
  echo "Example: ./build-server.sh v10.0.0"
  exit 1
fi

TAG="$1"
echo
echo "Building for linux/amd64 and linux/arm64/v8..."
docker buildx build -f src/LiteGraph.Server/Dockerfile --builder cloud-jchristn77-jchristn77 --platform linux/amd64,linux/arm64/v8 --tag "jchristn77/litegraph:${TAG}" --tag jchristn77/litegraph:latest --push src
echo
echo "Done"
