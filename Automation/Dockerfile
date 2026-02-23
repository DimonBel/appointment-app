# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy the entire source directory to preserve relative paths
COPY . .

# Restore dependencies
RUN dotnet restore "AutomationApp.API/AutomationApp.API.csproj"

# Build and publish in one step
WORKDIR "/src/AutomationApp.API"
RUN dotnet publish "AutomationApp.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published files
COPY --from=build /app/publish .

# Expose port
EXPOSE 5006

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5006
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:5006/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "AutomationApp.API.dll"]