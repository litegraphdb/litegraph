#!/usr/bin/env bash
set -euo pipefail

if [ "$#" -ne 1 ]; then
  echo
  echo "Provide exactly one image tag argument."
  echo "Example: ./build-all.sh v6.0.0"
  exit 1
fi

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
image_tag="$1"

"${script_dir}/build-server.bat" "${image_tag}"
"${script_dir}/build-dashboard.bat" "${image_tag}"
"${script_dir}/build-mcp.bat" "${image_tag}"

echo
echo "Done"
