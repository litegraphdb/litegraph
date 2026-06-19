#!/bin/bash

if [ -z "${IMG_TAG}" ]; then
  IMG_TAG='v7.0.0'
fi

echo Using image tag $IMG_TAG

if [ ! -f "litegraph.json" ]
then
  echo Configuration file litegraph.json not found.
  exit
fi

# Items that require persistence
#   litegraph.json
#   logs/
#   temp/
#   backups/

# Argument order matters!

docker run \
  -p 8702:8702 \
  -p 8703:8703 \
  -p 8704:8704 \
  -t \
  -i \
  -e "TERM=xterm-256color" \
  -v ./litegraph.json:/app/litegraph.json \
  -v ./logs/:/app/logs/ \
  -v ./temp/:/app/temp/ \
  -v ./backups/:/app/backups/ \
  jchristn77/litegraph-mcp:$IMG_TAG
