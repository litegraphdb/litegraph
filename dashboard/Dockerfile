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
COPY package*.json ./
RUN npm install
COPY . .
RUN npm run build
EXPOSE 3000
CMD [ "npm", "run", "start" ]
