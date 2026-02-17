# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy all project files
COPY ["ChatApp.API/ChatApp.API.csproj", "ChatApp.API/"]
COPY ["ChatApp.Domain/ChatApp.Domain.csproj", "ChatApp.Domain/"]
COPY ["ChatApp.Service/ChatApp.Service.csproj", "ChatApp.Service/"]
COPY ["ChatApp.Repository/ChatApp.Repository.csproj", "ChatApp.Repository/"]
COPY ["ChatApp.Postgres/ChatApp.Postgres.csproj", "ChatApp.Postgres/"]

# Restore dependencies
RUN dotnet restore "ChatApp.API/ChatApp.API.csproj"

# Copy all source files
COPY . .

# Build and publish in one step
WORKDIR "/src/ChatApp.API"
RUN dotnet publish "ChatApp.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published files
COPY --from=build /app/publish .

# Expose port
EXPOSE 5002

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5002
ENV ASPNETCORE_ENVIRONMENT=Production

# Entry point
ENTRYPOINT ["dotnet", "ChatApp.API.dll"]
