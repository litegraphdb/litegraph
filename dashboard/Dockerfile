FROM node:20-alpine

# Install basic networking and troubleshooting tools
RUN apk add --no-cache \
    curl \
    wget \
    netcat-openbsd \
    iputils \
    bind-tools \
    busybox-extras \
    net-tools \
    tcpdump \
    nmap

WORKDIR /app
ARG LITEGRAPH_SERVER=
ENV LITEGRAPH_SERVER=${LITEGRAPH_SERVER}
ENV NEXT_PUBLIC_LITEGRAPH_SERVER_URL=${LITEGRAPH_SERVER}
COPY package*.json ./
RUN npm install
COPY . .
RUN npm run build
EXPOSE 3000
CMD [ "npm", "run", "start" ]
