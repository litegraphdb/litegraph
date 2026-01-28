#!/bin/bash

if [ -z "${IMG_TAG}" ]; then
  IMG_TAG='v1.0.0'
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
  -p 8200:8200 \
  -p 8201:8201 \
  -p 8202:8202 \
  -t \
  -i \
  -e "TERM=xterm-256color" \
  -v ./litegraph.json:/app/litegraph.json \
  -v ./logs/:/app/logs/ \
  -v ./temp/:/app/temp/ \
  -v ./backups/:/app/backups/ \
  jchristn77/litegraph-mcp:$IMG_TAG

