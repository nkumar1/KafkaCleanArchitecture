# Kafka Vehicle Location Processing Solution

This solution provides an end-to-end pipeline for ingesting, processing, and persisting vehicle location data using Apache Kafka, a .NET 8 Worker Service, and a simulated Web API. It is designed for rapid prototyping and demonstration purposes.

---

## 📂 Table of Contents

- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [Running Kafka and Zookeeper with Docker Compose](#running-kafka-and-zookeeper-with-docker-compose)
- [Configuration](#configuration)
- [Build and Run](#build-and-run)
- [Health Checks](#health-checks)
- [Stopping Services](#stopping-services)
- [Project Structure](#project-structure)
- [Contributing](#contributing)

---

## 🦩 Overview

This solution consists of the following components:

- **Kafka.Worker**: .NET 8 Worker Service that consumes vehicle location messages from a Kafka topic and persists them to a SQL Server database.
- **SimulatedSamsaraAPI**: .NET 8 Web API for simulating vehicle location data and publishing it to Kafka.
- **Infrastructure**: Implements data access logic and sets up dependency injection.
- **Domain**: Contains core business entities and interfaces.

---

## 🛠 Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- (Optional) SQL Server (locally or in Docker)

---

## 🐳 Running Kafka and Zookeeper with Docker Compose

Start Kafka and Zookeeper locally using the provided `docker-compose.yml`:

```bash
docker-compose up -d
Kafka: localhost:9092
Zookeeper: localhost:2181
To stop the containers:

bash
docker-compose down
⚙ Configuration
Kafka Settings
Add the following section to appsettings.json for both the Worker and API:

JSON
"Kafka": {
  "BootstrapServers": "localhost:9092",
  "TopicName": "vehicle-locations",
  "GroupId": "vehicle-location-consumer-group"
}
Database Connection
JSON
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=VehicleDb;User Id=sa;Password=Your_password123;"
}
Adjust this based on your SQL Server setup.

🚀 Build and Run
Restore and Build:

bash
dotnet build
Apply EF Core Migrations:

bash
dotnet ef database update --project Infrastructure.Persistence
Run the Kafka Worker:

bash
dotnet run --project Kafka.Worker
Run the Simulated API:

bash
dotnet run --project SimulatedSamsaraAPI
Send Test Data:

Use Postman or Swagger UI to POST to:

Code
POST /api/vehicles/locations
Example payload:

JSON
{
  "vehicleId": "V100",
  "timestamp": "2025-05-26T10:00:00",
  "latitude": 22.5,
  "longitude": 78.9,
  "speed": 60,
  "fuelLevel": 80
}
💡 Health Checks
Both API and Worker services expose health endpoints (if enabled):

/health/ready – readiness probe
/health/live – liveness probe
Example configuration in Program.cs:

C#
builder.Services.AddHealthChecks()
    .AddKafka(new ProducerConfig { BootstrapServers = kafkaBootstrapServers })
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
🚤 Stopping Services
To stop Kafka and Zookeeper:

bash
docker-compose down
To stop the Worker or API:
Press Ctrl+C in each respective terminal.

📂 Project Structure
Code
/
├── docker-compose.yml
├── README.md
├── Kafka.Worker/                 # Kafka consumer (background service)
├── SimulatedSamsaraAPI/          # Web API producer
├── Domain/                       # Domain entities and interfaces
├── Infrastructure.DependencyInjection/
├── Infrastructure.Persistence/   # EF Core DbContext, Repositories
├── Kafka.Infrastructure/         # Kafka topic management
📨 Contributing

# Health Check
===================
## Check Readiness
```sh
curl http://localhost:5072/health/ready -k
```

## Check Liveness
```sh
curl http://localhost:5072/health/live -k
```
PRs and suggestions are welcome. Please open an issue first for significant changes.
