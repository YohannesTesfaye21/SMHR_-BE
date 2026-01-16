# SMHFR Backend API

A .NET 8 Web API project with PostgreSQL database, Docker support, and Swagger documentation.

## Prerequisites

- .NET 8 SDK
- Docker and Docker Compose
- (Optional) PostgreSQL client tools

## Getting Started

### Option 1: Using Docker Compose (Recommended)

1. Build and run the entire stack:
```bash
docker-compose up --build
```

This will start both the PostgreSQL database and the API service.

2. Access the application:
   - API: http://localhost:8080
   - Swagger UI: http://localhost:8080 (root path)
   - PostgreSQL: localhost:5432

### Option 2: Local Development

1. Start PostgreSQL using Docker:
```bash
docker-compose up postgres
```

2. Update the connection string in `appsettings.Development.json` if needed.

3. Run the application:
```bash
dotnet restore
dotnet run
```

4. Access the application:
   - API: http://localhost:5000 or https://localhost:5001
   - Swagger UI: http://localhost:5000 or https://localhost:5001

## Database Migrations

When using Entity Framework Core migrations:

```bash
# Create a migration
dotnet ef migrations add MigrationName

# Apply migrations
dotnet ef database update
```

## Project Structure

```
SMHFR_BE/
├── Controllers/          # API controllers
├── Data/                 # DbContext and data models
├── Services/             # Business logic services
├── appsettings.json      # Configuration
├── Dockerfile            # Docker image definition
├── docker-compose.yml    # Docker Compose configuration
└── Program.cs            # Application entry point
```

## Configuration

Connection strings and other settings can be configured in:
- `appsettings.json` - Production settings
- `appsettings.Development.json` - Development settings

## API Documentation

Swagger UI is automatically available when running in Development mode at the root path.

## Docker Commands

```bash
# Build and start services
docker-compose up --build

# Start services in detached mode
docker-compose up -d

# Stop services
docker-compose down

# Stop and remove volumes
docker-compose down -v

# View logs
docker-compose logs -f api
docker-compose logs -f postgres
```

## License

[Your License Here]
