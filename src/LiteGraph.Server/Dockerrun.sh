if [ -z "${IMG_TAG}" ]; then
  IMG_TAG='v2.0.0'
fi

echo Using image tag $IMG_TAG

if [ ! -f "litegraph.json" ]
then
  echo Configuration file litegraph.json not found.
  exit
fi

# Items that require persistence
#   litegraph.json
#   litegraph.db
#   logs/

# Argument order matters!

docker run \
  -p 8701:8701 \
  -t \
  -i \
  -e "TERM=xterm-256color" \
  -v ./litegraph.json:/app/litegraph.json \
  -v ./litegraph.db:/app/litegraph.db \
  -v ./logs/:/app/logs/ \
  jchristn77/litegraph:$IMG_TAG
