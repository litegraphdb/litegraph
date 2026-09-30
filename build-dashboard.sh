#!/usr/bin/env bash
# Build and push the multi-architecture image with the given tag.  A release tag (vMAJOR.MINOR.PATCH,
# for example v10.0.0) also moves :latest; any other tag (v10.0.0-rc1, test builds) leaves :latest alone.
# Mirrors build-dashboard.bat.  Run from the repository root.
set -euo pipefail

if [ "$#" -ne 1 ]; then
  echo
  echo "Provide a tag argument."
  echo "Example: ./build-dashboard.sh v10.0.0"
  exit 1
fi

TAG="$1"
echo
echo "Building for linux/amd64 and linux/arm64/v8..."
latest_args=()
if [[ "${TAG}" =~ ^v[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
  latest_args=(--tag jchristn77/litegraph-ui:latest)
  echo "Release tag ${TAG}: also tagging jchristn77/litegraph-ui:latest"
else
  echo "Not a release tag: jchristn77/litegraph-ui:latest is left unchanged"
fi
docker buildx build -f dashboard/Dockerfile --builder cloud-jchristn77-jchristn77 --platform linux/amd64,linux/arm64/v8 --tag "jchristn77/litegraph-ui:${TAG}" "${latest_args[@]}" --push dashboard
echo
echo "Done"
