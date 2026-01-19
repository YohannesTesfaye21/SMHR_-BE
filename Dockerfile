# Use the official .NET 8 SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["SMHFR_BE.csproj", "./"]
RUN dotnet restore "SMHFR_BE.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src"
RUN dotnet build "SMHFR_BE.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "SMHFR_BE.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8443

# Install curl for healthchecks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=publish /app/publish .

# Set environment variable to use the connection string from docker-compose
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "SMHFR_BE.dll"]
