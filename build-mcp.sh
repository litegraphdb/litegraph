#!/usr/bin/env bash
# Build and push the multi-architecture image with the given tag.  A release tag (vMAJOR.MINOR.PATCH,
# for example v10.0.0) also moves :latest; any other tag (v10.0.0-rc1, test builds) leaves :latest alone.
# Mirrors build-mcp.bat.  Run from the repository root.
set -euo pipefail

if [ "$#" -ne 1 ]; then
  echo
  echo "Provide a tag argument."
  echo "Example: ./build-mcp.sh v10.0.0"
  exit 1
fi

TAG="$1"
echo
echo "Building for linux/amd64 and linux/arm64/v8..."
latest_args=()
if [[ "${TAG}" =~ ^v[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
  latest_args=(--tag jchristn77/litegraph-mcp:latest)
  echo "Release tag ${TAG}: also tagging jchristn77/litegraph-mcp:latest"
else
  echo "Not a release tag: jchristn77/litegraph-mcp:latest is left unchanged"
fi
docker buildx build -f src/LiteGraph.McpServer/Dockerfile --builder cloud-jchristn77-jchristn77 --platform linux/amd64,linux/arm64/v8 --tag "jchristn77/litegraph-mcp:${TAG}" "${latest_args[@]}" --push .
echo
echo "Done"
