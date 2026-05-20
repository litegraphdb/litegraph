#
#
# Run the docker build command from the repository root, not from src or the project directory.
#
#


#
#
# Build stage
#
#
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo

# Copy source
COPY src ./src
COPY sdk/csharp ./sdk/csharp
COPY README.md ./README.md

# Restore dependencies
RUN dotnet restore "src/LiteGraph.McpServer/LiteGraph.McpServer.csproj"

# Build the application
WORKDIR /repo/src/LiteGraph.McpServer
RUN dotnet build "LiteGraph.McpServer.csproj" -c Release -f net10.0 -o /app/build /p:GeneratePackageOnBuild=false /m:1


#
#
# Publish stage
#
#
FROM build AS publish
RUN dotnet publish "LiteGraph.McpServer.csproj" -c Release -f net10.0 -o /app/publish /p:UseAppHost=false /p:GeneratePackageOnBuild=false /p:ErrorOnDuplicatePublishOutputFiles=false


#
#
# Runtime stage
#
#
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS final
RUN apt-get update && apt-get install -y iputils-ping traceroute net-tools curl wget dnsutils iproute2 vim file && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8702 8703 8704
ENTRYPOINT ["dotnet", "LiteGraph.McpServer.dll"]

