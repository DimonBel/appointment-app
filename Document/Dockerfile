# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Add build arg for cache busting
ARG BUILD_DATE
ARG CACHEBUST=1

# Copy all files
COPY . .

# Restore dependencies
RUN dotnet restore "DocumentApp.API/DocumentApp.API.csproj"

# Build and publish
WORKDIR "/src/DocumentApp.API"
RUN dotnet build "DocumentApp.API.csproj" -c Release -o /app/build
RUN dotnet publish "DocumentApp.API.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published files
COPY --from=build /app/publish .

# Expose port
EXPOSE 5004

# Set environment variable
ENV ASPNETCORE_URLS=http://+:5004

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:5004/health || exit 1

ENTRYPOINT ["dotnet", "DocumentApp.API.dll"]