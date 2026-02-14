# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy all project files
COPY ["AppointmentApp.API/AppointmentApp.API.csproj", "AppointmentApp.API/"]
COPY ["AppointmentApp.Domain/AppointmentApp.Domain.csproj", "AppointmentApp.Domain/"]
COPY ["AppointmentApp.Service/AppointmentApp.Service.csproj", "AppointmentApp.Service/"]
COPY ["AppointmentApp.Repository/AppointmentApp.Repository.csproj", "AppointmentApp.Repository/"]
COPY ["AppointmentApp.Postgres/AppointmentApp.Postgres.csproj", "AppointmentApp.Postgres/"]

# Restore dependencies
RUN dotnet restore "AppointmentApp.API/AppointmentApp.API.csproj"

# Copy all source files
COPY . .

# Build and publish in one step
WORKDIR "/src/AppointmentApp.API"
RUN dotnet publish "AppointmentApp.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published files
COPY --from=build /app/publish .

# Expose port
EXPOSE 5001

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5001
ENV ASPNETCORE_ENVIRONMENT=Production

# Entry point
ENTRYPOINT ["dotnet", "AppointmentApp.API.dll"]
