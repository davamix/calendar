# syntax=docker/dockerfile:1

# --- Build stage ---------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first (cached) using just the project file.
COPY src/CalendarApi/CalendarApi.csproj src/CalendarApi/
RUN dotnet restore src/CalendarApi/CalendarApi.csproj

# Copy the rest and publish a framework-dependent build.
COPY . .
RUN dotnet publish src/CalendarApi/CalendarApi.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

# --- Runtime stage -------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Kestrel listens on 8080 inside the container (default for the aspnet image).
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Run as the image's built-in non-root user (resolves Trivy AVD-DS-0002).
USER $APP_UID

ENTRYPOINT ["dotnet", "CalendarApi.dll"]
