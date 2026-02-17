# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy all project files
COPY ["NotificationApp.API/NotificationApp.API.csproj", "NotificationApp.API/"]
COPY ["NotificationApp.Domain/NotificationApp.Domain.csproj", "NotificationApp.Domain/"]
COPY ["NotificationApp.Service/NotificationApp.Service.csproj", "NotificationApp.Service/"]
COPY ["NotificationApp.Repository/NotificationApp.Repository.csproj", "NotificationApp.Repository/"]
COPY ["NotificationApp.Postgres/NotificationApp.Postgres.csproj", "NotificationApp.Postgres/"]

# Restore dependencies
RUN dotnet restore "NotificationApp.API/NotificationApp.API.csproj"

# Copy all source files
COPY . .

# Build and publish in one step
WORKDIR "/src/NotificationApp.API"
RUN dotnet publish "NotificationApp.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published files
COPY --from=build /app/publish .

# Expose port
EXPOSE 5003

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5003
ENV ASPNETCORE_ENVIRONMENT=Production

# Entry point
ENTRYPOINT ["dotnet", "NotificationApp.API.dll"]
