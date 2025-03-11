FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
RUN apt-get update && apt-get install -y iputils-ping traceroute net-tools curl wget dnsutils iproute2 vim file && rm -rf /var/lib/apt/lists/*
WORKDIR /app

# Copy everything from source directory into /App
COPY . ./
EXPOSE 8701
ENTRYPOINT ["dotnet", "LiteGraph.Server.dll"]
