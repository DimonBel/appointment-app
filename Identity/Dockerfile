# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy all project files
COPY ["IdentityApp.API/IdentityApp.API.csproj", "IdentityApp.API/"]
COPY ["IdentityApp.Domain/IdentityApp.Domain.csproj", "IdentityApp.Domain/"]
COPY ["IdentityApp.Service/IdentityApp.Service.csproj", "IdentityApp.Service/"]
COPY ["IdentityApp.Repository/IdentityApp.Repository.csproj", "IdentityApp.Repository/"]
COPY ["IdentityApp.Postgres/IdentityApp.Postgres.csproj", "IdentityApp.Postgres/"]

# Restore dependencies
RUN dotnet restore "IdentityApp.API/IdentityApp.API.csproj"

# Copy all source files
COPY . .

# Build and publish in one step
WORKDIR "/src/IdentityApp.API"
RUN dotnet publish "IdentityApp.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published files
COPY --from=build /app/publish .

# Expose port
EXPOSE 5005

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5005
ENV ASPNETCORE_ENVIRONMENT=Production

# Entry point
ENTRYPOINT ["dotnet", "IdentityApp.API.dll"]
