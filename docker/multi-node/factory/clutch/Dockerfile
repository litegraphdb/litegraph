#
# Clutch server image with curl added for Docker healthchecks.
#
# The published jchristn77/clutch-server image does not include curl or wget, and every
# HTTP service in this deployment is healthchecked with curl.  Remove this wrapper once the
# published image includes curl.
#

ARG CLUTCH_IMAGE=jchristn77/clutch-server:v0.2.0
FROM ${CLUTCH_IMAGE}
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
