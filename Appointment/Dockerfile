# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy only the necessary source files for Appointment service
COPY ["Appointment/AppointmentApp.API/AppointmentApp.API.csproj", "Appointment/AppointmentApp.API/"]
COPY ["Appointment/AppointmentApp.Domain/AppointmentApp.Domain.csproj", "Appointment/AppointmentApp.Domain/"]
COPY ["Appointment/AppointmentApp.Repository/AppointmentApp.Repository.csproj", "Appointment/AppointmentApp.Repository/"]
COPY ["Appointment/AppointmentApp.Service/AppointmentApp.Service.csproj", "Appointment/AppointmentApp.Service/"]
COPY ["Appointment/AppointmentApp.Postgres/AppointmentApp.Postgres.csproj", "Appointment/AppointmentApp.Postgres/"]
COPY ["Identity/IdentityApp.API/IdentityApp.API.csproj", "Identity/IdentityApp.API/"]
COPY ["Identity/IdentityApp.Domain/IdentityApp.Domain.csproj", "Identity/IdentityApp.Domain/"]
COPY ["Identity/IdentityApp.Repository/IdentityApp.Repository.csproj", "Identity/IdentityApp.Repository/"]
COPY ["Identity/IdentityApp.Service/IdentityApp.Service.csproj", "Identity/IdentityApp.Service/"]
COPY ["Identity/IdentityApp.Postgres/IdentityApp.Postgres.csproj", "Identity/IdentityApp.Postgres/"]

# Restore dependencies
RUN dotnet restore "Appointment/AppointmentApp.API/AppointmentApp.API.csproj"

# Copy source code (excluding appsettings files from IdentityApp)
COPY Appointment/ ./Appointment/
COPY Identity/IdentityApp.API/ ./Identity/IdentityApp.API/
COPY Identity/IdentityApp.Domain/ ./Identity/IdentityApp.Domain/
COPY Identity/IdentityApp.Repository/ ./Identity/IdentityApp.Repository/
COPY Identity/IdentityApp.Service/ ./Identity/IdentityApp.Service/
COPY Identity/IdentityApp.Postgres/ ./Identity/IdentityApp.Postgres/

# Remove IdentityApp.API appsettings files to avoid conflicts
RUN rm -f /src/Identity/IdentityApp.API/appsettings*.json

# Build and publish in one step
WORKDIR "/src/Appointment/AppointmentApp.API"
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
